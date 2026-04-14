namespace WolfPage.Generator.Application.Messages;

public class CreatePageRequestedMessage
{
    public string CorrelationId { get; set; } = default!;
    public Guid TenantId { get; set; }
    public Guid TemplateVersionId { get; set; }
    public string PageName { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public Dictionary<string, object> Content { get; set; } = new();
}