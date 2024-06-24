using HtmlAgilityPack;
using ReverseMarkdown;

namespace HTMLToMarkdown.Services;


class DomProcessorService : IDomProcessorService
{

  private readonly IFormatterService _formatterService;
  private readonly ICommonFiltersService _commonFiltersService;

  public DomProcessorService(IFormatterService formatterService, ICommonFiltersService commonFiltersService)
  {
    _formatterService = formatterService;
    _commonFiltersService = commonFiltersService;
  }
  public (string title, string markdown) ProcessDom(string url, HtmlDocument doc, bool inlineTitle, bool ignoreLinks, string elementID = "")
  {
    string readableHtml;
    string title;

    // only selects div elements
    if (!string.IsNullOrEmpty(elementID))
    {
      var element = doc.GetElementbyId(elementID);
      if (element != null)
      {
        var newHtml = $"<!DOCTYPE html>{element.OuterHtml}";
        doc = new HtmlDocument();
        doc.LoadHtml(newHtml);
      }
    }

    title = GetShortTitle(doc);


    readableHtml = doc.DocumentNode.SelectSingleNode("//body").InnerHtml;
    Console.Write(readableHtml);

    var replacements = new List<Replacement>();
    readableHtml = _formatterService.FormatTables(readableHtml, ref replacements);
    readableHtml = _formatterService.FormatCodeBlocks(readableHtml, ref replacements);

    var converter = new ReverseMarkdown.Converter
    {
      Config =
            {
                UnknownTags = Config.UnknownTagsOption.PassThrough, // Keep unknown tags
                GithubFlavored = true // Use GitHub flavored markdown
            }
    };

    if (ignoreLinks)
    {
      converter.Config.SmartHrefHandling = false; // Disable automatic link handling
    }

    var markdown = converter.Convert(readableHtml);

    markdown = _commonFiltersService.ApplyFilters(url, markdown, ignoreLinks);


    if (inlineTitle && !string.IsNullOrEmpty(title))
    {
      markdown = "# " + title + "\n" + markdown;
    }

    return (title, markdown);
  }

  private string GetShortTitle(HtmlDocument document)
  {
    var titleNode = document.DocumentNode.SelectSingleNode("//title");
    return titleNode != null ? titleNode.InnerText : "";
  }
}
