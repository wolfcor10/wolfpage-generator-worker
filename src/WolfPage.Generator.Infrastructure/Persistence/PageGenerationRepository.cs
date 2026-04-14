using Microsoft.EntityFrameworkCore;
using WolfPage.Generator.Application.Persistence;
using WolfPage.Generator.Domain.Entities;

namespace WolfPage.Generator.Infrastructure.Persistence;

public class PageGenerationRepository : IPageGenerationRepository
{
    private readonly AppDbContext _dbContext;

    public PageGenerationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TemplateVersion?> FindTemplateVersionAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.TemplateVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public void AddRequest(PageGenerationRequest request) =>
        _dbContext.PageGenerationRequests.Add(request);

    public void AddPage(Page page) =>
        _dbContext.Pages.Add(page);

    public Task SaveAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
