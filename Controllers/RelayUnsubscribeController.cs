using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using ProcessZero.Infrastructure.Services;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Public, unauthenticated endpoint that the unsubscribe link in outgoing
    /// cold emails points to. The link carries a URL-safe token that encodes the
    /// campaign + lead, which we decode here and mark the lead as unsubscribed so
    /// the sequence engine immediately stops emailing them
    /// (ProcessSequenceAsync filters on RelayCampaignLead.Unsubscribed).
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    [Route("api/relay")]
    public class RelayUnsubscribeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RelayUnsubscribeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromQuery] string token)
        {
            if (!RelayEmailContentHelper.TryDecodeUnsubscribeToken(token, out var campaignId, out var leadId))
                return Content(BuildPage("This unsubscribe link is invalid or has expired."), "text/html");

            var campaignLeads = await _context.RelayCampaignLeads
                .Where(cl => cl.RelayCampaignId == campaignId && cl.RelayLeadId == leadId)
                .ToListAsync();

            foreach (var cl in campaignLeads)
            {
                cl.Unsubscribed = true;
                cl.Status = CampaignLeadStatus.Unsubscribed;
            }

            if (campaignLeads.Any())
                await _context.SaveChangesAsync();

            return Content(
                BuildPage("You've been unsubscribed. You will no longer receive these emails."),
                "text/html");
        }

        private static string BuildPage(string message)
        {
            return
                "<!DOCTYPE html><html><head><meta charset=\"utf-8\"/>" +
                "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>" +
                "<title>Unsubscribe</title></head>" +
                "<body style=\"font-family:Arial,Helvetica,sans-serif;background:#f6f6f6;margin:0;padding:40px\">" +
                "<div style=\"max-width:520px;margin:0 auto;background:#fff;border-radius:8px;padding:32px;text-align:center\">" +
                $"<h2 style=\"color:#333\">{message}</h2>" +
                "</div></body></html>";
        }
    }
}
