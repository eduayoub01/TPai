using Microsoft.EntityFrameworkCore;
using TPai.Data;
using TPai.Models;

namespace TPai.Repositories;

public class CarRepository : ICarRepository
{
    private readonly RentCarDbContext _context;

    public CarRepository(RentCarDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Car>> GetAllAsync()
    {
        return await _context.Cars
            .Include(c => c.CarCategory)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Car?> GetByIdAsync(int id)
    {
        return await _context.Cars
            .Include(c => c.CarCategory)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Car> AddAsync(Car entity)
    {
        _context.Cars.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(Car entity)
    {
        _context.Cars.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car is not null)
        {
            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();
        }
    }

    public Task<bool> AnyInCategoryAsync(int categoryId)
    {
        return _context.Cars.AnyAsync(c => c.CarCategoryId == categoryId);
    }

    public Task<bool> PlateNumberExistsAsync(string plateNumber, int? excludeCarId = null)
    {
        return _context.Cars.AnyAsync(c =>
            c.PlateNumber == plateNumber && (excludeCarId == null || c.Id != excludeCarId));
    }
}
