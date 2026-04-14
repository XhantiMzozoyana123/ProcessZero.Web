using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Dtos
{
    public class MeetingDto
    {
        public string UserId { get; set; } = string.Empty; // select the user who is booking the meeting by using the UserService getbyId

        public Contact Contact { get; set; } = new Contact(); // select the client who we are booking the meeting with by using the ClientService getbyUserId

        public Product Product { get; set; } = new Product(); // Select the product we are selling to the client by using ProductService getbyId

        public Meeting Meeting { get; set; } = new Meeting();
    }
}
