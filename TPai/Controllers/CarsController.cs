using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPai.Models;
using TPai.Repositories;

namespace TPai.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CarsController : ControllerBase
{
    private readonly ICarRepository _carRepository;
    private readonly ICarCategoryRepository _categoryRepository;
    private readonly IAuditLogRepository _logRepository;

    public CarsController(
        ICarRepository carRepository,
        ICarCategoryRepository categoryRepository,
        IAuditLogRepository logRepository)
    {
        _carRepository = carRepository;
        _categoryRepository = categoryRepository;
        _logRepository = logRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Car>>> GetAll()
    {
        var cars = await _carRepository.GetAllAsync();
        return Ok(cars);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Car>> GetById(int id)
    {
        var car = await _carRepository.GetByIdAsync(id);
        if (car is null)
        {
            return NotFound($"Car #{id} was not found.");
        }
        return Ok(car);
    }

    [HttpPost]
    public async Task<ActionResult<Car>> Create(CarCreateDto dto)
    {
        var validationError = await ValidateAsync(dto.Model, dto.PlateNumber, dto.CarCategoryId, null);
        if (validationError is { } createError)
        {
            return ErrorResult(createError);
        }

        var car = new Car
        {
            Model = dto.Model.Trim(),
            PlateNumber = dto.PlateNumber.Trim(),
            CarCategoryId = dto.CarCategoryId
        };

        var created = await _carRepository.AddAsync(car);

        await _logRepository.LogAsync("CREATE", "Car", $"Created car {created.Model} ({created.PlateNumber})", User.Identity?.Name ?? "Unknown");

        // Reload so the response also contains the category name.
        var fullCar = await _carRepository.GetByIdAsync(created.Id) ?? created;
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, fullCar);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CarUpdateDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest("Id mismatch.");
        }

        var existing = await _carRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound($"Car #{id} was not found.");
        }

        var validationError = await ValidateAsync(dto.Model, dto.PlateNumber, dto.CarCategoryId, id);
        if (validationError is { } updateError)
        {
            return ErrorResult(updateError);
        }

        existing.Model = dto.Model.Trim();
        existing.PlateNumber = dto.PlateNumber.Trim();
        existing.CarCategoryId = dto.CarCategoryId;

        try
        {
            await _carRepository.UpdateAsync(existing);
        }
        catch (DbUpdateException)
        {
            return Conflict("The car could not be saved because the data conflicts with another record.");
        }

        await _logRepository.LogAsync("UPDATE", "Car", $"Updated car #{id} ({existing.Model}, {existing.PlateNumber})", User.Identity?.Name ?? "Unknown");
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _carRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound($"Car #{id} was not found.");
        }

        await _carRepository.DeleteAsync(id);
        await _logRepository.LogAsync("DELETE", "Car", $"Deleted car #{id} ({existing.Model}, {existing.PlateNumber})", User.Identity?.Name ?? "Unknown");
        return NoContent();
    }

    private static ActionResult ErrorResult((int StatusCode, string Message) error)
    {
        return error.StatusCode == StatusCodes.Status409Conflict
            ? new ConflictObjectResult(error.Message)
            : new BadRequestObjectResult(error.Message);
    }

    private async Task<(int StatusCode, string Message)?> ValidateAsync(
        string? model, string? plateNumber, int categoryId, int? carId)
    {
        var trimmedModel = (model ?? string.Empty).Trim();
        var trimmedPlate = (plateNumber ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(trimmedModel))
        {
            return (StatusCodes.Status400BadRequest, "Model is required.");
        }

        if (trimmedModel.Length > 100)
        {
            return (StatusCodes.Status400BadRequest, "Model must be 100 characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(trimmedPlate))
        {
            return (StatusCodes.Status400BadRequest, "Plate number is required.");
        }

        if (trimmedPlate.Length > 20)
        {
            return (StatusCodes.Status400BadRequest, "Plate number must be 20 characters or fewer.");
        }

        var category = await _categoryRepository.GetByIdAsync(categoryId);
        if (category is null)
        {
            return (StatusCodes.Status400BadRequest,
                $"Car category #{categoryId} does not exist. Create the category first.");
        }

        if (await _carRepository.PlateNumberExistsAsync(trimmedPlate, carId))
        {
            return (StatusCodes.Status409Conflict, $"A car with plate \"{trimmedPlate}\" already exists.");
        }

        return null;
    }
}
