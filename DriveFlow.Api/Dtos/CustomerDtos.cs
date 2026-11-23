using System.ComponentModel.DataAnnotations;
namespace DriveFlow.Api.Dtos;

public class CustomerCreateDto
{
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [Required, MaxLength(20)]  public string PhoneNumber { get; set; } = "";
    [Required, EmailAddress, MaxLength(100)] public string Email { get; set; } = "";
    [Required, MaxLength(30)]  public string DriverLicenseNumber { get; set; } = "";
}

public class CustomerDto
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Email { get; set; } = "";
    public string DriverLicenseNumber { get; set; } = "";
}
