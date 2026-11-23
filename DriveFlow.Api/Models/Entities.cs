namespace DriveFlow.Api.Models;

// Plain data classes that mirror the 10 tables in the database. These are used by EF Core to map to the database tables.

public class Customer
{
    public int CustomerID { get; set; }
    public string Name { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Email { get; set; } = "";
    public string DriverLicenseNumber { get; set; } = "";
}

public class VehicleCategory
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = "";
}

public class FuelType
{
    public int FuelTypeID { get; set; }
    public string FuelTypeName { get; set; } = "";   // column name in DB: FuelType
}

public class Vehicle
{
    public int VehicleID { get; set; }
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public int Year { get; set; }
    public int Mileage { get; set; }
    public int FuelTypeID { get; set; }
    public string PlateNumber { get; set; } = "";
    public int CategoryID { get; set; }
    public decimal RentalPricePerDay { get; set; }
    public string VehicleStatus { get; set; } = "";
}

public class RentalBooking
{
    public int BookingID { get; set; }
    public int CustomerID { get; set; }
    public int VehicleID { get; set; }
    public DateTime BookingDate { get; set; }
    public DateTime RentalStartDate { get; set; }
    public DateTime RentalEndDate { get; set; }
    public string PickupLocation { get; set; } = "";
    public string DropoffLocation { get; set; } = "";
    public int InitialRentalDays { get; set; }
    public decimal RentalPricePerDay { get; set; }
    public decimal InitialTotalDueAmount { get; set; }
    public DateTime? ActualPickupDate { get; set; }
    public int? PickupMileage { get; set; }
    public string? InitialCheckNotes { get; set; }
    public string BookingStatus { get; set; } = "";
    public DateTime? CancellationDate { get; set; }
    public string? CancellationReason { get; set; }
}

public class RentalPayment
{
    public int PaymentID { get; set; }
    public int BookingID { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal PaymentAmount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string PaymentType { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public string? PaymentDetails { get; set; }
}

public class VehicleReturn
{
    public int ReturnID { get; set; }
    public int BookingID { get; set; }
    public DateTime ActualReturnDate { get; set; }
    public int ActualRentalDays { get; set; }
    public int ReturnMileage { get; set; }
    public int ConsumedMileage { get; set; }
    public string? FinalCheckNotes { get; set; }
}

public class RentalSettlement
{
    public int SettlementID { get; set; }
    public int ReturnID { get; set; }
    public decimal ActualRentalAmount { get; set; }
    public decimal AdditionalChargesAmount { get; set; }
    public decimal ActualTotalDueAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public DateTime SettlementDate { get; set; }
}

public class AdditionalCharge
{
    public int ChargeID { get; set; }
    public int ReturnID { get; set; }
    public string ChargeType { get; set; } = "";
    public string ChargeDescription { get; set; } = "";
    public decimal ChargeAmount { get; set; }
    public DateTime ChargeDate { get; set; }
}

public class Maintenance
{
    public int MaintenanceID { get; set; }
    public int VehicleID { get; set; }
    public string Description { get; set; } = "";
    public DateTime MaintenanceDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Cost { get; set; }
    public string MaintenanceStatus { get; set; } = "";
}
