using DriveFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveFlow.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _service;
    public ReportsController(ReportService service) => _service = service;

    private IActionResult? CheckRange(DateTime? from, DateTime? to)
        => from.HasValue && to.HasValue && from > to
            ? BadRequest(new { message = "'from' cannot be after 'to'." })
            : null;

    [HttpGet("bookings")]
    public async Task<IActionResult> Bookings([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => CheckRange(from, to) ?? Ok(await _service.GetBookingsReportAsync(from, to));

    [HttpGet("payments")]
    public async Task<IActionResult> Payments([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => CheckRange(from, to) ?? Ok(await _service.GetPaymentsReportAsync(from, to));

    [HttpGet("vehicles")]
    public async Task<IActionResult> Vehicles()
        => Ok(await _service.GetVehiclesReportAsync());

    [HttpGet("returns")]
    public async Task<IActionResult> Returns([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => CheckRange(from, to) ?? Ok(await _service.GetReturnsReportAsync(from, to));

    [HttpGet("maintenance")]
    public async Task<IActionResult> Maintenance([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => CheckRange(from, to) ?? Ok(await _service.GetMaintenanceReportAsync(from, to));
}
