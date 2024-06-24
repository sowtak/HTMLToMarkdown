using System.Text.RegularExpressions;

namespace HTMLToMarkdown.Services;
public class CommonFiltersService : ICommonFiltersService
{
  public string ApplyFilters(string url, string markdown, bool ignoreLinks = false)
  {
    string domain = string.Empty;
    string baseAddress = string.Empty;

    if (!string.IsNullOrEmpty(url))
    {
      Uri parsedURL = new Uri(url);
      baseAddress = $"{parsedURL.Scheme}://{parsedURL.Host}";
      domain = parsedURL.Host;
    }

    var filterList = new List<FilterItem>
        {
            new FilterItem
            {
                Domain = @".*",
                Remove = new List<string>
                {
                    @"\[¶\](\(#[^\s]+\s+""[^""]+""\))"
                },
                Replace = new List<ReplaceItem>
                {
                    new ReplaceItem
                    {
                        Find = @"\[([^\]]*)\](\/\/([^\)]*))",
                        ReplacementString = @"[$1](https://$3)"
                    }
                }
            },
            new FilterItem
            {
                Domain = @".*\.wikipedia\.org",
    Remove = new List<string>
    {
        @"\*\*\[\^\]\(#cite_ref[^\)]+\)\*\*",
        @"(?:\\\[)?\[edit\](?:\([^)]+\))?(?:\\\])?",  // Updated regex
        @"\^(\s\[Jump up to[^\)]*\))",
        @"\[([^\]]*)\]\(#cite_ref[^\)]+\)",
        @"\[\!\[Edit this at Wikidata\].*"
    },
    Replace = new List<ReplaceItem>
    {
        new ReplaceItem
        {
            Find = @"\(https:\/\/upload\.wikimedia\.org\/wikipedia\/([^\/]+)\/thumb\/([^\)]+\..{3,4})\/[^\)]+\)",
            ReplacementString = @"(https://upload.wikimedia.org/wikipedia/$1/$2)"
        },
        new ReplaceItem
        {
            Find = @"\n(.+)\n-{32,}\n",
            ReplacementFunc = match => $"\n{match.Groups[1].Value}\n{new string('-', match.Groups[1].Value.Length)}\n"
        }
    }
            },
            new FilterItem
            {
                Domain = @"(?:.*\.)?medium\.com",
                Replace = new List<ReplaceItem>
                {
                    new ReplaceItem
                    {
                        Find = @"(https://miro.medium.com/max/60/",
                        ReplacementString = @"(https://miro.medium.com/max/600/"
                    }
                }
            },
            new FilterItem
            {
                Domain = @"(?:.*\.)?stackoverflow\.com",
                Remove = new List<string>
                {
                    @"\* +Links(.|\\r|\\n)*Three +\|"
                }
            }
        };

    foreach (var filterItem in filterList)
    {
      if (Regex.IsMatch(domain, filterItem.Domain))
      {
        foreach (var removeRegex in filterItem.Remove)
        {
          markdown = Regex.Replace(markdown, removeRegex, string.Empty);
        }

        foreach (var replaceItem in filterItem.Replace)
        {
          markdown = Regex.Replace(markdown, replaceItem.Find, new MatchEvaluator(match => replaceItem.ResolveReplacement(match)));
        }
      }
    }

    // Make relative URLs absolute
    markdown = Regex.Replace(
        markdown,
        @"\[([^\]]*)\](\/([^\/][^\)]*))",
        match => $"[{match.Groups[1].Value}]({baseAddress}/{match.Groups[3].Value})"
    );

    // Remove inline links and refs if ignoreLinks is true
    if (ignoreLinks)
    {
      markdown = Regex.Replace(markdown, @"\[(\\\[)?([^\]]+)(\\\])?\]\([^\)]+\)", "$2");
      markdown = Regex.Replace(markdown, @"\[(\\\[\])?([0-9]+)(\\\[\])?\]", "[$2]");
    }

    return markdown;
  }


}