using DriveFlow.Api.Common;
using DriveFlow.Api.Data;
using DriveFlow.Api.Dtos;
using DriveFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveFlow.Api.Services;

public class MaintenanceService
{
    private readonly DriveFlowDbContext _db;
    private readonly VehicleService _vehicles;

    public MaintenanceService(DriveFlowDbContext db, VehicleService vehicles)
    {
        _db = db;
        _vehicles = vehicles;
    }

    // Admin endpoint
    public async Task<Result<MaintenanceDto>> CreateAsync(MaintenanceCreateDto dto)
    {
        var vehicle = await _db.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.VehicleID == dto.VehicleId);
        if (vehicle == null) return Result<MaintenanceDto>.Fail(ErrorKind.NotFound, "Vehicle not found.");

        if (dto.EndDate < dto.StartDate)
            return Result<MaintenanceDto>.Fail(ErrorKind.Invalid, "End date cannot be before start date.");
        if (dto.Cost < 0)
            return Result<MaintenanceDto>.Fail(ErrorKind.Invalid, "Cost cannot be negative.");

        if (vehicle.VehicleStatus == VehicleStatuses.Rented)
            return Result<MaintenanceDto>.Fail(ErrorKind.Conflict, "The vehicle is currently rented. Record its return first.");

        if (await _db.MaintenanceRecords.AnyAsync(m => m.VehicleID == dto.VehicleId && m.MaintenanceStatus == MaintenanceStatuses.InProgress))
            return Result<MaintenanceDto>.Fail(ErrorKind.Conflict, "This vehicle already has a maintenance record in progress.");

        var start = dto.StartDate.Date;
        var end = dto.EndDate.Date;
        var clashes = await _db.RentalBookings.AsNoTracking()
            .Where(b => b.VehicleID == dto.VehicleId &&
                        (b.BookingStatus == BookingStatuses.Confirmed || b.BookingStatus == BookingStatuses.Active) &&
                        b.RentalStartDate <= end && b.RentalEndDate > start)
            .Select(b => b.BookingID).ToListAsync();
        if (clashes.Count > 0)
            return Result<MaintenanceDto>.Fail(ErrorKind.Conflict,
                $"These bookings overlap the maintenance period and must be resolved first: {string.Join(", ", clashes)}.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var entity = new Maintenance
        {
            VehicleID = dto.VehicleId,
            Description = dto.Description,
            MaintenanceDate = DateTime.UtcNow.Date,
            StartDate = start,
            EndDate = end,
            Cost = dto.Cost,
            MaintenanceStatus = MaintenanceStatuses.InProgress
        };
        _db.MaintenanceRecords.Add(entity);
        await _db.SaveChangesAsync();

        var status = await _vehicles.SetStatusAsync(dto.VehicleId, VehicleStatuses.UnderMaintenance);
        if (!status.Ok) return Result<MaintenanceDto>.Fail(status.Kind, status.Error!);

        await tx.CommitAsync();
        return Result<MaintenanceDto>.Success(ToDto(entity));
    }

    // INTERNAL: called by ReturnService when the car came back damaged.
    // No conflict check. Does NOT set the vehicle status (ReturnService does that).
    public async Task<Result<MaintenanceDto>> CreateFromReturnAsync(int vehicleId, string description)
    {
        var today = DateTime.UtcNow.Date;
        var entity = new Maintenance
        {
            VehicleID = vehicleId,
            Description = description,
            MaintenanceDate = today,
            StartDate = today,
            EndDate = null,                       // unknown yet (column must allow NULL, see Sql/setup.sql)
            Cost = 0,
            MaintenanceStatus = MaintenanceStatuses.InProgress
        };
        _db.MaintenanceRecords.Add(entity);
        await _db.SaveChangesAsync();
        return Result<MaintenanceDto>.Success(ToDto(entity));
    }

    public async Task<Result<MaintenanceDto>> UpdateAsync(int id, MaintenanceUpdateDto dto)
    {
        var entity = await _db.MaintenanceRecords.FindAsync(id);
        if (entity == null) return Result<MaintenanceDto>.Fail(ErrorKind.NotFound, "Maintenance record not found.");
        if (entity.MaintenanceStatus == MaintenanceStatuses.Completed)
            return Result<MaintenanceDto>.Fail(ErrorKind.Conflict, "A completed maintenance record cannot be changed.");
        if (dto.EndDate < dto.StartDate)
            return Result<MaintenanceDto>.Fail(ErrorKind.Invalid, "End date cannot be before start date.");
        if (dto.Cost < 0)
            return Result<MaintenanceDto>.Fail(ErrorKind.Invalid, "Cost cannot be negative.");

        entity.Description = dto.Description;
        entity.StartDate = dto.StartDate.Date;
        entity.EndDate = dto.EndDate.Date;
        entity.Cost = dto.Cost;
        await _db.SaveChangesAsync();
        return Result<MaintenanceDto>.Success(ToDto(entity));
    }

    // BR-12 / TC-28
    public async Task<Result<MaintenanceDto>> CompleteAsync(int id, MaintenanceCompleteDto dto)
    {
        var entity = await _db.MaintenanceRecords.FindAsync(id);
        if (entity == null) return Result<MaintenanceDto>.Fail(ErrorKind.NotFound, "Maintenance record not found.");
        if (entity.MaintenanceStatus == MaintenanceStatuses.Completed)
            return Result<MaintenanceDto>.Fail(ErrorKind.Conflict, "This maintenance record is already completed.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        entity.MaintenanceStatus = MaintenanceStatuses.Completed;
        entity.EndDate = (dto.EndDate ?? DateTime.UtcNow).Date;
        await _db.SaveChangesAsync();

        if (dto.VehicleReady)
        {
            var otherInProgress = await _db.MaintenanceRecords.AnyAsync(m =>
                m.VehicleID == entity.VehicleID && m.MaintenanceID != id &&
                m.MaintenanceStatus == MaintenanceStatuses.InProgress);

            var vehicle = await _db.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.VehicleID == entity.VehicleID);
            if (!otherInProgress && vehicle != null && vehicle.VehicleStatus == VehicleStatuses.UnderMaintenance)
            {
                var r = await _vehicles.SetStatusAsync(entity.VehicleID, VehicleStatuses.Available);
                if (!r.Ok) return Result<MaintenanceDto>.Fail(r.Kind, r.Error!);
            }
        }
        // VehicleReady == false -> the vehicle stays Under Maintenance

        await tx.CommitAsync();
        return Result<MaintenanceDto>.Success(ToDto(entity));
    }

    public async Task<Result<List<MaintenanceDto>>> GetHistoryAsync(int vehicleId)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.VehicleID == vehicleId))
            return Result<List<MaintenanceDto>>.Fail(ErrorKind.NotFound, "Vehicle not found.");

        var list = await _db.MaintenanceRecords.AsNoTracking()
            .Where(m => m.VehicleID == vehicleId)
            .OrderByDescending(m => m.StartDate).ThenByDescending(m => m.MaintenanceID)
            .ToListAsync();
        return Result<List<MaintenanceDto>>.Success(list.Select(ToDto).ToList());
    }

    private static MaintenanceDto ToDto(Maintenance m) => new()
    {
        MaintenanceId = m.MaintenanceID,
        VehicleId = m.VehicleID,
        Description = m.Description,
        MaintenanceDate = m.MaintenanceDate,
        StartDate = m.StartDate,
        EndDate = m.EndDate,
        Cost = m.Cost,
        MaintenanceStatus = m.MaintenanceStatus
    };
}
