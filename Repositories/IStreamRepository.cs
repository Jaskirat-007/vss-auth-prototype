using VSSAuthPrototype.Models;

namespace VSSAuthPrototype.Repositories
{
    public interface IStreamRepository
    {
        Task<Stream?> GetByIdAsync(Guid id);
        Task<Stream?> GetBySlugAsync(string slug);
        Task<List<Stream>> GetAllActiveStreamsAsync();
        Task<Stream> CreateAsync(Stream stream);
        Task<Stream> UpdateAsync(Stream stream);
    }
}
