using System.ComponentModel.DataAnnotations;
namespace DriveFlow.Api.Dtos;

public class ChargeInputDto
{
    [Required] public string ChargeType { get; set; } = "";      // Damage | Cleaning | LateFee | Fuel | Other
    [Required, MaxLength(200)] public string ChargeDescription { get; set; } = "";
    [Range(0.01, 100000000)] public decimal ChargeAmount { get; set; }
}

public class SettleRequestDto
{
    public int BookingId { get; set; }
    public DateTime ActualReturnDate { get; set; }
    [Range(0, int.MaxValue)] public int ReturnMileage { get; set; }
    [MaxLength(500)] public string? FinalCheckNotes { get; set; }
    public bool RequiresMaintenance { get; set; }
    [MaxLength(300)] public string? MaintenanceDescription { get; set; }
    public string? RefundMethod { get; set; }                   // default Cash
    public List<ChargeInputDto> Charges { get; set; } = new();  // may be empty
}

public class ChargeDto
{
    public int ChargeId { get; set; }
    public int ReturnId { get; set; }
    public string ChargeType { get; set; } = "";
    public string ChargeDescription { get; set; } = "";
    public decimal ChargeAmount { get; set; }
    public DateTime ChargeDate { get; set; }
}

public class ReturnDto
{
    public int ReturnId { get; set; }
    public int BookingId { get; set; }
    public DateTime ActualReturnDate { get; set; }
    public int ActualRentalDays { get; set; }
    public int ReturnMileage { get; set; }
    public int ConsumedMileage { get; set; }
    public string? FinalCheckNotes { get; set; }
}

public class SettlementDto
{
    public int SettlementId { get; set; }
    public int ReturnId { get; set; }
    public decimal ActualRentalAmount { get; set; }
    public decimal AdditionalChargesAmount { get; set; }
    public decimal ActualTotalDueAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public DateTime SettlementDate { get; set; }
}

public class ReturnDetailsDto
{
    public ReturnDto Return { get; set; } = new();
    public List<ChargeDto> Charges { get; set; } = new();
    public SettlementDto? Settlement { get; set; }
}

public class ReturnResultDto
{
    public int ReturnId { get; set; }
    public int ActualRentalDays { get; set; }
    public int ConsumedMileage { get; set; }
    public decimal ActualRentalAmount { get; set; }
    public decimal AdditionalChargesAmount { get; set; }
    public decimal ActualTotalDueAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public string BookingStatus { get; set; } = "";
}
