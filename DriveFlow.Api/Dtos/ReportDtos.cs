namespace DriveFlow.Api.Dtos;

public class BookingsReportDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int TotalBookings { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
    public List<BookingDto> Bookings { get; set; } = new();
}

public class PaymentsReportDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int PaymentsCount { get; set; }
    public decimal TotalInitial { get; set; }
    public decimal TotalBalance { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal NetRevenue { get; set; }
}

public class VehiclesReportDto
{
    public int TotalVehicles { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
    public int AvailableNow { get; set; }
}

public class ReturnsReportDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int ReturnsCount { get; set; }
    public double AverageActualRentalDays { get; set; }
    public long TotalConsumedMileage { get; set; }
    public decimal TotalAdditionalCharges { get; set; }
}

public class MaintenanceReportDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Count { get; set; }
    public decimal TotalCost { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
}
