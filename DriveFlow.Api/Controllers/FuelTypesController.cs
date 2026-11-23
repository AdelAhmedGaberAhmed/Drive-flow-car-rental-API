using DriveFlow.Api.Common;
using DriveFlow.Api.Dtos;
using DriveFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveFlow.Api.Controllers;

[ApiController]
[Route("api/fuel-types")]
public class FuelTypesController : ControllerBase
{
    private readonly LookupService _service;
    public FuelTypesController(LookupService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetFuelTypesAsync());

    [HttpPost]
    public async Task<IActionResult> Create(FuelTypeSaveDto dto)
        => this.ToCreatedResult(await _service.CreateFuelTypeAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, FuelTypeSaveDto dto)
        => this.ToActionResult(await _service.UpdateFuelTypeAsync(id, dto));
}
