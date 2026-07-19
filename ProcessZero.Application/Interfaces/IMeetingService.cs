using ProcessZero.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface IMeetingService
    {
        Task AddMeetingAsync(MeetingDto meetingDto, string notes);

        Task<MeetingDto> GetMeetingByIdAsync(int id);

        Task<List<MeetingDto>> GetAllMeetingsAsync();

        // Paginated versions
        Task<List<MeetingDto>> GetAllMeetingsAsync(int page, int pageSize);

        Task<List<MeetingDto>> GetAllMeetingsByUserIdAsync(string userId);

        Task<List<MeetingDto>> GetAllMeetingsByUserIdAsync(string userId, int page, int pageSize);

        Task UpdateMeetingAsync(MeetingDto meetingDto, string notes);

        Task DeleteMeetingAsync(int id, string notes);
    }
}
