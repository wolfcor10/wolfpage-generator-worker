using WolfPage.Generator.Application.Messages;

namespace WolfPage.Generator.Application.Services;

public interface IPageGenerationService
{
    Task ProcessAsync(CreatePageRequestedMessage message, CancellationToken cancellationToken = default);
}