namespace BusinessService.Application.DTOs;

public record CategoryTagsDto(
    Guid CategoryId,
    string CategoryName,
    List<TagDto> Tags
);