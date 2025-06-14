using firnal.dashboard.data;

namespace firnal.dashboard.services.Interfaces
{
    public interface IAuthService
    {
        Task<string?> RegisterUser(string email, string username, string password, string role, List<string>? schemas);
        Task<User?> AuthenticateUser(string email, string password);
    }
}
