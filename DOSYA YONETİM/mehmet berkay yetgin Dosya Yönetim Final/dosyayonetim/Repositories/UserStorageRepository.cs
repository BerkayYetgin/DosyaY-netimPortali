using Microsoft.EntityFrameworkCore;
using dosyayonetim.Models;

using System.Linq.Expressions;

namespace dosyayonetim.Repositories
{
    public class UserStorageRepository : GenericRepository<UserStorage>
    {
        public UserStorageRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<UserStorage> GetByUserIdAsync(string userId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(us => us.UserId == userId);
        }

        public async Task<UserStorage> CreateUserStorageAsync(string userId, long totalStorage = 5L * 1024 * 1024 * 1024)
        {
            var userStorage = new UserStorage
            {
                UserId = userId,
                TotalStorage = totalStorage,
                UsedStorage = 0,
                CreatedAt = DateTime.UtcNow
            };

            await AddAsync(userStorage);
            return userStorage;
        }

        public async Task UpdateStorageLimitAsync(string userId, long newTotalStorage)
        {
            var userStorage = await GetByUserIdAsync(userId);
            if (userStorage == null)
            {
                throw new KeyNotFoundException("User storage not found.");
            }

            if (newTotalStorage < userStorage.UsedStorage)
            {
                throw new InvalidOperationException("New storage limit cannot be less than used storage.");
            }

            userStorage.TotalStorage = newTotalStorage;
            userStorage.UpdatedAt = DateTime.UtcNow;

            await UpdateAsync(userStorage);
        }

        public async Task<bool> HasEnoughStorageAsync(string userId, long requiredSize)
        {
            var userStorage = await GetByUserIdAsync(userId);
            if (userStorage == null)
            {
                throw new KeyNotFoundException("User storage not found.");
            }

            return userStorage.TotalStorage - userStorage.UsedStorage >= requiredSize;
        }

        public async Task<long> GetAvailableStorageAsync(string userId)
        {
            var userStorage = await GetByUserIdAsync(userId);
            if (userStorage == null)
            {
                throw new KeyNotFoundException("User storage not found.");
            }

            return userStorage.TotalStorage - userStorage.UsedStorage;
        }

        public async Task<List<UserStorage>> GetActiveUserStoragesAsync()
        {
            return await _dbSet
                .Where(us => us.User != null && (us.User.GetType().GetProperty("IsDeleted") == null || !(bool)us.User.GetType().GetProperty("IsDeleted").GetValue(us.User)))
                .ToListAsync();
       }
    }
}