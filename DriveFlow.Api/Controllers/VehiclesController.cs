using DriveFlow.Api.Common;
using DriveFlow.Api.Dtos;
using DriveFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveFlow.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly VehicleService _service;
    public VehiclesController(VehicleService service) => _service = service;

    [HttpGet]                                   // GET /api/vehicles?categoryId=&fuelTypeId=&status=
    public async Task<IActionResult> List([FromQuery] VehicleFilterDto filter)
        => Ok(await _service.ListAsync(filter));

    [HttpGet("available")]                      // GET /api/vehicles/available?start=&end=&categoryId=&fuelTypeId=
    public async Task<IActionResult> GetAvailable([FromQuery] AvailableVehiclesQueryDto query)
        => this.ToActionResult(await _service.GetAvailableAsync(query));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
        => this.ToActionResult(await _service.GetAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create(VehicleSaveDto dto)
        => this.ToCreatedResult(await _service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, VehicleSaveDto dto)
        => this.ToActionResult(await _service.UpdateAsync(id, dto));

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(int id, VehicleStatusDto dto)
        => this.ToActionResult(await _service.ChangeStatusAsync(id, dto.Status));
}
