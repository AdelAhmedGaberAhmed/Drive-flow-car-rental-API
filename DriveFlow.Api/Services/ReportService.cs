using DriveFlow.Api.Common;
using DriveFlow.Api.Data;
using DriveFlow.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DriveFlow.Api.Services;

/// <summary>Read-only reports. "to" is inclusive (the whole last day counts).</summary>
public class ReportService
{
    private readonly DriveFlowDbContext _db;
    public ReportService(DriveFlowDbContext db) => _db = db;

    public async Task<BookingsReportDto> GetBookingsReportAsync(DateTime? from, DateTime? to)
    {
        var q = _db.RentalBookings.AsNoTracking().AsQueryable();
        if (from.HasValue) q = q.Where(b => b.BookingDate >= from.Value.Date);
        if (to.HasValue) { var end = to.Value.Date.AddDays(1); q = q.Where(b => b.BookingDate < end); }

        var list = await (from b in q
                          join c in _db.Customers on b.CustomerID equals c.CustomerID
                          join v in _db.Vehicles on b.VehicleID equals v.VehicleID
                          orderby b.BookingID descending
                          select new BookingDto
                          {
                              BookingId = b.BookingID,
                              CustomerId = b.CustomerID,
                              CustomerName = c.Name,
                              VehicleId = b.VehicleID,
                              VehiclePlate = v.PlateNumber,
                              RentalStartDate = b.RentalStartDate,
                              RentalEndDate = b.RentalEndDate,
                              InitialRentalDays = b.InitialRentalDays,
                              InitialTotalDueAmount = b.InitialTotalDueAmount,
                              BookingStatus = b.BookingStatus
                          }).ToListAsync();

        return new BookingsReportDto
        {
            From = from,
            To = to,
            TotalBookings = list.Count,
            ByStatus = list.GroupBy(b => b.BookingStatus).ToDictionary(g => g.Key, g => g.Count()),
            Bookings = list
        };
    }

    public async Task<PaymentsReportDto> GetPaymentsReportAsync(DateTime? from, DateTime? to)
    {
        var q = _db.RentalPayments.AsNoTracking().Where(p => p.PaymentStatus == PaymentStatuses.Completed);
        if (from.HasValue) q = q.Where(p => p.PaymentDate >= from.Value.Date);
        if (to.HasValue) { var end = to.Value.Date.AddDays(1); q = q.Where(p => p.PaymentDate < end); }

        var rows = await q.Select(p => new { p.PaymentType, p.PaymentAmount }).ToListAsync();
        var initial = rows.Where(r => r.PaymentType == PaymentTypes.Initial).Sum(r => r.PaymentAmount);
        var balance = rows.Where(r => r.PaymentType == PaymentTypes.Balance).Sum(r => r.PaymentAmount);
        var refunds = rows.Where(r => r.PaymentType == PaymentTypes.Refund).Sum(r => r.PaymentAmount);

        return new PaymentsReportDto
        {
            From = from,
            To = to,
            PaymentsCount = rows.Count,
            TotalInitial = initial,
            TotalBalance = balance,
            TotalRefunds = refunds,
            NetRevenue = initial + balance - refunds
        };
    }

    public async Task<VehiclesReportDto> GetVehiclesReportAsync()
    {
        var statuses = await _db.Vehicles.AsNoTracking().Select(v => v.VehicleStatus).ToListAsync();
        var byStatus = statuses.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

        return new VehiclesReportDto
        {
            TotalVehicles = statuses.Count,
            ByStatus = byStatus,
            AvailableNow = byStatus.TryGetValue(VehicleStatuses.Available, out var n) ? n : 0
        };
    }

    public async Task<ReturnsReportDto> GetReturnsReportAsync(DateTime? from, DateTime? to)
    {
        var q = _db.VehicleReturns.AsNoTracking().AsQueryable();
        if (from.HasValue) q = q.Where(r => r.ActualReturnDate >= from.Value.Date);
        if (to.HasValue) { var end = to.Value.Date.AddDays(1); q = q.Where(r => r.ActualReturnDate < end); }

        var returns = await q.Select(r => new { r.ReturnID, r.ActualRentalDays, r.ConsumedMileage }).ToListAsync();
        var ids = returns.Select(r => r.ReturnID).ToList();
        var extra = ids.Count == 0
            ? 0m
            : await _db.AdditionalCharges.Where(c => ids.Contains(c.ReturnID)).SumAsync(c => c.ChargeAmount);

        return new ReturnsReportDto
        {
            From = from,
            To = to,
            ReturnsCount = returns.Count,
            AverageActualRentalDays = returns.Count == 0 ? 0 : returns.Average(r => r.ActualRentalDays),
            TotalConsumedMileage = returns.Sum(r => (long)r.ConsumedMileage),
            TotalAdditionalCharges = extra
        };
    }

    public async Task<MaintenanceReportDto> GetMaintenanceReportAsync(DateTime? from, DateTime? to)
    {
        var q = _db.MaintenanceRecords.AsNoTracking().AsQueryable();
        if (from.HasValue) q = q.Where(m => m.StartDate >= from.Value.Date);
        if (to.HasValue) q = q.Where(m => m.StartDate <= to.Value.Date);

        var rows = await q.Select(m => new { m.MaintenanceStatus, m.Cost }).ToListAsync();
        return new MaintenanceReportDto
        {
            From = from,
            To = to,
            Count = rows.Count,
            TotalCost = rows.Sum(r => r.Cost),
            ByStatus = rows.GroupBy(r => r.MaintenanceStatus).ToDictionary(g => g.Key, g => g.Count())
        };
    }
}
