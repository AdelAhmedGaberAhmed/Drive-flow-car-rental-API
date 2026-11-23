using DriveFlow.Api.Common;
using DriveFlow.Api.Dtos;
using DriveFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveFlow.Api.Controllers;

[ApiController]
public class MaintenanceController : ControllerBase
{
    private readonly MaintenanceService _service;
    public MaintenanceController(MaintenanceService service) => _service = service;

    [HttpPost("api/maintenance")]
    public async Task<IActionResult> Create(MaintenanceCreateDto dto)
        => this.ToCreatedResult(await _service.CreateAsync(dto));

    [HttpPut("api/maintenance/{id:int}")]
    public async Task<IActionResult> Update(int id, MaintenanceUpdateDto dto)
        => this.ToActionResult(await _service.UpdateAsync(id, dto));

    [HttpPut("api/maintenance/{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, MaintenanceCompleteDto dto)
        => this.ToActionResult(await _service.CompleteAsync(id, dto));

    [HttpGet("api/vehicles/{vehicleId:int}/maintenance")]
    public async Task<IActionResult> History(int vehicleId)
        => this.ToActionResult(await _service.GetHistoryAsync(vehicleId));
}
