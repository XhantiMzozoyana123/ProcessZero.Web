using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ProcessZero.Application.Dtos
{
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "User ID is required.")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Reset token is required.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
