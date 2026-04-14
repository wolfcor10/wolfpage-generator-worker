using System.Text.Json;
using Microsoft.Extensions.Logging;
using WolfPage.Generator.Application.Messages;
using WolfPage.Generator.Application.Persistence;
using WolfPage.Generator.Application.Rendering;
using WolfPage.Generator.Domain.Entities;
using WolfPage.Generator.Domain.Enums;

namespace WolfPage.Generator.Application.Services;

public class PageGenerationService : IPageGenerationService
{
    private readonly IPageGenerationRepository _repository;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ILogger<PageGenerationService> _logger;

    public PageGenerationService(
        IPageGenerationRepository repository,
        ITemplateRenderer templateRenderer,
        ILogger<PageGenerationService> logger)
    {
        _repository = repository;
        _templateRenderer = templateRenderer;
        _logger = logger;
    }

    public async Task ProcessAsync(CreatePageRequestedMessage message, CancellationToken cancellationToken = default)
    {
        var request = new PageGenerationRequest
        {
            Id = Guid.NewGuid(),
            TenantId = message.TenantId,
            TemplateVersionId = message.TemplateVersionId,
            CorrelationId = message.CorrelationId,
            PageName = message.PageName,
            Slug = message.Slug,
            ContentJson = JsonSerializer.Serialize(message.Content),
            Status = RequestStatus.Processing,
            CreatedAt = DateTime.UtcNow
        };

        _repository.AddRequest(request);
        await _repository.SaveAsync(cancellationToken);

        try
        {
            var templateVersion = await _repository.FindTemplateVersionAsync(
                message.TemplateVersionId, cancellationToken);

            if (templateVersion is null)
                throw new InvalidOperationException($"No existe TemplateVersion {message.TemplateVersionId}");

            var html = _templateRenderer.Render(templateVersion.HtmlTemplate, message.Content);
            var css = _templateRenderer.Render(templateVersion.CssTemplate ?? string.Empty, message.Content);
            var js = _templateRenderer.Render(templateVersion.JsTemplate ?? string.Empty, message.Content);

            var page = new Page
            {
                Id = Guid.NewGuid(),
                TenantId = message.TenantId,
                TemplateVersionId = message.TemplateVersionId,
                RequestId = request.Id,
                Title = message.PageName,
                Slug = message.Slug,
                RoutePath = $"/{message.Slug}",
                HtmlContent = html,
                CssContent = css,
                JsContent = js,
                Status = PageStatus.Generated,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _repository.AddPage(page);

            request.Status = RequestStatus.Completed;
            request.ProcessedAt = DateTime.UtcNow;

            await _repository.SaveAsync(cancellationToken);

            _logger.LogInformation(
                "Página generada. RequestId={RequestId}, PageId={PageId}, CorrelationId={CorrelationId}",
                request.Id, page.Id, message.CorrelationId);
        }
        catch (Exception ex)
        {
            request.Status = RequestStatus.Failed;
            request.ErrorMessage = ex.Message;
            request.ProcessedAt = DateTime.UtcNow;

            await _repository.SaveAsync(cancellationToken);

            _logger.LogError(ex,
                "Error procesando solicitud. RequestId={RequestId}, CorrelationId={CorrelationId}",
                request.Id, message.CorrelationId);

            throw;
        }
    }
}
