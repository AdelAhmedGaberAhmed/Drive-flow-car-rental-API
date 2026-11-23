using DriveFlow.Api.Common;
using DriveFlow.Api.Data;
using DriveFlow.Api.Dtos;
using DriveFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveFlow.Api.Services;

public class VehicleService
{
    private readonly DriveFlowDbContext _db;
    public VehicleService(DriveFlowDbContext db) => _db = db;

    // Vehicle + category name + fuel type name, ready for the DTO
    private IQueryable<VehicleDto> Query() =>
        from v in _db.Vehicles.AsNoTracking()
        join c in _db.VehicleCategories on v.CategoryID equals c.CategoryID
        join f in _db.FuelTypes on v.FuelTypeID equals f.FuelTypeID
        select new VehicleDto
        {
            VehicleId = v.VehicleID,
            Make = v.Make,
            Model = v.Model,
            Year = v.Year,
            Mileage = v.Mileage,
            FuelTypeId = v.FuelTypeID,
            FuelType = f.FuelTypeName,
            PlateNumber = v.PlateNumber,
            CategoryId = v.CategoryID,
            CategoryName = c.CategoryName,
            RentalPricePerDay = v.RentalPricePerDay,
            VehicleStatus = v.VehicleStatus
        };

    public async Task<List<VehicleDto>> ListAsync(VehicleFilterDto filter)
    {
        var q = Query();
        if (filter.CategoryId.HasValue) q = q.Where(v => v.CategoryId == filter.CategoryId.Value);
        if (filter.FuelTypeId.HasValue) q = q.Where(v => v.FuelTypeId == filter.FuelTypeId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status)) q = q.Where(v => v.VehicleStatus == filter.Status);
        return await q.OrderBy(v => v.VehicleId).ToListAsync();
    }

    // FR-15/16: cars that can be booked for [Start, End)
    public async Task<Result<List<VehicleDto>>> GetAvailableAsync(AvailableVehiclesQueryDto qry)
    {
        if (qry.End <= qry.Start)
            return Result<List<VehicleDto>>.Fail(ErrorKind.Invalid, "End date must be after start date.");

        var start = qry.Start;
        var end = qry.End;

        var q = Query().Where(v => v.VehicleStatus != VehicleStatuses.UnderMaintenance
                                && v.VehicleStatus != VehicleStatuses.Retired);

        if (qry.CategoryId.HasValue) q = q.Where(v => v.CategoryId == qry.CategoryId.Value);
        if (qry.FuelTypeId.HasValue) q = q.Where(v => v.FuelTypeId == qry.FuelTypeId.Value);

        // no Confirmed/Active booking overlaps (same rule as RentalCalculator.PeriodsOverlap)
        q = q.Where(v => !_db.RentalBookings.Any(b =>
                b.VehicleID == v.VehicleId &&
                (b.BookingStatus == BookingStatuses.Confirmed || b.BookingStatus == BookingStatuses.Active) &&
                b.RentalStartDate < end && b.RentalEndDate > start));

        return Result<List<VehicleDto>>.Success(await q.OrderBy(v => v.VehicleId).ToListAsync());
    }

    public async Task<Result<VehicleDto>> GetAsync(int id)
    {
        var v = await Query().FirstOrDefaultAsync(x => x.VehicleId == id);
        return v == null
            ? Result<VehicleDto>.Fail(ErrorKind.NotFound, "Vehicle not found.")
            : Result<VehicleDto>.Success(v);
    }

    public async Task<Result<VehicleDto>> CreateAsync(VehicleSaveDto dto)
    {
        var check = await ValidateAsync(dto, null);
        if (check != null) return Result<VehicleDto>.Fail(check.Value.kind, check.Value.msg);

        var entity = new Vehicle
        {
            Make = dto.Make,
            Model = dto.Model,
            Year = dto.Year,
            Mileage = dto.Mileage,
            FuelTypeID = dto.FuelTypeId,
            PlateNumber = dto.PlateNumber,
            CategoryID = dto.CategoryId,
            RentalPricePerDay = dto.RentalPricePerDay,
            VehicleStatus = VehicleStatuses.Available
        };
        _db.Vehicles.Add(entity);
        await _db.SaveChangesAsync();
        return await GetAsync(entity.VehicleID);
    }

    public async Task<Result<VehicleDto>> UpdateAsync(int id, VehicleSaveDto dto)
    {
        var entity = await _db.Vehicles.FindAsync(id);
        if (entity == null) return Result<VehicleDto>.Fail(ErrorKind.NotFound, "Vehicle not found.");

        var check = await ValidateAsync(dto, id);
        if (check != null) return Result<VehicleDto>.Fail(check.Value.kind, check.Value.msg);

        entity.Make = dto.Make;
        entity.Model = dto.Model;
        entity.Year = dto.Year;
        entity.Mileage = dto.Mileage;
        entity.FuelTypeID = dto.FuelTypeId;
        entity.PlateNumber = dto.PlateNumber;
        entity.CategoryID = dto.CategoryId;
        entity.RentalPricePerDay = dto.RentalPricePerDay;      // status is NOT changed here
        await _db.SaveChangesAsync();
        return await GetAsync(id);
    }

    // FR-12: the admin may only set Available or Retired by hand
    public async Task<Result<VehicleDto>> ChangeStatusAsync(int id, string newStatus)
    {
        if (newStatus != VehicleStatuses.Available && newStatus != VehicleStatuses.Retired)
            return Result<VehicleDto>.Fail(ErrorKind.Invalid,
                "You can only set the status to 'Available' or 'Retired'. 'Under Maintenance' comes from maintenance and 'Rented' from pickup.");

        var v = await _db.Vehicles.FindAsync(id);
        if (v == null) return Result<VehicleDto>.Fail(ErrorKind.NotFound, "Vehicle not found.");

        if (newStatus == VehicleStatuses.Retired)
        {
            var hasBookings = await _db.RentalBookings.AnyAsync(b => b.VehicleID == id &&
                (b.BookingStatus == BookingStatuses.Confirmed || b.BookingStatus == BookingStatuses.Active));
            if (hasBookings)
                return Result<VehicleDto>.Fail(ErrorKind.Conflict, "The vehicle has Confirmed/Active bookings and cannot be retired.");
        }
        else // Available
        {
            if (await _db.MaintenanceRecords.AnyAsync(m => m.VehicleID == id && m.MaintenanceStatus == MaintenanceStatuses.InProgress))
                return Result<VehicleDto>.Fail(ErrorKind.Conflict, "A maintenance record is still in progress for this vehicle.");
            if (await _db.RentalBookings.AnyAsync(b => b.VehicleID == id && b.BookingStatus == BookingStatuses.Active))
                return Result<VehicleDto>.Fail(ErrorKind.Conflict, "The vehicle is currently rented.");
        }

        v.VehicleStatus = newStatus;
        await _db.SaveChangesAsync();
        return await GetAsync(id);
    }

    // ---------- internal helpers used by other services ----------

    // No business checks: just sets the status (and mileage if given).
    public async Task<Result<bool>> SetStatusAsync(int vehicleId, string status, int? newMileage = null)
    {
        var v = await _db.Vehicles.FindAsync(vehicleId);
        if (v == null) return Result<bool>.Fail(ErrorKind.NotFound, "Vehicle not found.");

        v.VehicleStatus = status;
        if (newMileage.HasValue) v.Mileage = newMileage.Value;
        await _db.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> CheckAvailabilityAsync(int vehicleId, DateTime start, DateTime end, int? excludeBookingId = null)
    {
        var v = await _db.Vehicles.AsNoTracking().FirstOrDefaultAsync(x => x.VehicleID == vehicleId);
        if (v == null) return Result<bool>.Fail(ErrorKind.NotFound, "Vehicle not found.");

        if (v.VehicleStatus == VehicleStatuses.UnderMaintenance || v.VehicleStatus == VehicleStatuses.Retired)
            return Result<bool>.Fail(ErrorKind.Conflict, "This vehicle is not available for booking.");

        var overlap = await _db.RentalBookings.AnyAsync(b =>
            b.VehicleID == vehicleId &&
            (excludeBookingId == null || b.BookingID != excludeBookingId) &&
            (b.BookingStatus == BookingStatuses.Confirmed || b.BookingStatus == BookingStatuses.Active) &&
            b.RentalStartDate < end && b.RentalEndDate > start);

        return overlap
            ? Result<bool>.Fail(ErrorKind.Conflict, "Vehicle is already booked for these dates.")
            : Result<bool>.Success(true);
    }

    // plate unique (409), category + fuel type exist (400), price > 0 (400)
    private async Task<(ErrorKind kind, string msg)?> ValidateAsync(VehicleSaveDto dto, int? excludeId)
    {
        if (dto.RentalPricePerDay <= 0) return (ErrorKind.Invalid, "Rental price per day must be greater than zero.");
        if (!await _db.VehicleCategories.AnyAsync(c => c.CategoryID == dto.CategoryId))
            return (ErrorKind.Invalid, "Category does not exist.");
        if (!await _db.FuelTypes.AnyAsync(f => f.FuelTypeID == dto.FuelTypeId))
            return (ErrorKind.Invalid, "Fuel type does not exist.");
        if (await _db.Vehicles.AnyAsync(v => v.PlateNumber == dto.PlateNumber && (excludeId == null || v.VehicleID != excludeId)))
            return (ErrorKind.Conflict, "A vehicle with this plate number already exists.");
        return null;
    }
}
