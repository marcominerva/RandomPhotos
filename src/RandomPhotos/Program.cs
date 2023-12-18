using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using ChatGptNet;
using DallENet;
using DallENet.Exceptions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using MinimalHelpers.Routing;
using OperationResults.AspNetCore.Http;
using Polly;
using Polly.Retry;
using RandomPhotos.BusinessLayer.Services;
using RandomPhotos.BusinessLayer.Services.Interfaces;
using RandomPhotos.BusinessLayer.Settings;
using RandomPhotos.Extensions;
using RandomPhotos.Swagger;
using TinyHelpers.AspNetCore.Extensions;
using TinyHelpers.AspNetCore.Swagger;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true);

// Add services to the container.
var settings = builder.Services.ConfigureAndGet<AppSettings>(builder.Configuration, nameof(AppSettings));
var swagger = builder.Services.ConfigureAndGet<SwaggerSettings>(builder.Configuration, nameof(SwaggerSettings));

builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorPages();

builder.Services.AddRequestLocalization(settings.SupportedCultures);

builder.Services.AddWebOptimizer(minifyCss: true, minifyJavaScript: builder.Environment.IsProduction());

builder.Services.AddChatGpt(builder.Configuration);
builder.Services.AddDallE(builder.Configuration);

builder.Services.AddResiliencePipeline("DallEContentFilterResiliencePipeline", (builder, context) =>
{
    builder.AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder().Handle<DallEException>(ex => ex.Error?.Code == "contentFilter"),
        Delay = TimeSpan.Zero,
        BackoffType = DelayBackoffType.Constant,
        MaxRetryAttempts = 3,
        OnRetry = args =>
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogError(args.Outcome.Exception, "Unexpected error while generating the image");
            return default;
        }
    });
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var statusCode = context.ProblemDetails.Status.GetValueOrDefault(StatusCodes.Status500InternalServerError);
        context.ProblemDetails.Type ??= $"https://httpstatuses.io/{statusCode}";
        context.ProblemDetails.Title ??= ReasonPhrases.GetReasonPhrase(statusCode);
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
        context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    };
});

builder.Services.Scan(scan => scan
          .FromAssemblyOf<IPhotoService>()
            .AddClasses(classes => classes.InNamespaceOf<PhotoService>().Where(type => type.Name.EndsWith("Service")))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

if (swagger.IsEnabled)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "RandomPhotos API", Version = "v1" });

        options.AddAcceptLanguageHeader();

        options.AddDefaultResponse();
    });
}

builder.Services.AddOperationResult(options =>
{
    options.ErrorResponseFormat = ErrorResponseFormat.List;
});

builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("Default", policy =>
    {
        policy.SetLocking(false)
            .SetVaryByHeader(HeaderNames.AcceptLanguage, HeaderNames.UserAgent)
            .Expire(TimeSpan.FromDays(30));
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
    {
        return RateLimitPartition.GetTokenBucketLimiter("Default", _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 500,
            TokensPerPeriod = 50,
            ReplenishmentPeriod = TimeSpan.FromHours(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, token) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var window))
        {
            context.HttpContext.Response.Headers.RetryAfter = window.TotalSeconds.ToString();
        }

        return ValueTask.CompletedTask;
    };
});

var app = builder.Build();
app.Services.GetRequiredService<IWebHostEnvironment>().ApplicationName = settings.ApplicationName;

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseWhen(context => context.IsWebRequest(), builder =>
{
    if (!app.Environment.IsDevelopment())
    {
        builder.UseExceptionHandler("/errors/500");

        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        builder.UseHsts();
    }

    builder.UseStatusCodePagesWithReExecute("/errors/{0}");
});

app.UseWhen(context => context.IsApiRequest(), builder =>
{
    builder.UseExceptionHandler();
    builder.UseStatusCodePages();
});

app.UseWebOptimizer();
app.UseStaticFiles();

if (swagger.IsEnabled)
{
    app.UseMiddleware<SwaggerBasicAuthenticationMiddleware>();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RandomPhotos API v1");
        options.InjectStylesheet("/css/swagger.css");
    });
}

app.UseRouting();
app.UseRequestLocalization();

app.UseWhen(context => context.IsApiRequest(), builder =>
{
    builder.UseRateLimiter();
});

// app.UseCors();

app.UseWhen(context => context.IsWebRequest(), builder =>
{
    // In Razor Pages apps and apps with controllers, UseOutputCache must be called after UseRouting.
    builder.UseOutputCache();
});

app.MapEndpoints();
app.MapRazorPages();

app.Run();