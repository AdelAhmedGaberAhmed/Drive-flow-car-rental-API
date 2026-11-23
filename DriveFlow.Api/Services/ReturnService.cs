using DriveFlow.Api.Common;
using DriveFlow.Api.Data;
using DriveFlow.Api.Dtos;
using DriveFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveFlow.Api.Services;

public class ReturnService
{
    private readonly DriveFlowDbContext _db;
    private readonly SettlementService _settlements;
    private readonly MaintenanceService _maintenance;
    private readonly VehicleService _vehicles;

    public ReturnService(DriveFlowDbContext db, SettlementService settlements,
                         MaintenanceService maintenance, VehicleService vehicles)
    {
        _db = db;
        _settlements = settlements;
        _maintenance = maintenance;
        _vehicles = vehicles;
    }

    // POST /api/returns/settle : return + charges + settlement in ONE transaction
    public async Task<Result<ReturnResultDto>> ProcessReturnAsync(SettleRequestDto req)
    {
        // 1. booking must exist and be Active (picked up)
        var booking = await _db.RentalBookings.FirstOrDefaultAsync(b => b.BookingID == req.BookingId);
        if (booking == null) return Result<ReturnResultDto>.Fail(ErrorKind.NotFound, "Booking not found.");
        if (booking.BookingStatus != BookingStatuses.Active || booking.ActualPickupDate == null || booking.PickupMileage == null)
            return Result<ReturnResultDto>.Fail(ErrorKind.Conflict, "Only an Active booking (already picked up) can be returned.");

        // 2. BR-10: one return per booking
        if (await _db.VehicleReturns.AnyAsync(r => r.BookingID == req.BookingId))
            return Result<ReturnResultDto>.Fail(ErrorKind.Conflict, "This booking already has a return record.");

        // 3. dates and mileage
        var pickupDate = booking.ActualPickupDate.Value;
        var pickupMileage = booking.PickupMileage.Value;
        if (req.ActualReturnDate < pickupDate)
            return Result<ReturnResultDto>.Fail(ErrorKind.Invalid, "Return date cannot be before the pickup date.");
        if (req.ReturnMileage < pickupMileage)
            return Result<ReturnResultDto>.Fail(ErrorKind.Invalid, "Return mileage cannot be less than the pickup mileage.");

        // 4. charges
        var charges = req.Charges ?? new List<ChargeInputDto>();
        foreach (var c in charges)
        {
            if (!ChargeTypes.All.Contains(c.ChargeType))
                return Result<ReturnResultDto>.Fail(ErrorKind.Invalid, "Charge type must be Damage, Cleaning, LateFee, Fuel or Other.");
            if (c.ChargeAmount <= 0)
                return Result<ReturnResultDto>.Fail(ErrorKind.Invalid, "Each charge amount must be greater than zero.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        // 5. save the return
        var ret = new VehicleReturn
        {
            BookingID = booking.BookingID,
            ActualReturnDate = req.ActualReturnDate,
            ActualRentalDays = RentalCalculator.CalculateDays(pickupDate, req.ActualReturnDate),
            ReturnMileage = req.ReturnMileage,
            ConsumedMileage = RentalCalculator.CalculateConsumedMileage(pickupMileage, req.ReturnMileage),
            FinalCheckNotes = req.FinalCheckNotes
        };
        _db.VehicleReturns.Add(ret);
        await _db.SaveChangesAsync();                     // gives ret.ReturnID

        // 6. save the charges
        foreach (var c in charges)
        {
            _db.AdditionalCharges.Add(new AdditionalCharge
            {
                ReturnID = ret.ReturnID,
                ChargeType = c.ChargeType,
                ChargeDescription = c.ChargeDescription,
                ChargeAmount = c.ChargeAmount,
                ChargeDate = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        // 7. maintenance record when repair is needed
        if (req.RequiresMaintenance)
        {
            var description = !string.IsNullOrWhiteSpace(req.MaintenanceDescription) ? req.MaintenanceDescription!
                            : !string.IsNullOrWhiteSpace(req.FinalCheckNotes) ? req.FinalCheckNotes!
                            : "Maintenance required after return";
            var m = await _maintenance.CreateFromReturnAsync(booking.VehicleID, description);
            if (!m.Ok) return Result<ReturnResultDto>.Fail(m.Kind, m.Error!);
        }

        // 8. vehicle: final status + mileage
        var status = await _vehicles.SetStatusAsync(booking.VehicleID,
            req.RequiresMaintenance ? VehicleStatuses.UnderMaintenance : VehicleStatuses.Available,
            req.ReturnMileage);
        if (!status.Ok) return Result<ReturnResultDto>.Fail(status.Kind, status.Error!);

        // 9. settlement (also records the automatic refund and completes the booking when nothing is left to pay)
        var refundMethod = string.IsNullOrWhiteSpace(req.RefundMethod) ? PaymentMethods.Cash : req.RefundMethod!;
        if (!PaymentMethods.All.Contains(refundMethod))
            return Result<ReturnResultDto>.Fail(ErrorKind.Invalid, "Refund method must be Cash, Card or BankTransfer.");

        var settlement = await _settlements.CalculateAndSaveAsync(ret.ReturnID, refundMethod);
        if (!settlement.Ok) return Result<ReturnResultDto>.Fail(settlement.Kind, settlement.Error!);

        // 10. commit
        await tx.CommitAsync();

        var s = settlement.Data!;
        return Result<ReturnResultDto>.Success(new ReturnResultDto
        {
            ReturnId = ret.ReturnID,
            ActualRentalDays = ret.ActualRentalDays,
            ConsumedMileage = ret.ConsumedMileage,
            ActualRentalAmount = s.ActualRentalAmount,
            AdditionalChargesAmount = s.AdditionalChargesAmount,
            ActualTotalDueAmount = s.ActualTotalDueAmount,
            TotalPaidAmount = s.TotalPaidAmount,
            RemainingAmount = s.RemainingAmount,
            RefundAmount = s.RefundAmount,
            BookingStatus = booking.BookingStatus
        });
    }

    // GET /api/bookings/{id}/return : return + charges + settlement
    public async Task<Result<ReturnDetailsDto>> GetByBookingAsync(int bookingId)
    {
        var ret = await _db.VehicleReturns.AsNoTracking().FirstOrDefaultAsync(r => r.BookingID == bookingId);
        if (ret == null) return Result<ReturnDetailsDto>.Fail(ErrorKind.NotFound, "No return found for this booking.");

        var charges = await _db.AdditionalCharges.AsNoTracking()
            .Where(c => c.ReturnID == ret.ReturnID).OrderBy(c => c.ChargeID).ToListAsync();
        var settlement = await _db.RentalSettlements.AsNoTracking().FirstOrDefaultAsync(s => s.ReturnID == ret.ReturnID);

        return Result<ReturnDetailsDto>.Success(new ReturnDetailsDto
        {
            Return = new ReturnDto
            {
                ReturnId = ret.ReturnID,
                BookingId = ret.BookingID,
                ActualReturnDate = ret.ActualReturnDate,
                ActualRentalDays = ret.ActualRentalDays,
                ReturnMileage = ret.ReturnMileage,
                ConsumedMileage = ret.ConsumedMileage,
                FinalCheckNotes = ret.FinalCheckNotes
            },
            Charges = charges.Select(ToChargeDto).ToList(),
            Settlement = settlement == null ? null : SettlementService.ToDto(settlement)
        });
    }

    public async Task<Result<List<ChargeDto>>> GetChargesAsync(int returnId)
    {
        if (!await _db.VehicleReturns.AnyAsync(r => r.ReturnID == returnId))
            return Result<List<ChargeDto>>.Fail(ErrorKind.NotFound, "Return not found.");

        var charges = await _db.AdditionalCharges.AsNoTracking()
            .Where(c => c.ReturnID == returnId).OrderBy(c => c.ChargeID).ToListAsync();
        return Result<List<ChargeDto>>.Success(charges.Select(ToChargeDto).ToList());
    }

    private static ChargeDto ToChargeDto(AdditionalCharge c) => new()
    {
        ChargeId = c.ChargeID,
        ReturnId = c.ReturnID,
        ChargeType = c.ChargeType,
        ChargeDescription = c.ChargeDescription,
        ChargeAmount = c.ChargeAmount,
        ChargeDate = c.ChargeDate
    };
}
