using Microsoft.EntityFrameworkCore;
using TPai.Data;
using TPai.Models;

namespace TPai.Repositories;

public class CarCategoryRepository : ICarCategoryRepository
{
    private readonly RentCarDbContext _context;

    public CarCategoryRepository(RentCarDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CarCategory>> GetAllAsync()
    {
        return await _context.CarCategories
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<CarCategory?> GetByIdAsync(int id)
    {
        return await _context.CarCategories.FindAsync(id);
    }

    public async Task<CarCategory> AddAsync(CarCategory entity)
    {
        _context.CarCategories.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(CarCategory entity)
    {
        _context.CarCategories.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _context.CarCategories.FindAsync(id);
        if (category is not null)
        {
            _context.CarCategories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }
}
