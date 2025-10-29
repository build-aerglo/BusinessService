namespace BusinessService.Application.DTOs;

public record UpdateBusinessDto
(
    string Name,
    string? Website,
    List<string>? CategoryIds
    );