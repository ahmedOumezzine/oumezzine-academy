using Microsoft.AspNetCore.Mvc;
using OumezzineAcademy.Application.Abstractions;
using System.Text;
using System.Xml;

namespace OumezzineAcademy.Controllers;

public class SeoController : Controller
{
    private readonly ISitemapQueries _queries;

    public SeoController(ISitemapQueries queries)
    {
        _queries = queries;
    }

    [Route("robots.txt")]
    public IActionResult Robots()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Content($"User-agent: *\nAllow: /\nSitemap: {baseUrl}/sitemap.xml\n", "text/plain", Encoding.UTF8);
    }

    [Route("sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        var baseUrl = $"https://{Request.Host}";
        var urls = new List<string>
        {
            $"{baseUrl}/fr/", $"{baseUrl}/en/",
            $"{baseUrl}/fr/cours", $"{baseUrl}/en/courses",
            $"{baseUrl}/fr/categories", $"{baseUrl}/en/categories",
            $"{baseUrl}/fr/parcours", $"{baseUrl}/en/learning-paths",
            $"{baseUrl}/fr/a-propos", $"{baseUrl}/en/about"
        };

        foreach (var language in new[] { "fr", "en" })
        {
            var coursePrefix = language == "fr" ? "cours" : "courses";
            var pathPrefix = language == "fr" ? "parcours" : "learning-paths";
            var lessonPrefix = language == "fr" ? "lecons" : "lessons";

            // Keep EF projections scalar and provider-neutral. URL formatting and
            // null/empty slug checks happen after materialization, outside SQL.
            var data = await _queries.GetSlugsAsync(language);
            var courseSlugs = data.Courses;
            urls.AddRange(courseSlugs.Where(HasSlug).Select(slug => $"{baseUrl}/{language}/{coursePrefix}/{slug}"));

            var categorySlugs = data.Categories;
            urls.AddRange(categorySlugs.Where(HasSlug).Select(slug => $"{baseUrl}/{language}/categories/{slug}"));

            var pathSlugs = data.LearningPaths;
            urls.AddRange(pathSlugs.Where(HasSlug).Select(slug => $"{baseUrl}/{language}/{pathPrefix}/{slug}"));

            var lessonSlugs = data.Lessons;
            urls.AddRange(lessonSlugs.Where(HasSlug).Select(slug => $"{baseUrl}/{language}/{lessonPrefix}/{slug}"));
        }

        var xml = new StringBuilder();
        using (var writer = XmlWriter.Create(xml, new XmlWriterSettings
        {
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true
        }))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
            foreach (var url in urls.Distinct())
            {
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", "http://www.sitemaps.org/schemas/sitemap/0.9", url);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Content(xml.ToString(), "application/xml", Encoding.UTF8);
    }

    private static bool HasSlug(string? slug) => !string.IsNullOrWhiteSpace(slug);
}