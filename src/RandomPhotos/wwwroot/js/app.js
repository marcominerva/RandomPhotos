async function generateRandomPhoto(language) {
    const response = await fetch('/api/photos', {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "Accept-Language": language
        }
    });

    return response;
}

function GetErrorMessage(statusCode, content)
{
    if (statusCode >= 200 && statusCode <= 299)
        return null;

    if (content.errors)
    {
        return `${content.title ?? content} (${content.errors[0].message})`;
    }

    return content.detail ?? content.title ?? content;
}

function sleep(time) {
    return new Promise((resolve) => {
        setTimeout(resolve, time);
    });
}

async function copyToClipboard(element, text, copyToClipboardMessage, copiedMessage)
{
    let tooltip = bootstrap.Tooltip.getInstance(element);
    tooltip.hide();

    navigator.clipboard.writeText(text);

    element.setAttribute('data-bs-title', copiedMessage);

    tooltip = new bootstrap.Tooltip(element);
    tooltip.show();

    await sleep(3000);
    tooltip.hide();

    // Resets the tooltip title
    element.setAttribute('data-bs-title', copyToClipboardMessage);
    new bootstrap.Tooltip(element);
}