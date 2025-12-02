namespace BusinessService.Application.DTOs;


public record NewTagRequest
(
    Guid CategoryId,
List<string> TagNames
);

public record TagDto
(   
    Guid Id,
    Guid CategoryId,
    string Name
);