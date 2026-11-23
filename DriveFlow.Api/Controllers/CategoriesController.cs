using DriveFlow.Api.Common;
using DriveFlow.Api.Dtos;
using DriveFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveFlow.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly LookupService _service;
    public CategoriesController(LookupService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetCategoriesAsync());

    [HttpPost]
    public async Task<IActionResult> Create(CategorySaveDto dto)
        => this.ToCreatedResult(await _service.CreateCategoryAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CategorySaveDto dto)
        => this.ToActionResult(await _service.UpdateCategoryAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteCategoryAsync(id);
        return result.Ok ? NoContent() : this.ToActionResult(result);
    }
}
