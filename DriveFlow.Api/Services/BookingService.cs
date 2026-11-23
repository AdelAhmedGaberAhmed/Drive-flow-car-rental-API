using System.Data;
using DriveFlow.Api.Common;
using DriveFlow.Api.Data;
using DriveFlow.Api.Dtos;
using DriveFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveFlow.Api.Services;

public class BookingService
{
    private readonly DriveFlowDbContext _db;
    private readonly VehicleService _vehicles;
    private readonly PaymentService _payments;

    public BookingService(DriveFlowDbContext db, VehicleService vehicles, PaymentService payments)
    {
        _db = db;
        _vehicles = vehicles;
        _payments = payments;
    }

    public async Task<Result<BookingDetailsDto>> CreateAsync(BookingCreateDto dto)
    {
        // 1. basic input checks
        if (dto.RentalEndDate <= dto.RentalStartDate)
            return Result<BookingDetailsDto>.Fail(ErrorKind.Invalid, "End date must be after start date.");
        if (dto.InitialPayment == null || dto.InitialPayment.Amount <= 0)
            return Result<BookingDetailsDto>.Fail(ErrorKind.Invalid, "An initial payment greater than zero is required.");

        // 2. customer + vehicle exist
        if (!await _db.Customers.AnyAsync(c => c.CustomerID == dto.CustomerId))
            return Result<BookingDetailsDto>.Fail(ErrorKind.NotFound, "Customer not found.");
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleID == dto.VehicleId);
        if (vehicle == null)
            return Result<BookingDetailsDto>.Fail(ErrorKind.NotFound, "Vehicle not found.");

        // 3. transaction: two bookings made at the same moment cannot both pass the check
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        // 4. availability (maintenance / retired / overlap)
        var avail = await _vehicles.CheckAvailabilityAsync(dto.VehicleId, dto.RentalStartDate, dto.RentalEndDate);
        if (!avail.Ok) return Result<BookingDetailsDto>.Fail(avail.Kind, avail.Error!);

        // 5. days + total
        var days = RentalCalculator.CalculateDays(dto.RentalStartDate, dto.RentalEndDate);
        var total = RentalCalculator.CalculateAmount(days, vehicle.RentalPricePerDay);

        // 6. save the booking (the price per day is stored in the booking)
        var booking = new RentalBooking
        {
            CustomerID = dto.CustomerId,
            VehicleID = dto.VehicleId,
            BookingDate = DateTime.UtcNow,
            RentalStartDate = dto.RentalStartDate,
            RentalEndDate = dto.RentalEndDate,
            PickupLocation = dto.PickupLocation,
            DropoffLocation = dto.DropoffLocation,
            InitialRentalDays = days,
            RentalPricePerDay = vehicle.RentalPricePerDay,
            InitialTotalDueAmount = total,
            BookingStatus = BookingStatuses.Confirmed
        };
        _db.RentalBookings.Add(booking);
        await _db.SaveChangesAsync();

        // 7. initial payment
        var pay = await _payments.AddPaymentAsync(booking.BookingID, dto.InitialPayment.Amount,
            dto.InitialPayment.PaymentMethod, PaymentTypes.Initial, dto.InitialPayment.PaymentDetails);
        if (!pay.Ok) return Result<BookingDetailsDto>.Fail(pay.Kind, pay.Error!);

        // 8. commit (vehicle status is NOT changed; availability is decided by dates)
        await tx.CommitAsync();
        return Result<BookingDetailsDto>.Success(await ToDetailsDtoAsync(booking));
    }

    public async Task<List<BookingDto>> ListAsync(BookingFilterDto filter)
    {
        var q = _db.RentalBookings.AsNoTracking().AsQueryable();
        if (filter.CustomerId.HasValue) q = q.Where(b => b.CustomerID == filter.CustomerId.Value);
        if (filter.VehicleId.HasValue) q = q.Where(b => b.VehicleID == filter.VehicleId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status)) q = q.Where(b => b.BookingStatus == filter.Status);

        return await (from b in q
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
    }

    public async Task<Result<BookingDetailsDto>> GetAsync(int id)
    {
        var b = await _db.RentalBookings.AsNoTracking().FirstOrDefaultAsync(x => x.BookingID == id);
        if (b == null) return Result<BookingDetailsDto>.Fail(ErrorKind.NotFound, "Booking not found.");
        return Result<BookingDetailsDto>.Success(await ToDetailsDtoAsync(b));
    }

    // FR-27: only while Confirmed
    public async Task<Result<BookingDetailsDto>> UpdateAsync(int id, BookingUpdateDto dto)
    {
        var b = await _db.RentalBookings.FirstOrDefaultAsync(x => x.BookingID == id);
        if (b == null) return Result<BookingDetailsDto>.Fail(ErrorKind.NotFound, "Booking not found.");
        if (b.BookingStatus != BookingStatuses.Confirmed)
            return Result<BookingDetailsDto>.Fail(ErrorKind.Conflict, "Only a Confirmed booking can be updated.");
        if (dto.RentalEndDate <= dto.RentalStartDate)
            return Result<BookingDetailsDto>.Fail(ErrorKind.Invalid, "End date must be after start date.");

        var vehicle = await _db.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.VehicleID == dto.VehicleId);
        if (vehicle == null) return Result<BookingDetailsDto>.Fail(ErrorKind.NotFound, "Vehicle not found.");

        var avail = await _vehicles.CheckAvailabilityAsync(dto.VehicleId, dto.RentalStartDate, dto.RentalEndDate, excludeBookingId: id);
        if (!avail.Ok) return Result<BookingDetailsDto>.Fail(avail.Kind, avail.Error!);

        // new vehicle -> its current price; same vehicle -> keep the saved price
        var price = dto.VehicleId != b.VehicleID ? vehicle.RentalPricePerDay : b.RentalPricePerDay;
        var days = RentalCalculator.CalculateDays(dto.RentalStartDate, dto.RentalEndDate);

        b.VehicleID = dto.VehicleId;
        b.RentalStartDate = dto.RentalStartDate;
        b.RentalEndDate = dto.RentalEndDate;
        b.PickupLocation = dto.PickupLocation;
        b.DropoffLocation = dto.DropoffLocation;
        b.RentalPricePerDay = price;
        b.InitialRentalDays = days;
        b.InitialTotalDueAmount = RentalCalculator.CalculateAmount(days, price);
        await _db.SaveChangesAsync();

        return Result<BookingDetailsDto>.Success(await ToDetailsDtoAsync(b));
    }

    // Only Confirmed + not picked up. Full refund of the net paid amount.
    public async Task<Result<BookingDetailsDto>> CancelAsync(int id, BookingCancelDto dto)
    {
        var b = await _db.RentalBookings.FirstOrDefaultAsync(x => x.BookingID == id);
        if (b == null) return Result<BookingDetailsDto>.Fail(ErrorKind.NotFound, "Booking not found.");
        if (b.BookingStatus != BookingStatuses.Confirmed || b.ActualPickupDate != null)
            return Result<BookingDetailsDto>.Fail(ErrorKind.Conflict, "Only a Confirmed booking that was not picked up can be cancelled.");
        if (string.IsNullOrWhiteSpace(dto.CancellationReason))
            return Result<BookingDetailsDto>.Fail(ErrorKind.Invalid, "A cancellation reason is required.");

        var refundMethod = string.IsNullOrWhiteSpace(dto.RefundMethod) ? PaymentMethods.Cash : dto.RefundMethod!;
        if (!PaymentMethods.All.Contains(refundMethod))
            return Result<BookingDetailsDto>.Fail(ErrorKind.Invalid, "Refund method must be Cash, Card or BankTransfer.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        b.BookingStatus = BookingStatuses.Cancelled;
        b.CancellationDate = DateTime.UtcNow;
        b.CancellationReason = dto.CancellationReason;
        await _db.SaveChangesAsync();

        var net = await _payments.GetNetPaidAsync(id);
        if (net > 0)
        {
            var refund = await _payments.AddPaymentAsync(id, net, refundMethod, PaymentTypes.Refund, "Cancellation refund");
            if (!refund.Ok) return Result<BookingDetailsDto>.Fail(refund.Kind, refund.Error!);
        }

        await tx.CommitAsync();       // vehicle status is not touched
        return Result<BookingDetailsDto>.Success(await ToDetailsDtoAsync(b));
    }

    // FR-35..37, TC-12
    public async Task<Result<BookingDetailsDto>> RecordPickupAsync(int id, BookingPickupDto dto)
    {
        var b = await _db.RentalBookings.FirstOrDefaultAsync(x => x.BookingID == id);
        if (b == null) return Result<BookingDetailsDto>.Fail(ErrorKind.NotFound, "Booking not found.");
        if (b.BookingStatus != BookingStatuses.Confirmed)
            return Result<BookingDetailsDto>.Fail(ErrorKind.Conflict, "Only a Confirmed booking can be picked up.");

        var vehicle = await _db.Vehicles.AsNoTracking().FirstAsync(v => v.VehicleID == b.VehicleID);
        if (vehicle.VehicleStatus != VehicleStatuses.Available)
            return Result<BookingDetailsDto>.Fail(ErrorKind.Conflict,
                $"The vehicle is not available for pickup (status: {vehicle.VehicleStatus}).");
        if (dto.PickupMileage < vehicle.Mileage)
            return Result<BookingDetailsDto>.Fail(ErrorKind.Invalid, "Pickup mileage cannot be less than the vehicle's current mileage.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        b.ActualPickupDate = dto.ActualPickupDate;
        b.PickupMileage = dto.PickupMileage;
        b.InitialCheckNotes = dto.InitialCheckNotes;
        b.BookingStatus = BookingStatuses.Active;
        await _db.SaveChangesAsync();

        var status = await _vehicles.SetStatusAsync(b.VehicleID, VehicleStatuses.Rented);
        if (!status.Ok) return Result<BookingDetailsDto>.Fail(status.Kind, status.Error!);

        await tx.CommitAsync();
        return Result<BookingDetailsDto>.Success(await ToDetailsDtoAsync(b));
    }

    private async Task<BookingDetailsDto> ToDetailsDtoAsync(RentalBooking b)
    {
        var customerName = await _db.Customers.Where(c => c.CustomerID == b.CustomerID).Select(c => c.Name).FirstAsync();
        var plate = await _db.Vehicles.Where(v => v.VehicleID == b.VehicleID).Select(v => v.PlateNumber).FirstAsync();

        return new BookingDetailsDto
        {
            BookingId = b.BookingID,
            CustomerId = b.CustomerID,
            CustomerName = customerName,
            VehicleId = b.VehicleID,
            VehiclePlate = plate,
            RentalStartDate = b.RentalStartDate,
            RentalEndDate = b.RentalEndDate,
            InitialRentalDays = b.InitialRentalDays,
            InitialTotalDueAmount = b.InitialTotalDueAmount,
            BookingStatus = b.BookingStatus,
            BookingDate = b.BookingDate,
            PickupLocation = b.PickupLocation,
            DropoffLocation = b.DropoffLocation,
            RentalPricePerDay = b.RentalPricePerDay,
            ActualPickupDate = b.ActualPickupDate,
            PickupMileage = b.PickupMileage,
            InitialCheckNotes = b.InitialCheckNotes,
            CancellationDate = b.CancellationDate,
            CancellationReason = b.CancellationReason
        };
    }
}
