using System.ComponentModel.DataAnnotations;
namespace DriveFlow.Api.Dtos;

public class InitialPaymentDto
{
    [Range(0.01, 100000000)] public decimal Amount { get; set; }
    [Required] public string PaymentMethod { get; set; } = "";
    [MaxLength(200)] public string? PaymentDetails { get; set; }
}

public class BookingCreateDto
{
    public int CustomerId { get; set; }
    public int VehicleId { get; set; }
    public DateTime RentalStartDate { get; set; }
    public DateTime RentalEndDate { get; set; }
    [Required, MaxLength(100)] public string PickupLocation { get; set; } = "";
    [Required, MaxLength(100)] public string DropoffLocation { get; set; } = "";
    [Required] public InitialPaymentDto InitialPayment { get; set; } = new();
}

public class BookingUpdateDto
{
    public int VehicleId { get; set; }
    public DateTime RentalStartDate { get; set; }
    public DateTime RentalEndDate { get; set; }
    [Required, MaxLength(100)] public string PickupLocation { get; set; } = "";
    [Required, MaxLength(100)] public string DropoffLocation { get; set; } = "";
}

public class BookingCancelDto
{
    [Required, MaxLength(300)] public string CancellationReason { get; set; } = "";
    public string? RefundMethod { get; set; }          // default Cash
}

public class BookingPickupDto
{
    public DateTime ActualPickupDate { get; set; }
    [Range(0, int.MaxValue)] public int PickupMileage { get; set; }
    [MaxLength(500)] public string? InitialCheckNotes { get; set; }
}

public class BookingFilterDto
{
    public int? CustomerId { get; set; }
    public int? VehicleId { get; set; }
    public string? Status { get; set; }
}

public class BookingDto
{
    public int BookingId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public int VehicleId { get; set; }
    public string VehiclePlate { get; set; } = "";
    public DateTime RentalStartDate { get; set; }
    public DateTime RentalEndDate { get; set; }
    public int InitialRentalDays { get; set; }
    public decimal InitialTotalDueAmount { get; set; }
    public string BookingStatus { get; set; } = "";
}

public class BookingDetailsDto : BookingDto
{
    public DateTime BookingDate { get; set; }
    public string PickupLocation { get; set; } = "";
    public string DropoffLocation { get; set; } = "";
    public decimal RentalPricePerDay { get; set; }
    public DateTime? ActualPickupDate { get; set; }
    public int? PickupMileage { get; set; }
    public string? InitialCheckNotes { get; set; }
    public DateTime? CancellationDate { get; set; }
    public string? CancellationReason { get; set; }
}
