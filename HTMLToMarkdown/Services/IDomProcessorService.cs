using HtmlAgilityPack;

namespace HTMLToMarkdown.Services;
public interface IDomProcessorService
{
  (string title, string markdown) ProcessDom(string url, HtmlDocument doc, bool inlineTitle = true, bool ignoreLinks = false, string elementID = "");
}