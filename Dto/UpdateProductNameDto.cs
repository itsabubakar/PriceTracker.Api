using System.ComponentModel.DataAnnotations;

public class UpdateProductNameDto
{
    [Required(AllowEmptyStrings = false)]
    public string Name { get; set; } = null!;
}