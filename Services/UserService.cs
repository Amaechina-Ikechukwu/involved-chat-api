using Involved_Chat.Data;
using Involved_Chat.Models;
using Involved_Chat.DTOS;
using MongoDB.Driver;

namespace Involved_Chat.Services
{
    public class UserService
    {
        private readonly MongoDbContext _context;

        public UserService(MongoDbContext context)
        {
            _context = context;
        }
          

        public async Task UpdateStatusAsync(string userId, bool isOnline, DateTime? lastSeen, string connectionId)
{
    var update = Builders<User>.Update
        .Set(u => u.IsOnline, isOnline)
        .Set(u => u.LastSeen, lastSeen ?? DateTime.UtcNow);

    if (isOnline)
        update = update.AddToSet(u => u.ConnectionIds, connectionId);
    else
        update = update.Pull(u => u.ConnectionIds, connectionId);

    await _context.Users.UpdateOneAsync(u => u.Id == userId, update);
}

        public async Task UpdatePhotoUrlAsync(string userId, string photoUrl)
        {
            var update = Builders<User>.Update.Set(u => u.PhotoURL, photoUrl);
            await _context.Users.UpdateOneAsync(u => u.Id == userId, update);
        }

        public async Task UpdateAboutAsync(string userId, string about)
        {
            var update = Builders<User>.Update.Set(u => u.About, about);
            await _context.Users.UpdateOneAsync(u => u.Id == userId, update);
        }

        public async Task UpdateDisplayNameAsync(string userId, string displayName)
        {
            if (displayName == null) displayName = string.Empty;
            var update = Builders<User>.Update.Set(u => u.DisplayName, displayName);
            await _context.Users.UpdateOneAsync(u => u.Id == userId, update);
        }

        public async Task BlockUserAsync(string userId, string blockUserId)
        {
            var update = Builders<User>.Update.AddToSet(u => u.BlockedUsers, blockUserId);
            await _context.Users.UpdateOneAsync(u => u.Id == userId, update);
        }

        public async Task UnblockUserAsync(string userId, string unblockUserId)
        {
            var update = Builders<User>.Update.Pull(u => u.BlockedUsers, unblockUserId);
            await _context.Users.UpdateOneAsync(u => u.Id == userId, update);
        }

        public async Task<UserDto?> GetUserInfoAsync(string userId)
        {
            var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                DisplayName = user.DisplayName,
                PhotoURL = user.PhotoURL,
                IsOnline = user.IsOnline,
                LastSeen = user.LastSeen,
                Status = user.Status,
                Contacts = user.Contacts,
                PushTokens = user.PushTokens,
                Location = user.Location == null ? null : new DTOS.LocationDto 
                { 
                    Latitude = user.Location.Latitude, 
                    Longitude = user.Location.Longitude 
                },
                ConnectionIds = user.ConnectionIds,
                About = user.About,
                BlockedUsers = user.BlockedUsers
            };
        }

        public async Task AddPushTokenAsync(string userId, string pushToken)
        {
            if (string.IsNullOrWhiteSpace(pushToken)) return;
            var update = Builders<User>.Update.AddToSet(u => u.PushTokens, pushToken);
            await _context.Users.UpdateOneAsync(u => u.Id == userId, update);
        }

        public async Task RemovePushTokenAsync(string userId, string pushToken)
        {
            if (string.IsNullOrWhiteSpace(pushToken)) return;
            var update = Builders<User>.Update.Pull(u => u.PushTokens, pushToken);
            await _context.Users.UpdateOneAsync(u => u.Id == userId, update);
        }

        public async Task UpdateLocationAsync(string userId, double? latitude, double? longitude)
        {
            var location = new UserLocation { Latitude = latitude, Longitude = longitude };
            var update = Builders<User>.Update.Set(u => u.Location, location);
            await _context.Users.UpdateOneAsync(u => u.Id == userId, update);
        }

        // Add contact mutually (both users add each other)
        public async Task AddContactAsync(string userId, string contactId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(contactId) || userId == contactId)
                return;

            // Add contactId to userId's contacts
            var updateUser = Builders<User>.Update.AddToSet(u => u.Contacts, contactId);
            await _context.Users.UpdateOneAsync(u => u.Id == userId, updateUser);

            // Add userId to contactId's contacts
            var updateContact = Builders<User>.Update.AddToSet(u => u.Contacts, userId);
            await _context.Users.UpdateOneAsync(u => u.Id == contactId, updateContact);
        }

        // Return list of user ids the user has exchanged messages with (either sender or receiver)
        public async Task<List<string>> GetContactsAsync(string userId)
        {
            // Query Messages collection for distinct other party ids
            var sentFilter = Builders<Message>.Filter.Eq(m => m.SenderId, userId);
            var receivedFilter = Builders<Message>.Filter.Eq(m => m.ReceiverId, userId);
            var combined = Builders<Message>.Filter.Or(sentFilter, receivedFilter);

            var collection = _context.Messages;

            // Project the other party id per message
            var projection = Builders<Message>.Projection.Expression(m => m.SenderId == userId ? m.ReceiverId : m.SenderId);

            var list = await collection.Find(combined).Project(projection).ToListAsync();

            // distinct and remove nulls/own id if present
            return list.Where(id => !string.IsNullOrEmpty(id) && id != userId).Distinct().ToList();
        }

        public async Task<PaginatedUserResponse> GetNearbyUsersAsync(string userId, int page, int pageSize)
        {
            var currentUser = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (currentUser?.Location?.Latitude == null || currentUser?.Location?.Longitude == null)
            {
                return new PaginatedUserResponse { Users = new List<NearbyUserDto>() };
            }

            // Get all users with location data (exclude current user)
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Ne(u => u.Id, userId),
                Builders<User>.Filter.Ne(u => u.Location, null)
            );

            var allUsers = await _context.Users.Find(filter).ToListAsync();

            // Calculate distances and filter by 10km radius
            var maxDistanceMeters = 10000.0; // 10km
            var nearbyUsersWithDistance = allUsers
                .Where(u => u.Location != null) // Additional null check for safety
                .Select(u => new
                {
                    User = u,
                    Distance = GetDistance(currentUser.Location, u.Location!)
                })
                .Where(x => x.Distance >= 0 && x.Distance <= maxDistanceMeters)
                .OrderBy(x => x.Distance)
                .ToList();

            var total = nearbyUsersWithDistance.Count;

            // Apply pagination
            var paginatedUsers = nearbyUsersWithDistance
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new NearbyUserDto
                {
                    Id = x.User.Id,
                    Username = x.User.Username,
                    DisplayName = x.User.DisplayName,
                    PhotoURL = x.User.PhotoURL,
                    Distance = x.Distance
                })
                .ToList();

            return new PaginatedUserResponse
            {
                Users = paginatedUsers,
                Page = page,
                PageSize = pageSize,
                Total = total,
                HasNextPage = (page * pageSize) < total
            };
        }

        public async Task<int> GetAllUsersWithLocationCountAsync()
        {
            var filter = Builders<User>.Filter.Ne(u => u.Location, null);
            return (int)await _context.Users.CountDocumentsAsync(filter);
        }

        private double GetDistance(UserLocation loc1, UserLocation loc2)
        {
            if (loc1?.Latitude == null || loc1?.Longitude == null || loc2?.Latitude == null || loc2?.Longitude == null)
            {
                return -1;
            }
            
            var lat1 = loc1.Latitude.Value;
            var lon1 = loc1.Longitude.Value;
            var lat2 = loc2.Latitude.Value;
            var lon2 = loc2.Longitude.Value;
            
            var d1 = lat1 * (Math.PI / 180.0);
            var num1 = lon1 * (Math.PI / 180.0);
            var d2 = lat2 * (Math.PI / 180.0);
            var num2 = lon2 * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);
            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
        }
    }
}