using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Dtos
{
    public class TwilioSmsDto
    {
        public string PhoneNumber { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }

    public class TwilioWhatsAppDto
    {
        public string PhoneNumber { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }

    public class TwilioFacebookDto
    {
        public string RecipientId { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}
