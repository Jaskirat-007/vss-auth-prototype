using Microsoft.EntityFrameworkCore;
using VSSAuthPrototype.Data;
using VSSAuthPrototype.Models;

namespace VSSAuthPrototype.Repositories
{
    public class StreamRepository : IStreamRepository
    {
        private readonly ApplicationDbContext _context;

        public StreamRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Stream?> GetByIdAsync(Guid id)
        {
            return await _context.Streams.FindAsync(id);
        }

        public async Task<Stream?> GetBySlugAsync(string slug)
        {
            return await _context.Streams
                .FirstOrDefaultAsync(s => s.Slug == slug && !s.IsDeleted);
        }

        public async Task<List<Stream>> GetAllActiveStreamsAsync()
        {
            return await _context.Streams
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Stream> CreateAsync(Stream stream)
        {
            _context.Streams.Add(stream);
            await _context.SaveChangesAsync();
            return stream;
        }

        public async Task<Stream> UpdateAsync(Stream stream)
        {
            stream.UpdatedAt = DateTime.UtcNow;
            _context.Streams.Update(stream);
            await _context.SaveChangesAsync();
            return stream;
        }
    }
}
