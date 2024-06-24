using HtmlAgilityPack;

namespace HTMLToMarkdown.Services;
public interface IFormatterService
{
  string FormatTables(string doc, ref List<Replacement> replacements);

  string FormatCodeBlocks(string doc, ref List<Replacement> replacements);

}