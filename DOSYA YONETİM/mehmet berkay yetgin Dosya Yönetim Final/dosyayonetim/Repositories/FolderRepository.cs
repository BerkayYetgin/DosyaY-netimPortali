using Microsoft.EntityFrameworkCore;
using dosyayonetim.Models;
using System.Linq.Expressions;
using dosyayonetim.DTOs;

namespace dosyayonetim.Repositories
{
    public class FolderRepository : GenericRepository<Folder>
    {
        public FolderRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Folder> GetFolderWithContentAsync(int id)
        {
            return await _dbSet
                .Include(f => f.Files)
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
        }

        public async Task<IEnumerable<Folder>> GetRootFoldersAsync(string userId)
        {
            return await _dbSet
                .Where(f => f.UserId == userId && !f.IsDeleted)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> IsFolderNameExistsAsync(string name, string userId)
        {
            return await _dbSet
                .AnyAsync(f => f.Name == name && f.UserId == userId && !f.IsDeleted);
        }

        public async Task<Folder> CreateFolderAsync(FolderCreateDTO folderDto, string userId)
        {
            if (await IsFolderNameExistsAsync(folderDto.Name, userId))
            {
                throw new InvalidOperationException("A folder with this name already exists.");
            }

            var folder = new Folder
            {
                Name = folderDto.Name,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await AddAsync(folder);
            return folder;
        }

        public async Task UpdateFolderAsync(int id, FolderUpdateDTO folderDto, string userId)
        {
            var folder = await GetByIdAsync(id);
            if (folder == null || folder.IsDeleted)
            {
                throw new KeyNotFoundException("Folder not found.");
            }

            if (folder.UserId != userId)
            {
                throw new UnauthorizedAccessException("You don't have permission to update this folder.");
            }

            if (await IsFolderNameExistsAsync(folderDto.Name, userId))
            {
                throw new InvalidOperationException("A folder with this name already exists.");
            }

            folder.Name = folderDto.Name;
            folder.UpdatedAt = DateTime.UtcNow;

            await UpdateAsync(folder);
        }

        public async Task DeleteFolderAsync(int id, string userId)
        {
            var folder = await GetByIdAsync(id);
            if (folder == null || folder.IsDeleted)
            {
                throw new KeyNotFoundException("Folder not found.");
            }

            if (folder.UserId != userId)
            {
                throw new UnauthorizedAccessException("You don't have permission to delete this folder.");
            }

            // Klasör altındaki dosyaları da soft delete yap
            var files = await _context.Files.Where(f => f.FolderId == id && !f.IsDeleted).ToListAsync();
            foreach (var file in files)
            {
                file.IsDeleted = true;
                file.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();

            // Soft delete
            folder.IsDeleted = true;
            folder.UpdatedAt = DateTime.UtcNow;
            await UpdateAsync(folder);
        }

        public async Task<IEnumerable<Folder>> SearchFoldersAsync(string searchTerm, string userId)
        {
            return await _dbSet
                .Where(f => f.UserId == userId &&
                          f.Name.Contains(searchTerm) &&
                          !f.IsDeleted)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<long> GetFolderSizeAsync(int id)
        {
            var folder = await GetFolderWithContentAsync(id);
            if (folder == null)
            {
                throw new KeyNotFoundException("Folder not found.");
            }

            return folder.Files.Sum(f => f.Size);
        }

        // Kullanıcının tüm dosyalarının toplam boyutunu döndür
        public async Task<long> GetUserTotalFileSizeAsync(string userId)
        {
            // User'ın silinmiş olup olmadığını kontrol et
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || (user.GetType().GetProperty("IsDeleted") != null && (bool)user.GetType().GetProperty("IsDeleted").GetValue(user)))
            {
                return 0;
            }
            return await _context.Files
                .Where(f => f.UserId == userId && !f.IsDeleted)
                .SumAsync(f => (long?)f.Size) ?? 0;
        }
    }
}