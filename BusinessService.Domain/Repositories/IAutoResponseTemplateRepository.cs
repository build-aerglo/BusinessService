using BusinessService.Domain.Entities;

namespace BusinessService.Domain.Repositories;

public interface IAutoResponseTemplateRepository
{
    Task<AutoResponseTemplate?> FindByIdAsync(Guid id);
    Task<List<AutoResponseTemplate>> FindByBusinessIdAsync(Guid businessId);
    Task<List<AutoResponseTemplate>> FindActiveByBusinessIdAsync(Guid businessId);
    Task<AutoResponseTemplate?> FindDefaultBySentimentAsync(Guid businessId, ReviewSentiment sentiment);
    Task<AutoResponseTemplate?> FindMatchingTemplateAsync(Guid businessId, ReviewSentiment sentiment, int starRating);
    Task AddAsync(AutoResponseTemplate template);
    Task UpdateAsync(AutoResponseTemplate template);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsByNameAndBusinessIdAsync(string name, Guid businessId);
    Task IncrementUsageAsync(Guid templateId);
}
