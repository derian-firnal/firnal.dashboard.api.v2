using firnal.dashboard.data;

namespace firnal.dashboard.repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmail(string? userId);
        Task<List<User>> GetAllUsers();
        Task<bool> UpdateUser(User user);
        Task<bool> DeleteUser(string userId);
    }
}
