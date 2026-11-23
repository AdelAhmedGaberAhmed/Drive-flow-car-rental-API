using System.ComponentModel.DataAnnotations;
namespace DriveFlow.Api.Dtos;

public class PaymentCreateDto
{
    [Range(0.01, 100000000)] public decimal Amount { get; set; }
    [Required] public string PaymentMethod { get; set; } = "";   // Cash | Card | BankTransfer
    [Required] public string PaymentType { get; set; } = "";     // Initial | Balance | Refund
    [MaxLength(200)] public string? PaymentDetails { get; set; }
}

public class PaymentDto
{
    public int PaymentId { get; set; }
    public int BookingId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string PaymentType { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public string? PaymentDetails { get; set; }
}

public class PaymentSummaryDto
{
    public int BookingId { get; set; }
    public List<PaymentDto> Payments { get; set; } = new();
    public decimal TotalPaid { get; set; }
    public decimal TotalRefunded { get; set; }
    public decimal NetPaid { get; set; }
}
