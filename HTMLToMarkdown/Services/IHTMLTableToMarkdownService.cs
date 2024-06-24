using HtmlAgilityPack;

namespace HTMLToMarkdown.Services;

public interface IHTMLTableToMarkdownService 
{
  string Clean(HtmlDocument doc);

  string Convert(string htmlTable);
}
