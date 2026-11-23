using DriveFlow.Api.Common;
using DriveFlow.Api.Dtos;
using DriveFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveFlow.Api.Controllers;

[ApiController]
public class ReturnsController : ControllerBase
{
    private readonly ReturnService _service;
    public ReturnsController(ReturnService service) => _service = service;

    // One call: return + charges + settlement
    [HttpPost("api/returns/settle")]
    public async Task<IActionResult> Settle(SettleRequestDto dto)
        => this.ToActionResult(await _service.ProcessReturnAsync(dto));

    [HttpGet("api/bookings/{bookingId:int}/return")]
    public async Task<IActionResult> GetByBooking(int bookingId)
        => this.ToActionResult(await _service.GetByBookingAsync(bookingId));

    [HttpGet("api/returns/{returnId:int}/charges")]
    public async Task<IActionResult> GetCharges(int returnId)
        => this.ToActionResult(await _service.GetChargesAsync(returnId));
}
