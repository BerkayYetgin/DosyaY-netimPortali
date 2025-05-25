using Microsoft.EntityFrameworkCore;
using dosyayonetim.Models;

using System.Linq.Expressions;

namespace dosyayonetim.Repositories
{
    public class FileRepository : GenericRepository<Models.File>
    {
        private readonly AppDbContext _context;
        private readonly UserStorageRepository _userStorageRepository;

        public FileRepository(AppDbContext context) : base(context)
        {
            _context = context;
            _userStorageRepository = new UserStorageRepository(context);
        }

        public async Task<Models.File> GetFileWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(f => f.Folder)
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<IEnumerable<Models.File>> GetFilesByFolderAsync(int folderId)
        {
            return await _dbSet
                .Where(f => f.FolderId == folderId && !f.IsDeleted)
                .ToListAsync();
        }

        public async Task<bool> IsFileNameExistsAsync(string name, int folderId)
        {
            return await _dbSet
                .AnyAsync(f => f.Name == name &&
                             f.FolderId == folderId &&
                             !f.IsDeleted);
        }

        private async Task<string> GetUniqueFileNameAsync(string originalName, int folderId)
        {
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalName);
            var extension = Path.GetExtension(originalName);
            var newName = originalName;
            var counter = 1;

            // Türkçe karakterleri ve özel karakterleri düzelt
            fileNameWithoutExt = fileNameWithoutExt.Replace("ı", "i")
                                                 .Replace("ğ", "g")
                                                 .Replace("ü", "u")
                                                 .Replace("ş", "s")
                                                 .Replace("ö", "o")
                                                 .Replace("ç", "c")
                                                 .Replace("İ", "I")
                                                 .Replace("Ğ", "G")
                                                 .Replace("Ü", "U")
                                                 .Replace("Ş", "S")
                                                 .Replace("Ö", "O")
                                                 .Replace("Ç", "C");

            // Özel karakterleri kaldır
            fileNameWithoutExt = new string(fileNameWithoutExt.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());

            while (await IsFileNameExistsAsync(newName, folderId))
            {
                newName = $"{fileNameWithoutExt}({counter}){extension}";
                counter++;
            }

            return newName;
        }

        public async Task<Models.File> UploadFileAsync(Models.File file, string userId)
        {
            // Check if user storage exists, if not create it
            var userStorage = await _userStorageRepository.GetByUserIdAsync(userId);
            if (userStorage == null)
            {
                userStorage = await _userStorageRepository.CreateUserStorageAsync(userId);
            }

            // Check user storage
            if (userStorage.UsedStorage + file.Size > userStorage.TotalStorage)
            {
                throw new InvalidOperationException("Insufficient storage space.");
            }

            // Get unique file name
            file.Name = await GetUniqueFileNameAsync(file.Name, file.FolderId);

            // Set FilePath with unique name
            file.FilePath = Path.Combine(userId, file.FolderId.ToString(), file.Name);

            // Set user ID and timestamps
            file.UserId = userId;
            file.CreatedAt = DateTime.UtcNow;
            file.UpdatedAt = DateTime.UtcNow;

            // Add file
            await AddAsync(file);

            // Update user storage
            userStorage.UsedStorage += file.Size;
            userStorage.UpdatedAt = DateTime.UtcNow;
            await _userStorageRepository.UpdateAsync(userStorage);

            return file;
        }

        public async Task UpdateFileAsync(int id, Models.File file, string userId)
        {
            var existingFile = await GetByIdAsync(id);
            if (existingFile == null)
            {
                throw new KeyNotFoundException("File not found.");
            }

            if (existingFile.UserId != userId)
            {
                throw new UnauthorizedAccessException("You don't have permission to update this file.");
            }

            // If file size changed, update user storage
            if (file.Size != existingFile.Size)
            {
                var userStorage = await _userStorageRepository.GetByUserIdAsync(userId);
                userStorage.UsedStorage = userStorage.UsedStorage - existingFile.Size + file.Size;

                if (userStorage.UsedStorage > userStorage.TotalStorage)
                {
                    throw new InvalidOperationException("Insufficient storage space.");
                }

                await _userStorageRepository.UpdateAsync(userStorage);
            }

            existingFile.Name = file.Name;
            existingFile.FilePath = file.FilePath;
            existingFile.FileType = file.FileType;
            existingFile.Size = file.Size;
            existingFile.UpdatedAt = DateTime.UtcNow;

            await UpdateAsync(existingFile);
        }

        public async Task DeleteFileAsync(int id, string userId)
        {
            var file = await GetByIdAsync(id);
            if (file == null)
            {
                throw new KeyNotFoundException("File not found.");
            }

            if (file.UserId != userId)
            {
                throw new UnauthorizedAccessException("You don't have permission to delete this file.");
            }

            // Get user storage
            var userStorage = await _userStorageRepository.GetByUserIdAsync(userId);
            if (userStorage != null)
            {
                // Update user storage
                userStorage.UsedStorage -= file.Size;
                userStorage.UpdatedAt = DateTime.UtcNow;
                await _userStorageRepository.UpdateAsync(userStorage);
            }

            // Soft delete
            file.IsDeleted = true;
            file.UpdatedAt = DateTime.UtcNow;
            await UpdateAsync(file);
        }

        public async Task<IEnumerable<Models.File>> SearchFilesAsync(string searchTerm, string userId)
        {
            return await _dbSet
                .Where(f => f.UserId == userId &&
                          f.Name.Contains(searchTerm) &&
                          !f.IsDeleted)
                .ToListAsync();
        }

        public async Task<IEnumerable<Models.File>> GetRecentFilesAsync(string userId, int count = 10)
        {
            return await _dbSet
                .Where(f => f.UserId == userId && !f.IsDeleted)
                .OrderByDescending(f => f.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Models.File>> GetFilesByTypeAsync(string fileType, string userId)
        {
            return await _dbSet
                .Where(f => f.UserId == userId &&
                          !f.IsDeleted &&
                          f.FileType == fileType)
                .ToListAsync();
        }

        // Dosya id'siyle dosya yolunu ve adını döndür
        public async Task<(string FilePath, string FileName, string FileType)> GetFilePathAndNameByIdAsync(int id)
        {
            var file = await _dbSet.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
            if (file == null)
                return (null, null, null);
            return (file.FilePath, file.Name, file.FileType);
        }
    }
}