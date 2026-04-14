using WolfPage.Generator.Domain.Entities;

namespace WolfPage.Generator.Application.Persistence;

public interface IPageGenerationRepository
{
    Task<TemplateVersion?> FindTemplateVersionAsync(Guid id, CancellationToken cancellationToken);
    void AddRequest(PageGenerationRequest request);
    void AddPage(Page page);
    Task SaveAsync(CancellationToken cancellationToken);
}
