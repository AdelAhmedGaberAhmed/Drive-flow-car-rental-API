using DriveFlow.Api.Common;
using DriveFlow.Api.Dtos;
using DriveFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveFlow.Api.Controllers;

[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _service;
    public PaymentsController(PaymentService service) => _service = service;

    [HttpPost("api/bookings/{bookingId:int}/payments")]
    public async Task<IActionResult> Add(int bookingId, PaymentCreateDto dto)
        => this.ToCreatedResult(await _service.RecordPaymentAsync(bookingId, dto));

    [HttpGet("api/bookings/{bookingId:int}/payments")]
    public async Task<IActionResult> History(int bookingId)
        => this.ToActionResult(await _service.GetHistoryAsync(bookingId));
}
