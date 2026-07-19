using ProcessZero.Application.Dtos;
using ProcessZero.Domain;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterDto model);
        Task<(string token, string userId, bool requires2FA)> LoginAsync(LoginDto model);
        Task<string> UpdateUserAsync(UserDto userUpdateDto);
        Task<UserDto> GetUserByIdAsync(string userId);
        Task<string> GenerateTwoFactorTokenAsync(ApplicationUser user);
        Task<bool> VerifyTwoFactorTokenAsync(ApplicationUser user, string token);
        Task SendEmailAsync(string email, string subject, string message);
        Task SendTwoFactorCodeAsync(ApplicationUser user);
        Task<string> VerifyTwoFactorCodeAsync(string userId, string token);
        Task<string> ForgotPasswordAsync(string email);
        Task<string> ResetPasswordAsync(ResetPasswordDto model);
        Task<string> GenerateImpersonationTokenAsync(string userId);
    }
}
