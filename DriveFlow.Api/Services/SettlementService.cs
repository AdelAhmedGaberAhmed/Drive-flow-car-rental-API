using DriveFlow.Api.Common;
using DriveFlow.Api.Data;
using DriveFlow.Api.Dtos;
using DriveFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveFlow.Api.Services;

public class SettlementService
{
    private readonly DriveFlowDbContext _db;
    private readonly PaymentService _payments;

    public SettlementService(DriveFlowDbContext db, PaymentService payments)
    {
        _db = db;
        _payments = payments;
    }

    // Called INSIDE ReturnService's transaction (the return and its charges are already saved).
    public async Task<Result<SettlementDto>> CalculateAndSaveAsync(int returnId, string refundMethod)
    {
        var ret = await _db.VehicleReturns.FirstOrDefaultAsync(r => r.ReturnID == returnId);
        if (ret == null) return Result<SettlementDto>.Fail(ErrorKind.NotFound, "Return not found.");

        // BR-11: one settlement per return
        if (await _db.RentalSettlements.AnyAsync(s => s.ReturnID == returnId))
            return Result<SettlementDto>.Fail(ErrorKind.Conflict, "A settlement already exists for this return.");

        var booking = await _db.RentalBookings.FirstAsync(b => b.BookingID == ret.BookingID);
        var extra = await _db.AdditionalCharges.Where(c => c.ReturnID == returnId).SumAsync(c => c.ChargeAmount);

        var actualRental = RentalCalculator.CalculateAmount(ret.ActualRentalDays, booking.RentalPricePerDay);
        var netPaid = await _payments.GetNetPaidAsync(booking.BookingID);          // before any refund
        var numbers = RentalCalculator.CalculateSettlement(actualRental, extra, netPaid);

        var settlement = new RentalSettlement
        {
            ReturnID = returnId,
            ActualRentalAmount = actualRental,
            AdditionalChargesAmount = extra,
            ActualTotalDueAmount = numbers.ActualTotalDue,
            TotalPaidAmount = netPaid,
            RemainingAmount = numbers.Remaining,
            RefundAmount = numbers.Refund,
            SettlementDate = DateTime.UtcNow
        };
        _db.RentalSettlements.Add(settlement);
        await _db.SaveChangesAsync();

        // automatic refund
        if (numbers.Refund > 0)
        {
            var refund = await _payments.AddPaymentAsync(booking.BookingID, numbers.Refund, refundMethod,
                PaymentTypes.Refund, "Automatic refund at settlement");
            if (!refund.Ok) return Result<SettlementDto>.Fail(refund.Kind, refund.Error!);
        }

        // nothing left to collect -> booking is Completed; otherwise it waits for the Balance payment
        if (numbers.Remaining == 0)
        {
            booking.BookingStatus = BookingStatuses.Completed;
            await _db.SaveChangesAsync();
        }

        return Result<SettlementDto>.Success(ToDto(settlement));
    }

    public async Task<Result<SettlementDto>> GetByReturnAsync(int returnId)
    {
        var s = await _db.RentalSettlements.AsNoTracking().FirstOrDefaultAsync(x => x.ReturnID == returnId);
        return s == null
            ? Result<SettlementDto>.Fail(ErrorKind.NotFound, "Settlement not found.")
            : Result<SettlementDto>.Success(ToDto(s));
    }

    public async Task<Result<SettlementDto>> GetByBookingAsync(int bookingId)
    {
        var s = await (from st in _db.RentalSettlements.AsNoTracking()
                       join r in _db.VehicleReturns on st.ReturnID equals r.ReturnID
                       where r.BookingID == bookingId
                       select st).FirstOrDefaultAsync();
        return s == null
            ? Result<SettlementDto>.Fail(ErrorKind.NotFound, "Settlement not found.")
            : Result<SettlementDto>.Success(ToDto(s));
    }

    public static SettlementDto ToDto(RentalSettlement s) => new()
    {
        SettlementId = s.SettlementID,
        ReturnId = s.ReturnID,
        ActualRentalAmount = s.ActualRentalAmount,
        AdditionalChargesAmount = s.AdditionalChargesAmount,
        ActualTotalDueAmount = s.ActualTotalDueAmount,
        TotalPaidAmount = s.TotalPaidAmount,
        RemainingAmount = s.RemainingAmount,
        RefundAmount = s.RefundAmount,
        SettlementDate = s.SettlementDate
    };
}
