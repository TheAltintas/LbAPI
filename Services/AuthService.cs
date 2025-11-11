using LittleBeaconAPI.Data;
using LittleBeaconAPI.Models;

namespace LittleBeaconAPI.Services
{
    public interface IAuthService
    {
        LoginResponse Authenticate(string username, string password);
        bool ValidateToken(string token);
        string? GetUsernameFromToken(string token);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly Dictionary<string, string> _tokens = new();

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public LoginResponse Authenticate(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Forkert brugernavn eller adgangskode"
                };
            }

            if (!string.Equals(user.Password, password, StringComparison.Ordinal))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Forkert brugernavn eller adgangskode"
                };
            }

            // Generate simple token (in real app, use JWT)
            var token = Guid.NewGuid().ToString();
            _tokens[token] = user.Username;

            return new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                Username = user.Username
            };
        }

        public bool ValidateToken(string token)
        {
            return _tokens.ContainsKey(token);
        }

        public string? GetUsernameFromToken(string token)
        {
            return _tokens.TryGetValue(token, out var username) ? username : null;
        }
    }
}
