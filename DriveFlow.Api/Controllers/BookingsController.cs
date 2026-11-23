using DriveFlow.Api.Common;
using DriveFlow.Api.Dtos;
using DriveFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveFlow.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly BookingService _service;
    public BookingsController(BookingService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Create(BookingCreateDto dto)
        => this.ToCreatedResult(await _service.CreateAsync(dto));

    [HttpGet]                                   // GET /api/bookings?customerId=&vehicleId=&status=
    public async Task<IActionResult> List([FromQuery] BookingFilterDto filter)
        => Ok(await _service.ListAsync(filter));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
        => this.ToActionResult(await _service.GetAsync(id));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, BookingUpdateDto dto)
        => this.ToActionResult(await _service.UpdateAsync(id, dto));

    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, BookingCancelDto dto)
        => this.ToActionResult(await _service.CancelAsync(id, dto));

    [HttpPut("{id:int}/pickup")]
    public async Task<IActionResult> Pickup(int id, BookingPickupDto dto)
        => this.ToActionResult(await _service.RecordPickupAsync(id, dto));
}
