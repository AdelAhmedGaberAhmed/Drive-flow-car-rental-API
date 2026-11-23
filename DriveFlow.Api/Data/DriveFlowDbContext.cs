using DriveFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveFlow.Api.Data;

public class DriveFlowDbContext : DbContext
{
    public DriveFlowDbContext(DbContextOptions<DriveFlowDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<VehicleCategory> VehicleCategories => Set<VehicleCategory>();
    public DbSet<FuelType> FuelTypes => Set<FuelType>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<RentalBooking> RentalBookings => Set<RentalBooking>();
    public DbSet<RentalPayment> RentalPayments => Set<RentalPayment>();
    public DbSet<VehicleReturn> VehicleReturns => Set<VehicleReturn>();
    public DbSet<RentalSettlement> RentalSettlements => Set<RentalSettlement>();
    public DbSet<AdditionalCharge> AdditionalCharges => Set<AdditionalCharge>();
    public DbSet<Maintenance> MaintenanceRecords => Set<Maintenance>();

    protected override void ConfigureConventions(ModelConfigurationBuilder cb)
    {
        cb.Properties<decimal>().HavePrecision(10, 2);   // decimal(10,2) like the PDF
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Customer>(e => { e.ToTable("Customer"); e.HasKey(x => x.CustomerID); });
        b.Entity<VehicleCategory>(e => { e.ToTable("VehicleCategories"); e.HasKey(x => x.CategoryID); });
        b.Entity<FuelType>(e =>
        {
            e.ToTable("FuelTypes"); e.HasKey(x => x.FuelTypeID);
            e.Property(x => x.FuelTypeName).HasColumnName("FuelType");
        });
        b.Entity<Vehicle>(e => { e.ToTable("Vehicle"); e.HasKey(x => x.VehicleID); });
        b.Entity<RentalBooking>(e =>
        {
            e.ToTable("RentalBooking"); e.HasKey(x => x.BookingID);
            e.Property(x => x.RentalStartDate).HasColumnType("date");
            e.Property(x => x.RentalEndDate).HasColumnType("date");
        });
        b.Entity<RentalPayment>(e => { e.ToTable("RentalPayments"); e.HasKey(x => x.PaymentID); });
        b.Entity<VehicleReturn>(e => { e.ToTable("VehicleReturns"); e.HasKey(x => x.ReturnID); });
        b.Entity<RentalSettlement>(e => { e.ToTable("RentalSettlement"); e.HasKey(x => x.SettlementID); });
        b.Entity<AdditionalCharge>(e => { e.ToTable("AdditionalCharges"); e.HasKey(x => x.ChargeID); });
        b.Entity<Maintenance>(e =>
        {
            e.ToTable("Maintenance"); e.HasKey(x => x.MaintenanceID);
            e.Property(x => x.MaintenanceDate).HasColumnType("date");
            e.Property(x => x.StartDate).HasColumnType("date");
            e.Property(x => x.EndDate).HasColumnType("date");
        });
    }
}
