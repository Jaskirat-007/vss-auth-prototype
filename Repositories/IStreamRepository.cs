using VSSAuthPrototype.Models;

namespace VSSAuthPrototype.Repositories
{
    public interface IStreamRepository
    {
        Task<List<VssStream>> GetAllAsync();
        Task<VssStream?> GetByIdAsync(Guid id);
        Task<VssStream> CreateAsync(VssStream stream);
        Task<VssStream> UpdateAsync(VssStream stream);
        Task<bool> DeleteAsync(Guid id);
    }
}
