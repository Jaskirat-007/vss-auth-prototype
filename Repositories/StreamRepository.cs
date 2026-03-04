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

        public async Task<VssStream?> GetByIdAsync(Guid id)
        {
            return await _context.Streams.FindAsync(id);
        }

        public async Task<VssStream?> GetBySlugAsync(string slug)
        {
            return await _context.Streams
                .FirstOrDefaultAsync(s => s.Slug == slug && !s.IsDeleted);
        }

        public async Task<List<VssStream>> GetAllAsync()
        {
            return await _context.Streams
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<VssStream> CreateAsync(VssStream stream)
        {
            _context.Streams.Add(stream);
            await _context.SaveChangesAsync();
            return stream;
        }

        public async Task<VssStream> UpdateAsync(VssStream stream)
        {
            stream.UpdatedAt = DateTime.UtcNow;

            _context.Streams.Update(stream);
            await _context.SaveChangesAsync();

            return stream;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var stream = await _context.Streams.FindAsync(id);

            if (stream == null)
                return false;

            stream.IsDeleted = true;
            stream.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}