using System.ComponentModel.DataAnnotations;

namespace BusinessService.Application.DTOs;

public class UpdateBusinessRequest
{
    [Required, MinLength(3), MaxLength(255)]
    public required string Name { get; set; }

    public string? Website { get; set; }
    public string? Description { get; set; }

    public required Guid CategoryId { get; set; }
    
    public required List<Guid> TagIds { get; set; }
    public required Guid Id { get; set; }
}