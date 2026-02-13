using System.ComponentModel.DataAnnotations;

namespace BusinessService.Application.DTOs;

public class CreateBusinessRequest
{
    [Required, MinLength(3), MaxLength(255)]
    public required string Name { get; set; }

    public string? Website { get; set; }

    public string? Email { get; set; }

    [Required, MinLength(1, ErrorMessage = "At least one category must be specified")]
    public required List<Guid> CategoryIds { get; set; }

    public Guid? ParentBusinessId { get; set; }
    public string? Status { get; set; }
    public string? BusinessCityTown { get; set; }
}