using System.Text.RegularExpressions;

namespace WolfPage.Generator.Application.Rendering;

public class SimpleTemplateRenderer : ITemplateRenderer
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{\s*(\w+)\s*\}\}", RegexOptions.Compiled);

    public string Render(string template, Dictionary<string, object> values)
    {
        if (string.IsNullOrWhiteSpace(template))
            return string.Empty;

        return PlaceholderRegex.Replace(template, match =>
        {
            var key = match.Groups[1].Value;

            if (!values.TryGetValue(key, out var value) || value is null)
                return string.Empty;

            return value.ToString() ?? string.Empty;
        });
    }
}