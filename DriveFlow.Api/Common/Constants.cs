namespace DriveFlow.Api.Common;

public static class BookingStatuses
{
    public const string Confirmed = "Confirmed", Active = "Active",
                        Completed = "Completed", Cancelled = "Cancelled";
}

public static class VehicleStatuses
{
    public const string Available = "Available",
                        Booked = "Booked",                     // diagram only; code never writes it
                        Rented = "Rented",
                        UnderInspection = "Under Inspection",  // diagram only
                        UnderMaintenance = "Under Maintenance",
                        Retired = "Retired";
}

public static class PaymentTypes
{
    public const string Initial = "Initial", Balance = "Balance", Refund = "Refund";
    public static readonly string[] All = { Initial, Balance, Refund };
}

public static class PaymentMethods
{
    public const string Cash = "Cash", Card = "Card", BankTransfer = "BankTransfer";
    public static readonly string[] All = { Cash, Card, BankTransfer };
}

public static class PaymentStatuses
{
    public const string Completed = "Completed";
}

public static class ChargeTypes
{
    public const string Damage = "Damage", Cleaning = "Cleaning", LateFee = "LateFee",
                        Fuel = "Fuel", Other = "Other";
    public static readonly string[] All = { Damage, Cleaning, LateFee, Fuel, Other };
}

public static class MaintenanceStatuses
{
    public const string Scheduled = "Scheduled", InProgress = "In Progress", Completed = "Completed";
}
