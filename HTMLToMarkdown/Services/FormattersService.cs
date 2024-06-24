using System.Text.RegularExpressions;
using System.Net;

namespace HTMLToMarkdown.Services;
class FormattersService : IFormatterService
{

  private readonly IHTMLTableToMarkdownService _htmlTableToMarkdownService;

  public FormattersService(IHTMLTableToMarkdownService htmlTableToMarkdownService)
  {
    _htmlTableToMarkdownService = htmlTableToMarkdownService;
  }
  public string FormatTables(string doc, ref List<Replacement> replacements)
  {
    int start = replacements.Count;
    var tables = Regex.Matches(doc, "<table[^>]*>(?:.|\n)*?</table>", RegexOptions.IgnoreCase);

    foreach (Match table in tables)
    {
      string markdown = _htmlTableToMarkdownService.Convert(table.Value);
      string placeholder = $"urltomarkdowntableplaceholder{start + replacements.Count}{new Random().NextDouble()}";
      replacements.Add(new Replacement { Placeholder = placeholder, ReplacementText = markdown });
      doc = Regex.Replace(doc, Regex.Escape(table.Value), $"<p>{placeholder}</p>");
    }

    return doc;
  }
  public string FormatCodeBlocks(string doc, ref List<Replacement> replacements)
    {
        int start = replacements.Count;
        var codeBlocks = Regex.Matches(doc, "<pre[^>]*>(?:.|\n)*?</pre>", RegexOptions.IgnoreCase);

        foreach (Match codeBlock in codeBlocks)
        {
            string filtered = codeBlock.Value;
            filtered = Regex.Replace(filtered, "<br[^>]*>", "\n", RegexOptions.IgnoreCase);
            filtered = Regex.Replace(filtered, "<p>", "\n", RegexOptions.IgnoreCase);
            filtered = Regex.Replace(filtered, "</?[^>]+(>|$)", "", RegexOptions.IgnoreCase);
            filtered = WebUtility.HtmlDecode(filtered);

            string markdown = $"```\n{filtered}\n```\n";
            string placeholder = $"urltomarkdowncodeblockplaceholder{start + replacements.Count}{new Random().NextDouble()}";
            replacements.Add(new Replacement { Placeholder = placeholder, ReplacementText = markdown });
            doc = Regex.Replace(doc, Regex.Escape(codeBlock.Value), $"<p>{placeholder}</p>");
        }

        return doc;
    }

}