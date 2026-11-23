using DriveFlow.Api.Common;
using DriveFlow.Api.Data;
using DriveFlow.Api.Dtos;
using DriveFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveFlow.Api.Services;

public class CustomerService
{
    private readonly DriveFlowDbContext _db;
    public CustomerService(DriveFlowDbContext db) => _db = db;

    public async Task<List<CustomerDto>> SearchAsync(string? search)
    {
        var query = _db.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Name.Contains(search)
                                  || c.PhoneNumber.Contains(search)
                                  || c.DriverLicenseNumber.Contains(search));
        }

        var list = await query.OrderBy(c => c.Name).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<Result<CustomerDto>> GetAsync(int id)
    {
        var c = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerID == id);
        return c == null
            ? Result<CustomerDto>.Fail(ErrorKind.NotFound, "Customer not found.")
            : Result<CustomerDto>.Success(ToDto(c));
    }

    public async Task<Result<CustomerDto>> CreateAsync(CustomerCreateDto dto)
    {
        if (await _db.Customers.AnyAsync(c => c.DriverLicenseNumber == dto.DriverLicenseNumber))
            return Result<CustomerDto>.Fail(ErrorKind.Conflict, "A customer with this driver license number already exists.");

        var entity = new Customer
        {
            Name = dto.Name,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            DriverLicenseNumber = dto.DriverLicenseNumber
        };
        _db.Customers.Add(entity);
        await _db.SaveChangesAsync();
        return Result<CustomerDto>.Success(ToDto(entity));
    }

    public async Task<Result<CustomerDto>> UpdateAsync(int id, CustomerCreateDto dto)
    {
        var entity = await _db.Customers.FindAsync(id);
        if (entity == null)
            return Result<CustomerDto>.Fail(ErrorKind.NotFound, "Customer not found.");

        var taken = await _db.Customers.AnyAsync(c => c.DriverLicenseNumber == dto.DriverLicenseNumber && c.CustomerID != id);
        if (taken)
            return Result<CustomerDto>.Fail(ErrorKind.Conflict, "A customer with this driver license number already exists.");

        entity.Name = dto.Name;
        entity.PhoneNumber = dto.PhoneNumber;
        entity.Email = dto.Email;
        entity.DriverLicenseNumber = dto.DriverLicenseNumber;
        await _db.SaveChangesAsync();
        return Result<CustomerDto>.Success(ToDto(entity));
    }

    private static CustomerDto ToDto(Customer c) => new()
    {
        CustomerId = c.CustomerID,
        Name = c.Name,
        PhoneNumber = c.PhoneNumber,
        Email = c.Email,
        DriverLicenseNumber = c.DriverLicenseNumber
    };
}
