using TPai.Models;

namespace TPai.Repositories;

public interface ICarRepository : IRepository<Car>
{
    /// <summary>Returns true when at least one car belongs to the given category.</summary>
    Task<bool> AnyInCategoryAsync(int categoryId);

    /// <summary>Returns true when another car already uses this plate number.</summary>
    Task<bool> PlateNumberExistsAsync(string plateNumber, int? excludeCarId = null);
}
