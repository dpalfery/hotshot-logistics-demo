using HotshotLogistics.Domain.Entities;

namespace HotshotLogistics.Contracts.Repositories
{
    public interface IDriverRepository : IRepository<Driver>
    {
        Task<IEnumerable<Driver>> GetDriversAsync(CancellationToken cancellationToken = default);
        Task<Driver?> GetDriverByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Driver> CreateDriverAsync(Driver driver, CancellationToken cancellationToken = default);
        Task<bool> DeleteDriverAsync(int id, CancellationToken cancellationToken = default);
        Task<Driver> UpdateDriverAsync(Driver driver, CancellationToken cancellationToken = default);
    }
}
