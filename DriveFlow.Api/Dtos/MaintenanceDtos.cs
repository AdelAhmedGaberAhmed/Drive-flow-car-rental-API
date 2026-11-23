using System.ComponentModel.DataAnnotations;
namespace DriveFlow.Api.Dtos;

public class MaintenanceCreateDto
{
    public int VehicleId { get; set; }
    [Required, MaxLength(300)] public string Description { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    [Range(0, 100000000)] public decimal Cost { get; set; }
}

public class MaintenanceUpdateDto
{
    [Required, MaxLength(300)] public string Description { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    [Range(0, 100000000)] public decimal Cost { get; set; }
}

public class MaintenanceCompleteDto
{
    public bool VehicleReady { get; set; }
    public DateTime? EndDate { get; set; }
}

public class MaintenanceDto
{
    public int MaintenanceId { get; set; }
    public int VehicleId { get; set; }
    public string Description { get; set; } = "";
    public DateTime MaintenanceDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Cost { get; set; }
    public string MaintenanceStatus { get; set; } = "";
}
