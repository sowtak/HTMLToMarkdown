using System.Text;
using Newtonsoft.Json.Linq;

namespace HTMLToMarkdown.Services;
public class AppleDevDocParserService : IAppleDevDocParserService
{
  private Dictionary<string, JObject> devReferences = new Dictionary<string, JObject>();

  public string GetDevDocURL(string url)
  {
    var queryParts = url.Split('?');
    var queryless = queryParts[0];
    if (queryless.EndsWith("/"))
    {
      queryless = queryless.Substring(0, queryless.Length - 1);
    }
    var parts = queryless.Split('/');
    var jsonURL = "https://developer.apple.com/tutorials/data";

    for (int i = 3; i < parts.Length; i++)
    {
      jsonURL += "/" + parts[i];
    }
    jsonURL += ".json";
    return jsonURL;
  }

  public string ParseDevDocJson(JObject jsonData, bool inlineTitle = true, bool ignoreLinks = false)
  {
    var text = new StringBuilder();
    if (inlineTitle && jsonData["metadata"]?["title"] != null)
    {
      text.AppendLine("# " + jsonData["metadata"]!["title"]);
      text.AppendLine();
    }
    if (jsonData["references"] != null)
    {
      devReferences = jsonData["references"]!.ToObject<Dictionary<string, JObject>>()!;
    }
    if (jsonData["primaryContentSections"] != null)
    {
      text.Append(ProcessSections(jsonData["primaryContentSections"]!.ToObject<List<JObject>>()!, ignoreLinks));
    }
    else if (jsonData["sections"] != null)
    {
      text.Append(ProcessSections(jsonData["sections"]!.ToObject<List<JObject>>()!, ignoreLinks));
    }
    return text.ToString();
  }

  private string ProcessSections(List<JObject> sections, bool ignoreLinks)
  {
    var text = new StringBuilder();
    foreach (var section in sections)
    {
      if (section["kind"] != null)
      {
        var kind = section["kind"]!.ToString();
        if (kind == "declarations" && section["declarations"] != null)
        {
          foreach (var declaration in section["declarations"]!)
          {
            if (declaration["tokens"] != null)
            {
              foreach (var token in declaration["tokens"]!)
              {
                text.Append(token["text"]);
              }
            }
            if (declaration["languages"] != null)
            {
              text.AppendLine(" Languages: " + string.Join(", ", declaration["languages"]!));
            }
            if (declaration["platforms"] != null)
            {
              text.AppendLine(" Platforms: " + string.Join(", ", declaration["platforms"]!));
            }
          }
          text.AppendLine();
        }
        else if (kind == "content")
        {
          text.Append(ProcessContentSection(section, ignoreLinks));
        }
      }
      if (section["title"] != null)
      {
        var title = section["title"]!.ToString();
        var prefix = section["kind"] != null && section["kind"]!.ToString() == "hero" ? "# " : "## ";
        text.AppendLine(prefix + title);
      }
      if (section["content"] != null)
      {
        foreach (var content in section["content"]!)
        {
          if (content["type"] != null && content["type"]!.ToString() == "text")
          {
            text.AppendLine(content["text"]!.ToString());
          }
        }
      }
    }
    return text.ToString();
  }

  private string ProcessContentSection(JObject section, bool ignoreLinks)
  {
    var text = new StringBuilder();
    foreach (var content in section["content"]!)
    {
      var type = content["type"]!.ToString();
      switch (type)
      {
        case "paragraph":
          if (content["inlineContent"] != null)
          {
            foreach (var inline in content["inlineContent"]!)
            {
              var inlineType = inline["type"]!.ToString();
              switch (inlineType)
              {
                case "text":
                  text.Append(inline["text"]);
                  break;
                case "link":
                  if (ignoreLinks)
                  {
                    text.Append(inline["title"]);
                  }
                  else
                  {
                    text.Append($"[{inline["title"]}]({inline["destination"]})");
                  }
                  break;
                case "reference":
                  if (inline["identifier"] != null && devReferences.ContainsKey(inline["identifier"]!.ToString()))
                  {
                    text.Append(devReferences[inline["identifier"]!.ToString()]["title"]);
                  }
                  break;
                case "codeVoice":
                  if (inline["code"] != null)
                  {
                    text.Append($"`{inline["code"]}`");
                  }
                  break;
              }
            }
            text.AppendLine();
          }
          break;
        case "codeListing":
          text.AppendLine("\n```\n" + string.Join("\n", content["code"]!) + "\n```\n");
          break;
        case "unorderedList":
          foreach (var listItem in content["items"]!)
          {
            text.AppendLine("* " + ProcessContentSection(listItem.ToObject<JObject>()!, ignoreLinks));
          }
          break;
        case "orderedList":
          int i = 1;
          foreach (var listItem in content["items"]!)
          {
            text.AppendLine($"{i}. " + ProcessContentSection(listItem.ToObject<JObject>()!, ignoreLinks));
            i++;
          }
          break;
        case "heading":
          if (content["level"] != null && content["text"] != null)
          {
            text.AppendLine($"{new string('#', (int)content["level"]!)} {content["text"]}");
          }
          break;
      }
    }
    return text.ToString();
  }
}