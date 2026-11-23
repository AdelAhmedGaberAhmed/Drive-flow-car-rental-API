using DriveFlow.Api.Common;
using DriveFlow.Api.Data;
using DriveFlow.Api.Dtos;
using DriveFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveFlow.Api.Services;

public class PaymentService
{
    private readonly DriveFlowDbContext _db;
    public PaymentService(DriveFlowDbContext db) => _db = db;

    // Used by the ENDPOINT (POST /api/bookings/{id}/payments)
    public async Task<Result<PaymentDto>> RecordPaymentAsync(int bookingId, PaymentCreateDto dto)
    {
        var booking = await _db.RentalBookings.FirstOrDefaultAsync(b => b.BookingID == bookingId);
        if (booking == null) return Result<PaymentDto>.Fail(ErrorKind.NotFound, "Booking not found.");

        if (!PaymentTypes.All.Contains(dto.PaymentType))
            return Result<PaymentDto>.Fail(ErrorKind.Invalid, "Payment type must be Initial, Balance or Refund.");

        if (dto.PaymentType == PaymentTypes.Initial)
        {
            if (booking.BookingStatus != BookingStatuses.Confirmed)
                return Result<PaymentDto>.Fail(ErrorKind.Conflict, "An initial payment can only be added while the booking is Confirmed.");
            return await AddPaymentAsync(bookingId, dto.Amount, dto.PaymentMethod, dto.PaymentType, dto.PaymentDetails);
        }

        if (dto.PaymentType == PaymentTypes.Refund)
            return await AddPaymentAsync(bookingId, dto.Amount, dto.PaymentMethod, dto.PaymentType, dto.PaymentDetails);

        // ---- Balance ----
        var settlement = await FindSettlementAsync(bookingId);
        if (settlement == null)
            return Result<PaymentDto>.Fail(ErrorKind.Conflict, "A balance payment is only possible after the return settlement.");

        var netBefore = await GetNetPaidAsync(bookingId);
        var remaining = settlement.ActualTotalDueAmount - netBefore;
        if (remaining <= 0)
            return Result<PaymentDto>.Fail(ErrorKind.Conflict, "Nothing remains to be paid for this booking.");
        if (dto.Amount > remaining)
            return Result<PaymentDto>.Fail(ErrorKind.Conflict, $"The amount is more than the remaining balance ({remaining}).");

        await using var tx = await _db.Database.BeginTransactionAsync();
        var added = await AddPaymentAsync(bookingId, dto.Amount, dto.PaymentMethod, PaymentTypes.Balance, dto.PaymentDetails);
        if (!added.Ok) return added;

        // BR-07: when everything is paid the booking is Completed
        if (netBefore + dto.Amount >= settlement.ActualTotalDueAmount)
        {
            booking.BookingStatus = BookingStatuses.Completed;
            await _db.SaveChangesAsync();
        }
        await tx.CommitAsync();
        return added;
    }

    // INTERNAL: called by BookingService and SettlementService. No transaction of its own.
    public async Task<Result<PaymentDto>> AddPaymentAsync(int bookingId, decimal amount, string method, string type, string? details)
    {
        if (amount <= 0) return Result<PaymentDto>.Fail(ErrorKind.Invalid, "Payment amount must be greater than zero.");
        if (!PaymentMethods.All.Contains(method))
            return Result<PaymentDto>.Fail(ErrorKind.Invalid, "Payment method must be Cash, Card or BankTransfer.");
        if (!PaymentTypes.All.Contains(type))
            return Result<PaymentDto>.Fail(ErrorKind.Invalid, "Payment type must be Initial, Balance or Refund.");

        if (type == PaymentTypes.Refund)
        {
            var net = await GetNetPaidAsync(bookingId);
            if (amount > net)
                return Result<PaymentDto>.Fail(ErrorKind.Conflict, $"A refund cannot be more than the net amount paid ({net}).");
        }

        var p = new RentalPayment
        {
            BookingID = bookingId,
            PaymentDate = DateTime.UtcNow,
            PaymentAmount = amount,
            PaymentMethod = method,
            PaymentType = type,
            PaymentStatus = PaymentStatuses.Completed,
            PaymentDetails = details
        };
        _db.RentalPayments.Add(p);
        await _db.SaveChangesAsync();
        return Result<PaymentDto>.Success(ToDto(p));
    }

    public async Task<Result<PaymentSummaryDto>> GetHistoryAsync(int bookingId)
    {
        if (!await _db.RentalBookings.AnyAsync(b => b.BookingID == bookingId))
            return Result<PaymentSummaryDto>.Fail(ErrorKind.NotFound, "Booking not found.");

        var rows = await _db.RentalPayments.AsNoTracking()
            .Where(p => p.BookingID == bookingId)
            .OrderBy(p => p.PaymentDate).ThenBy(p => p.PaymentID)
            .ToListAsync();

        var completed = rows.Where(p => p.PaymentStatus == PaymentStatuses.Completed).ToList();
        var paid = completed.Where(p => p.PaymentType != PaymentTypes.Refund).Sum(p => p.PaymentAmount);
        var refunded = completed.Where(p => p.PaymentType == PaymentTypes.Refund).Sum(p => p.PaymentAmount);

        return Result<PaymentSummaryDto>.Success(new PaymentSummaryDto
        {
            BookingId = bookingId,
            Payments = rows.Select(ToDto).ToList(),
            TotalPaid = paid,
            TotalRefunded = refunded,
            NetPaid = paid - refunded
        });
    }

    // Completed payments: Initial + Balance - Refund
    public async Task<decimal> GetNetPaidAsync(int bookingId)
    {
        var rows = await _db.RentalPayments.AsNoTracking()
            .Where(p => p.BookingID == bookingId && p.PaymentStatus == PaymentStatuses.Completed)
            .Select(p => new { p.PaymentType, p.PaymentAmount })
            .ToListAsync();

        return rows.Sum(r => r.PaymentType == PaymentTypes.Refund ? -r.PaymentAmount : r.PaymentAmount);
    }

    private async Task<RentalSettlement?> FindSettlementAsync(int bookingId)
    {
        return await (from s in _db.RentalSettlements.AsNoTracking()
                      join r in _db.VehicleReturns on s.ReturnID equals r.ReturnID
                      where r.BookingID == bookingId
                      select s).FirstOrDefaultAsync();
    }

    private static PaymentDto ToDto(RentalPayment p) => new()
    {
        PaymentId = p.PaymentID,
        BookingId = p.BookingID,
        PaymentDate = p.PaymentDate,
        Amount = p.PaymentAmount,
        PaymentMethod = p.PaymentMethod,
        PaymentType = p.PaymentType,
        PaymentStatus = p.PaymentStatus,
        PaymentDetails = p.PaymentDetails
    };
}
