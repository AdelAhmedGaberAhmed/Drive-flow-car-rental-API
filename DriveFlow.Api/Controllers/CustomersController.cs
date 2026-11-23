using DriveFlow.Api.Common;
using DriveFlow.Api.Dtos;
using DriveFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveFlow.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _service;
    public CustomersController(CustomerService service) => _service = service;

    [HttpGet]                          // GET /api/customers?search=ali
    public async Task<IActionResult> Search([FromQuery] string? search)
        => Ok(await _service.SearchAsync(search));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
        => this.ToActionResult(await _service.GetAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create(CustomerCreateDto dto)
        => this.ToCreatedResult(await _service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CustomerCreateDto dto)
        => this.ToActionResult(await _service.UpdateAsync(id, dto));
}
