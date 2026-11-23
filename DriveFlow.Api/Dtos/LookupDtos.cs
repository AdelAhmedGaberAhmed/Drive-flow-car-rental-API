using System.ComponentModel.DataAnnotations;
namespace DriveFlow.Api.Dtos;

public class CategorySaveDto
{
    [Required, MaxLength(50)] public string CategoryName { get; set; } = "";
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
}

public class FuelTypeSaveDto
{
    [Required, MaxLength(30)] public string FuelType { get; set; } = "";
}

public class FuelTypeDto
{
    public int FuelTypeId { get; set; }
    public string FuelType { get; set; } = "";
}
