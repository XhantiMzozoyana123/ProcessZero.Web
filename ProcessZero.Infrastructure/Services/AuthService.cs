using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;

namespace ProcessZero.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AuthService(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _context = context;
        }

        // Register user and assign default claims
        public async Task<string> RegisterAsync(RegisterDto model)
        {
            var user = new ApplicationUser { UserName = model.UserName.Replace(" ", "_"), Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.SetTwoFactorEnabledAsync(user, true);
                return "User registered successfully";
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception(errors);
        }

        // Updated LoginAsync method to use async JWT with account lockout support
        public async Task<(string token, string userId, bool requires2FA)> LoginAsync(LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                throw new Exception("Invalid login attempt.");

            // Check if the account is currently locked out
            if (await _userManager.IsLockedOutAsync(user))
                throw new Exception("Account is temporarily locked due to multiple failed login attempts. Please try again later.");

            if (await _userManager.CheckPasswordAsync(user, model.Password))
            {
                // Reset failed access count on successful login
                await _userManager.ResetAccessFailedCountAsync(user);

                var token = await GenerateJwtTokenAsync(user);
                return (token, user.Id, false);
            }

            // Increment failed access count — may trigger lockout
            await _userManager.AccessFailedAsync(user);

            throw new Exception("Invalid login attempt.");
        }

        // Async version of JWT generation
        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var baseClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var identityClaims = await _userManager.GetClaimsAsync(user);
            baseClaims.AddRange(identityClaims);
            // Include role claims so authorization policies that require roles work
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                baseClaims.Add(new Claim(ClaimTypes.Role, role));
                // also add common role claim names to maximize compatibility with different clients
                baseClaims.Add(new Claim("role", role));
                baseClaims.Add(new Claim("roles", role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: baseClaims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> UpdateUserAsync(UserDto userUpdateDto)
        {
            var user = await _userManager.FindByIdAsync(userUpdateDto.Id);
            if (user == null) return "User not found.";

            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, userUpdateDto.CurrentPassword, false);
            if (!passwordCheck.Succeeded) return "Current password is incorrect.";

            if (user.Email != userUpdateDto.NewEmail)
            {
                var emailUpdateResult = await _userManager.SetEmailAsync(user, userUpdateDto.NewEmail);
                if (!emailUpdateResult.Succeeded)
                {
                    var errors = string.Join(", ", emailUpdateResult.Errors.Select(e => e.Description));
                    return errors;
                }
            }

            if (!string.IsNullOrWhiteSpace(userUpdateDto.NewPassword))
            {
                var passwordChangeResult = await _userManager.ChangePasswordAsync(user, userUpdateDto.CurrentPassword, userUpdateDto.NewPassword);
                if (!passwordChangeResult.Succeeded)
                {
                    var errors = string.Join(", ", passwordChangeResult.Errors.Select(e => e.Description));
                    return errors;
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            return "User details updated successfully.";
        }

        public async Task<UserDto> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            return new UserDto
            {
                UserName = user.UserName,
                NewEmail = user.Email
            };
        }

        public async Task<string> GenerateTwoFactorTokenAsync(ApplicationUser user)
        {
            return await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
        }

        public async Task<bool> VerifyTwoFactorTokenAsync(ApplicationUser user, string token)
        {
            return await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, token);
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"];
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"]);
                var enableSsl = bool.Parse(_configuration["Email:EnableSsl"]);
                var emailAddress = _configuration["Email:EmailAddress"];
                var password = _configuration["Email:Password"];

                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(emailAddress, "Process Zero");
                    mail.To.Add(email);
                    mail.Subject = subject;
                    mail.Body = message;
                    mail.IsBodyHtml = true;

                    using (SmtpClient smtpServer = new SmtpClient(smtpHost))
                    {
                        smtpServer.Port = smtpPort;
                        smtpServer.Credentials = new System.Net.NetworkCredential(emailAddress, password);
                        smtpServer.EnableSsl = enableSsl;

                        await smtpServer.SendMailAsync(mail);
                    }
                }
            }
            catch (SmtpException ex)
            {
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
        }

        public async Task SendTwoFactorCodeAsync(ApplicationUser user)
        {
            var token = await GenerateTwoFactorTokenAsync(user);
            await SendEmailAsync(user.Email, "Your 2FA Code", $"Your 2FA code is: {token}");
        }

        public async Task<string> VerifyTwoFactorCodeAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            var isValid = await VerifyTwoFactorTokenAsync(user, token);
            if (!isValid) throw new Exception("Invalid 2FA token.");

            return await GenerateJwtTokenAsync(user);
        }

        public async Task<string> ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            // Always return the same message to prevent user enumeration
            if (user == null)
                return "If an account with that email exists, a password reset link has been sent.";

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = $"{_configuration["App:Url"]}/reset-password?token={Uri.EscapeDataString(token)}&id={Uri.EscapeDataString(user.Id)}";

            await SendEmailAsync(user.Email, "Password Reset", $"To reset your password, click the following link: {resetLink}");

            return "If an account with that email exists, a password reset link has been sent.";
        }

        public async Task<string> ResetPasswordAsync(ResetPasswordDto model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) throw new Exception("User not found.");

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (result.Succeeded) return "Password reset successfully.";

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception(errors);
        }
    }
}
