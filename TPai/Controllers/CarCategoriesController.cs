using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPai.Models;
using TPai.Repositories;

namespace TPai.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CarCategoriesController : ControllerBase
{
    private readonly ICarCategoryRepository _categoryRepository;
    private readonly ICarRepository _carRepository;
    private readonly IAuditLogRepository _logRepository;

    public CarCategoriesController(
        ICarCategoryRepository categoryRepository,
        ICarRepository carRepository,
        IAuditLogRepository logRepository)
    {
        _categoryRepository = categoryRepository;
        _carRepository = carRepository;
        _logRepository = logRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CarCategory>>> GetAll()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CarCategory>> GetById(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is null)
        {
            return NotFound($"Category #{id} was not found.");
        }
        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CarCategory>> Create(CarCategory category)
    {
        var name = (category.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Category name is required.");
        }

        if (name.Length > 100)
        {
            return BadRequest("Category name must be 100 characters or fewer.");
        }

        var categories = await _categoryRepository.GetAllAsync();
        if (categories.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict($"A category named \"{name}\" already exists.");
        }

        category.Name = name;
        var created = await _categoryRepository.AddAsync(category);
        await _logRepository.LogAsync("CREATE", "CarCategory", $"Created category {created.Name}", User.Identity?.Name ?? "Unknown");
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CarCategory category)
    {
        if (id != category.Id)
        {
            return BadRequest("Id mismatch.");
        }

        var name = (category.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Category name is required.");
        }

        if (name.Length > 100)
        {
            return BadRequest("Category name must be 100 characters or fewer.");
        }

        var existing = await _categoryRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound($"Category #{id} was not found.");
        }

        var categories = await _categoryRepository.GetAllAsync();
        if (categories.Any(c => c.Id != id && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict($"A category named \"{name}\" already exists.");
        }

        existing.Name = name;
        await _categoryRepository.UpdateAsync(existing);
        await _logRepository.LogAsync("UPDATE", "CarCategory", $"Updated category #{id} ({existing.Name})", User.Identity?.Name ?? "Unknown");
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _categoryRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound($"Category #{id} was not found.");
        }

        if (await _carRepository.AnyInCategoryAsync(id))
        {
            return Conflict(
                $"Cannot delete category \"{existing.Name}\" because it still has cars assigned to it. " +
                "Move those cars to another category or delete them first.");
        }

        try
        {
            await _categoryRepository.DeleteAsync(id);
        }
        catch (DbUpdateException)
        {
            return Conflict(
                $"Cannot delete category \"{existing.Name}\" because it is still referenced by other records.");
        }

        await _logRepository.LogAsync("DELETE", "CarCategory", $"Deleted category #{id} ({existing.Name})", User.Identity?.Name ?? "Unknown");
        return NoContent();
    }
}
