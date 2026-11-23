using DriveFlow.Api.Common;
using DriveFlow.Api.Data;
using DriveFlow.Api.Dtos;
using DriveFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveFlow.Api.Services;

/// <summary>Vehicle categories + fuel types.</summary>
public class LookupService
{
    private readonly DriveFlowDbContext _db;
    public LookupService(DriveFlowDbContext db) => _db = db;

    // ---------------- Categories ----------------
    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var list = await _db.VehicleCategories.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync();
        return list.Select(CategoryToDto).ToList();
    }

    public async Task<Result<CategoryDto>> CreateCategoryAsync(CategorySaveDto dto)
    {
        if (await _db.VehicleCategories.AnyAsync(c => c.CategoryName == dto.CategoryName))
            return Result<CategoryDto>.Fail(ErrorKind.Conflict, "A category with this name already exists.");

        var entity = new VehicleCategory { CategoryName = dto.CategoryName };
        _db.VehicleCategories.Add(entity);
        await _db.SaveChangesAsync();
        return Result<CategoryDto>.Success(CategoryToDto(entity));
    }

    public async Task<Result<CategoryDto>> UpdateCategoryAsync(int id, CategorySaveDto dto)
    {
        var entity = await _db.VehicleCategories.FindAsync(id);
        if (entity == null) return Result<CategoryDto>.Fail(ErrorKind.NotFound, "Category not found.");

        if (await _db.VehicleCategories.AnyAsync(c => c.CategoryName == dto.CategoryName && c.CategoryID != id))
            return Result<CategoryDto>.Fail(ErrorKind.Conflict, "A category with this name already exists.");

        entity.CategoryName = dto.CategoryName;
        await _db.SaveChangesAsync();
        return Result<CategoryDto>.Success(CategoryToDto(entity));
    }

    public async Task<Result<bool>> DeleteCategoryAsync(int id)
    {
        var entity = await _db.VehicleCategories.FindAsync(id);
        if (entity == null) return Result<bool>.Fail(ErrorKind.NotFound, "Category not found.");

        if (await _db.Vehicles.AnyAsync(v => v.CategoryID == id))
            return Result<bool>.Fail(ErrorKind.Conflict, "This category is used by vehicles and cannot be deleted.");

        _db.VehicleCategories.Remove(entity);
        await _db.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    // ---------------- Fuel types ----------------
    public async Task<List<FuelTypeDto>> GetFuelTypesAsync()
    {
        var list = await _db.FuelTypes.AsNoTracking().OrderBy(f => f.FuelTypeName).ToListAsync();
        return list.Select(FuelToDto).ToList();
    }

    public async Task<Result<FuelTypeDto>> CreateFuelTypeAsync(FuelTypeSaveDto dto)
    {
        if (await _db.FuelTypes.AnyAsync(f => f.FuelTypeName == dto.FuelType))
            return Result<FuelTypeDto>.Fail(ErrorKind.Conflict, "This fuel type already exists.");

        var entity = new FuelType { FuelTypeName = dto.FuelType };
        _db.FuelTypes.Add(entity);
        await _db.SaveChangesAsync();
        return Result<FuelTypeDto>.Success(FuelToDto(entity));
    }

    public async Task<Result<FuelTypeDto>> UpdateFuelTypeAsync(int id, FuelTypeSaveDto dto)
    {
        var entity = await _db.FuelTypes.FindAsync(id);
        if (entity == null) return Result<FuelTypeDto>.Fail(ErrorKind.NotFound, "Fuel type not found.");

        if (await _db.FuelTypes.AnyAsync(f => f.FuelTypeName == dto.FuelType && f.FuelTypeID != id))
            return Result<FuelTypeDto>.Fail(ErrorKind.Conflict, "This fuel type already exists.");

        entity.FuelTypeName = dto.FuelType;
        await _db.SaveChangesAsync();
        return Result<FuelTypeDto>.Success(FuelToDto(entity));
    }

    private static CategoryDto CategoryToDto(VehicleCategory c) => new() { CategoryId = c.CategoryID, CategoryName = c.CategoryName };
    private static FuelTypeDto FuelToDto(FuelType f) => new() { FuelTypeId = f.FuelTypeID, FuelType = f.FuelTypeName };
}
