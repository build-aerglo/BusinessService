using BusinessService.Application.DTOs;

namespace BusinessService.Application.Interfaces;

public interface ITagService
{
    Task <bool> CreateTagAsync(NewTagRequest request);
    
    Task<List<string>> GetCategoryTagsAsync(Guid id);
}