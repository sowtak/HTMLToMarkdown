using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Web;

namespace HTMLToMarkdown.Services;
public class HTMLTableToMarkdownService : IHTMLTableToMarkdownService
{
  public const int MaxWidth = 96;

  public string Clean(HtmlDocument doc)
  {
    var text = doc.DocumentNode.InnerText;
    text = Regex.Replace(text, @"<\/?[^>]+(>|$)", ""); // Remove HTML tags
    text = Regex.Replace(text, @"(\r\n|\n|\r)", "");   // Remove new lines
    text = HttpUtility.HtmlDecode(text);              // Decode HTML entities
    return text;
  }

  public string Convert(string htmlTable)
  {
    HtmlDocument doc = new HtmlDocument();
    doc.LoadHtml(htmlTable);
    var result = "\n";

    // Handle caption
    var captionNode = doc.DocumentNode.SelectSingleNode("//caption");
    if (captionNode != null)
    {
      HtmlDocument captionDoc = new HtmlDocument();
      captionDoc.LoadHtml(captionNode.InnerHtml);
      result += Clean(captionDoc) + "\n\n";
    }

    var rows = doc.DocumentNode.SelectNodes("//tr");
    if (rows == null || rows.Count < 2) return ""; // Check if it's a proper table

    // Collect data
    var items = new List<List<string>>();
    foreach (var row in rows)
    {
      var itemCols = new List<string>();
      var cols = row.SelectNodes("th|td");
      foreach (var col in cols)
      {
        HtmlDocument colDoc = new HtmlDocument();
        colDoc.LoadHtml(col.InnerHtml);
        itemCols.Add(Clean(colDoc));
      }
      items.Add(itemCols);
    }

    // Normalize and adjust column widths
    int nCols = items.Max(r => r.Count);
    int[] columnWidths = new int[nCols];
    for (int r = 0; r < items.Count; r++)
    {
      for (int c = 0; c < items[r].Count; c++)
      {
        int currentLength = items[r][c].Length;
        if (currentLength > columnWidths[c])
        {
          columnWidths[c] = currentLength;
        }
      }
    }

    // Build Markdown string based on total width
    int totalWidth = columnWidths.Sum();
    if (totalWidth < MaxWidth)
    {
      // Build Markdown table
      result += BuildMarkdownTable(items, columnWidths);
    }
    else
    {
      // Build indented list
      result += BuildIndentedList(items);
    }

    return result;
  }

  private string BuildMarkdownTable(List<List<string>> items, int[] columnWidths)
  {
    string result = "";
    for (int r = 0; r < items.Count; r++)
    {
      result += "|";
      for (int c = 0; c < items[r].Count; c++)
      {
        result += items[r][c].PadRight(columnWidths[c]) + "|";
      }
      result += "\n";
      if (r == 0) // Header separator
      {
        result += "|" + string.Join("|", columnWidths.Select(w => new string('-', w))) + "|\n";
      }
    }
    return result;
  }

  private string BuildIndentedList(List<List<string>> items)
{
    string result = "\n";
    if (items.Count == 0)
        return result;

    int headerCount = items[0].Count;  // Number of columns in the header row

    for (int r = 1; r < items.Count; r++)
    {
        int currentRowCount = items[r].Count;
        for (int c = 0; c < headerCount; c++)
        {
            string headerItem = c < items[0].Count ? items[0][c] : "";
            string rowItem = c < currentRowCount ? items[r][c] : "";

            if (!string.IsNullOrEmpty(headerItem) || !string.IsNullOrEmpty(rowItem))
            {
                if (c == 0)
                {
                    result += "* ";
                }
                else
                {
                    result += "  * ";
                }

                if (!string.IsNullOrEmpty(headerItem))
                {
                    result += headerItem + ": ";
                }

                if (!string.IsNullOrEmpty(rowItem))
                {
                    result += rowItem;
                }

                result += "\n";
            }
        }
    }
    return result;
}
}