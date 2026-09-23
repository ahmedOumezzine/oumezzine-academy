using Ganss.Xss;

namespace OumezzineAcademy.Infrastructure.Sanitization;

public sealed class HtmlSanitizerService : OumezzineAcademy.Application.Abstractions.IHtmlSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Clear();
        foreach (var tag in new[] { "p", "h2", "h3", "h4", "ul", "ol", "li", "strong", "em", "u", "a", "blockquote", "pre", "code", "img", "figure", "figcaption", "br", "table", "thead", "tbody", "tfoot", "tr", "th", "td" })
            _sanitizer.AllowedTags.Add(tag);
        _sanitizer.AllowedAttributes.Clear();
        foreach (var attribute in new[] { "href", "title", "target", "rel", "src", "alt", "width", "height", "loading", "class" })
            _sanitizer.AllowedAttributes.Add(attribute);
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("");
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("mailto");
        _sanitizer.UriAttributes.Clear();
        _sanitizer.UriAttributes.Add("href");
        _sanitizer.UriAttributes.Add("src");
    }

    public string? Sanitize(string? html)
        => string.IsNullOrWhiteSpace(html) ? html : _sanitizer.Sanitize(html);
}