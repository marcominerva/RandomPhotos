using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace RandomPhotos.Pages;

[OutputCache(PolicyName = "Default")]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
