namespace WolfPage.Generator.Application.Rendering;

public interface ITemplateRenderer
{
    string Render(string template, Dictionary<string, object> values);
}