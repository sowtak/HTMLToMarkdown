using System.Text.RegularExpressions;

namespace HTMLToMarkdown.Services;
public interface ICommonFiltersService
{
  string ApplyFilters(string url, string markdown, bool ignoreLinks = false);
}

public class FilterItem
{
  public string Domain { get; set; } = string.Empty;
  public List<string> Remove { get; set; } = new List<string>();
  public List<ReplaceItem> Replace { get; set; } = new List<ReplaceItem>();
}

public class ReplaceItem
{
  public string Find { get; set; } = string.Empty;
  public Func<Match, string>? ReplacementFunc { get; set; }
  public string ReplacementString { get; set; } = string.Empty;

  public string ResolveReplacement(Match match)
  {
    return ReplacementFunc != null ? ReplacementFunc(match) : ReplacementString;
  }
}