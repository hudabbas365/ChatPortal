using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace ChatPortal.Controllers;

/// <summary>
/// Provides SEO-related endpoints for the ChatPortal application, including a
/// dynamically generated XML sitemap consumed by search engine crawlers.
/// </summary>
[Route("sitemap.xml")]
public class SeoController : Controller
{
    private readonly IWebHostEnvironment _env;

    /// <summary>
    /// Initialises a new instance of <see cref="SeoController"/>.
    /// </summary>
    /// <param name="env">The web-host environment, used to determine base URL conventions.</param>
    public SeoController(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Generates and returns a standards-compliant XML sitemap listing all public pages
    /// of the ChatPortal application.
    /// </summary>
    /// <remarks>
    /// The sitemap conforms to the <see href="https://www.sitemaps.org/protocol.html">Sitemaps
    /// Protocol 0.9</see>. Each URL entry includes a <c>&lt;lastmod&gt;</c>,
    /// <c>&lt;changefreq&gt;</c>, and <c>&lt;priority&gt;</c> element to guide crawler
    /// scheduling. The sitemap is returned with the <c>application/xml</c> content type.
    /// </remarks>
    /// <returns>An <see cref="ContentResult"/> containing the UTF-8 encoded XML sitemap.</returns>
    [HttpGet]
    public IActionResult Index()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var entries = new[]
        {
            new SitemapEntry("/",              today, "daily",   "1.0"),
            new SitemapEntry("/Chat",          today, "daily",   "0.9"),
            new SitemapEntry("/Chat/History",  today, "weekly",  "0.7"),
            new SitemapEntry("/Pages/Docs",    today, "weekly",  "0.8"),
            new SitemapEntry("/Pages/About",   today, "monthly", "0.7"),
            new SitemapEntry("/Pages/Contact", today, "monthly", "0.6"),
            new SitemapEntry("/Pages/History", today, "weekly",  "0.7"),
            new SitemapEntry("/Blog",          today, "weekly",  "0.8"),
            new SitemapEntry("/Pricing",       today, "monthly", "0.7"),
        };

        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");

        foreach (var entry in entries)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{baseUrl}{entry.Path}</loc>");
            sb.AppendLine($"    <lastmod>{entry.LastMod}</lastmod>");
            sb.AppendLine($"    <changefreq>{entry.ChangeFreq}</changefreq>");
            sb.AppendLine($"    <priority>{entry.Priority}</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    /// <summary>
    /// Represents a single URL entry in the XML sitemap.
    /// </summary>
    /// <param name="Path">The relative URL path (e.g. <c>/About</c>).</param>
    /// <param name="LastMod">The date the page was last modified in <c>yyyy-MM-dd</c> format.</param>
    /// <param name="ChangeFreq">How frequently the page is likely to change (e.g. <c>weekly</c>).</param>
    /// <param name="Priority">
    /// Crawl priority relative to other URLs on the site, in the range 0.0–1.0.
    /// </param>
    private record SitemapEntry(string Path, string LastMod, string ChangeFreq, string Priority);
}
