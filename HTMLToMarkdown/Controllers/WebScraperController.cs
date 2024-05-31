using Microsoft.AspNetCore.Mvc;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Cors;
using Microsoft.Net.Http.Headers;
using System.Net;
using HtmlAgilityPack;
using ReverseMarkdown;

namespace Converter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowAllOrigins")]
    public class ConverterController : ControllerBase
    {
        private readonly ILogger<ConverterController> _logger;

        public ConverterController(ILogger<ConverterController> logger)
        {
            _logger = logger;
        }
        [HttpGet("convert")]
        public IActionResult ConvertToMarkdown([FromQuery] string url, [FromQuery] bool includeTitle = true, [FromQuery] bool ignoreLinks = true)
        {
            if (!IsValidUrl(url))
            {
                _logger.LogWarning("Invalid URL: {Url}", url);
                return BadRequest("Please specify a valid URL.");
            }

            try 
            {
                var (title, markdown) = ReadUrl(url, includeTitle, ignoreLinks);
                var response = new ContentResult
                {
                    Content = markdown,
                    ContentType = "text/markdown",
                    StatusCode = (int)HttpStatusCode.OK
                };
                Response.Headers.Append("X-Title", title ?? string.Empty);
                _logger.LogInformation("Converted URL: {Url}", url);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting URL: {Url}", url);
                return BadRequest("Could not parse that document.");
            }
        }

        [HttpPost("convert")]
        public IActionResult ConvertHtmlToMarkdown([FromBody] MarkdownRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Html))
                return BadRequest("Please specify HTML in the request body.");

            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(request.Html);
                var converter = new ReverseMarkdown.Converter();
                var markdown = converter.Convert(doc.DocumentNode.InnerHtml);
                return Ok(markdown);
                
            }
            catch
            {
                return BadRequest("Could not parse that document.");
            }
        }

        private bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private (string, string) ReadUrl(string url, bool includeTitle, bool ignoreLinks)
        {
            // Logic to fetch HTML from URL

            // Logic to fetch HTML, convert to Markdown, return title and content
            return ("Sample Title", "Converted Markdown");
        }

  }

    public class MarkdownRequest
    {
        public string Html { get; set; } = string.Empty;
        public string Url { get; set; }  = string.Empty;
        public bool IncludeTitle { get; set; }
        public bool IgnoreLinks { get; set; }
    }
}