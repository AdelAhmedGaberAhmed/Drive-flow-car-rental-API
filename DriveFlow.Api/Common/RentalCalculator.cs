namespace DriveFlow.Api.Common;

public record SettlementNumbers(decimal ActualTotalDue, decimal Remaining, decimal Refund);

/// <summary>Every money / date formula of the project lives here (no database).</summary>
public static class RentalCalculator
{
    // FR-23 / FR-40: calendar days between the dates, minimum 1
    public static int CalculateDays(DateTime start, DateTime end)
        => Math.Max(1, (end.Date - start.Date).Days);

    // FR-24 / FR-45 / BR-05
    public static decimal CalculateAmount(int days, decimal pricePerDay)
        => days * pricePerDay;

    // FR-41 / BR-09
    public static int CalculateConsumedMileage(int pickupMileage, int returnMileage)
        => returnMileage - pickupMileage;

    // BR-01: end date exclusive
    public static bool PeriodsOverlap(DateTime startA, DateTime endA, DateTime startB, DateTime endB)
        => startA < endB && endA > startB;

    // FR-46 -> FR-49 / BR-06 -> BR-08
    public static SettlementNumbers CalculateSettlement(decimal actualRentalAmount,
                                                        decimal additionalCharges,
                                                        decimal netPaid)
    {
        var due = actualRentalAmount + additionalCharges;
        return new SettlementNumbers(due, Math.Max(0m, due - netPaid), Math.Max(0m, netPaid - due));
    }
}
