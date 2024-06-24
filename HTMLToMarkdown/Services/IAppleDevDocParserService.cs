using Newtonsoft.Json.Linq;

namespace HTMLToMarkdown.Services;
public interface IAppleDevDocParserService
{
  string GetDevDocURL(string url);
  string ParseDevDocJson(JObject jsonData, bool inlineTitle = true, bool ignoreLinks = false);
}