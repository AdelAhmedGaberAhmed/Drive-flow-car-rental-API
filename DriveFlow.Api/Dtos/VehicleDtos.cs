using System.ComponentModel.DataAnnotations;
namespace DriveFlow.Api.Dtos;

public class VehicleSaveDto
{
    [Required, MaxLength(50)] public string Make { get; set; } = "";
    [Required, MaxLength(50)] public string Model { get; set; } = "";
    [Range(1900, 2100)] public int Year { get; set; }
    [Range(0, int.MaxValue)] public int Mileage { get; set; }
    public int FuelTypeId { get; set; }
    [Required, MaxLength(20)] public string PlateNumber { get; set; } = "";
    public int CategoryId { get; set; }
    [Range(0.01, 1000000)] public decimal RentalPricePerDay { get; set; }
}

public class VehicleDto
{
    public int VehicleId { get; set; }
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public int Year { get; set; }
    public int Mileage { get; set; }
    public int FuelTypeId { get; set; }
    public string FuelType { get; set; } = "";
    public string PlateNumber { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal RentalPricePerDay { get; set; }
    public string VehicleStatus { get; set; } = "";
}

public class VehicleFilterDto
{
    public int? CategoryId { get; set; }
    public int? FuelTypeId { get; set; }
    public string? Status { get; set; }
}

public class AvailableVehiclesQueryDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public int? CategoryId { get; set; }
    public int? FuelTypeId { get; set; }
}

public class VehicleStatusDto
{
    [Required] public string Status { get; set; } = "";
}
