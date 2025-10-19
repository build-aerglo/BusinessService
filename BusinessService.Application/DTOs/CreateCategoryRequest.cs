using System.ComponentModel.DataAnnotations;

namespace BusinessService.Application.DTOs;

public class CreateCategoryRequest
{
    [Required, MinLength(3), MaxLength(100)]
    public required string Name { get; set; }

    public string? Description { get; set; }

    public Guid? ParentCategoryId { get; set; }
}