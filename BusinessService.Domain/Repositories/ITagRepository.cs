using BusinessService.Domain.Entities;

namespace BusinessService.Domain.Repositories;

public interface ITagRepository
{
    Task AddTagsAsync(Tags tag);
    Task<bool> TagExistAsync(string name);
    Task<Tags?> FindByIdAsync(Guid id);
    Task<List<string>> GetTagsAsync(Guid id);
    Task<List<Tags>> FindByNamesAsync(string[] names);

    Task<bool> DeleteBusinessTagsAsync(Guid id);
    Task AddBusinessTagAsync(Guid id, Guid businessId);

}