using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using System.Net;
using HtmlAgilityPack;
using HTMLToMarkdown.Services;
using Newtonsoft.Json.Linq;

namespace Converter.Controllers;
[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowAllOrigins")]
public class ConverterController : ControllerBase
{

  private const string AppleDevPrefix = "https://developer.apple.com";
  private const string StackOverflowPrefix = "https://stackoverflow.com/questions";
  private readonly ILogger<ConverterController> _logger;

  private readonly IDomProcessorService _domProcessorService;

  private readonly IAppleDevDocParserService _AppleDevDocParserService;

  public ConverterController(ILogger<ConverterController> logger,
   IDomProcessorService domProcessorService,
   IAppleDevDocParserService AppleDevDocParserService)
  {
    _logger = logger;
    _domProcessorService = domProcessorService;
    _AppleDevDocParserService = AppleDevDocParserService;
  }

  [HttpGet("convert")]
  public async Task<IActionResult> ConvertHTMLToMarkdown([FromQuery] string url, [FromQuery] bool includeTitle = true, [FromQuery] bool ignoreLinks = true)
  {
    if (!IsValidURL(url))
    {
      _logger.LogWarning("Invalid URL: {URL}", url);
      return BadRequest("Please specify a valid URL.");
    }

    string title = "";
    string markdown;
    using (var httpClient = new HttpClient())
    {
      if (url.StartsWith(AppleDevPrefix, StringComparison.OrdinalIgnoreCase))
      {
        var jsonURL = _AppleDevDocParserService.GetDevDocURL(url);
        try
        {
          var appleDocResponse = await httpClient.GetAsync(jsonURL);
          appleDocResponse.EnsureSuccessStatusCode();
          var jsonData = await appleDocResponse.Content.ReadAsStringAsync();
          var json = JObject.Parse(jsonData);
          var appleDocMarkdown = _AppleDevDocParserService.ParseDevDocJson(json, includeTitle, ignoreLinks);
          _logger.LogInformation("Converted URL: {URL}", url);
          var response = new ContentResult
          {
            Content = appleDocMarkdown,
            ContentType = "text/markdown",
            StatusCode = (int)HttpStatusCode.OK
          };
          Response.Headers.Append("X-Title", title ?? string.Empty);
          return response;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error converting URL: {URL}", url);
          return BadRequest("Could not parse that document.");
        }

      }
      else if (url.StartsWith(StackOverflowPrefix, StringComparison.OrdinalIgnoreCase))
      {
        try
        {
          var stackOverflowResponse = await httpClient.GetAsync(url);
          stackOverflowResponse.EnsureSuccessStatusCode();
          var content = await stackOverflowResponse.Content.ReadAsStringAsync();
          var doc = new HtmlDocument();
          doc.LoadHtml(content);

          string stackOverflowTitle = "";
          string markdownA = "";
          string markdownQ = "";
          (stackOverflowTitle, markdownQ) = _domProcessorService.ProcessDom(url, doc, includeTitle, ignoreLinks, "question");
          (stackOverflowTitle, markdownA) = _domProcessorService.ProcessDom(url, doc, includeTitle, ignoreLinks, "answers");

          if (markdownA.StartsWith("Your Answer"))
          {
            _logger.LogInformation("Converted URL: {URL}", url);
            var response = new ContentResult
            {
              Content = markdownA,
              ContentType = "text/markdown",
              StatusCode = (int)HttpStatusCode.OK
            };
            Response.Headers.Append("X-Title", title ?? string.Empty);
            return response;
          }
          else
          {
            var response = new ContentResult
            {
              Content = markdownQ,
              ContentType = "text/markdown",
              StatusCode = (int)HttpStatusCode.OK
            };
            Response.Headers.Append("X-Title", title ?? string.Empty);
            _logger.LogInformation("Converted URL: {URL}", url);
            return Content(stackOverflowTitle!, markdownQ + "\n\n## Answer\n" + markdownA);
          }

        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error converting URL: {URL}", url);
          return BadRequest("Could not parse that document.");
        }
      }
      else
      {
        try
        {
          var htmlResponse = await httpClient.GetAsync(url);
          htmlResponse.EnsureSuccessStatusCode();
          var content = await htmlResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
          var doc = new HtmlDocument();
          doc.LoadHtml(content);
          if (doc.DocumentNode.SelectSingleNode("//title") != null)
          {
            title = doc.DocumentNode.SelectSingleNode("//title").InnerText;
          }
          (title, markdown) = _domProcessorService.ProcessDom(url, doc, includeTitle, ignoreLinks);


          var response = new ContentResult
          {
            Content = markdown,
            ContentType = "text/markdown",
            StatusCode = (int)HttpStatusCode.OK
          };
          Response.Headers.Append("X-Title", title ?? string.Empty);
          _logger.LogInformation("Converted URL: {URL}", url);
          return response;

        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error converting URL: {URL}", url);
          return BadRequest("Could not parse that document.");
        }
      }
    }
  }

  private bool IsValidURL(string url)
  {
    return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
           && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
  }


}
