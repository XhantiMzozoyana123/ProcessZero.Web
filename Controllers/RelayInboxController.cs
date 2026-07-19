using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain.Entities;
using System.Security.Claims;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// API Controller for managing email replies from leads through the relay inbox system.
    /// Handles syncing, retrieving, and managing email replies received through relay accounts.
    /// 
    /// ===== ENTITIES AND CLASSES USED =====
    /// 
    /// 1. RelayEmailReply (ProcessZero.Domain.Entities)
    ///    Purpose: Stores email replies received from leads in LeadLake through relay accounts
    ///    Key Columns:
    ///      - Id (int): Primary key
    ///      - RelayEmailAccountId (int): Foreign key to RelayEmailAccount
    ///      - LeadLakeId (int): Foreign key to LeadLake
    ///      - MessageId (string): Gmail message ID
    ///      - FromEmail (string): Sender's email address (max 256 chars)
    ///      - Subject (string): Email subject
    ///      - Body (string): Email body content
    ///      - ReceivedDate (DateTime): When the email was received
    ///      - IsRead (bool): Read status flag
    ///      - UserId (string): Sales rep user ID who owns this reply (max 450 chars)
    ///      - Tags (string): Comma-separated tags for categorization (max 1000 chars)
    ///      - CreatedAt (DateTime): Record creation timestamp
    ///      - UpdatedAt (DateTime): Last update timestamp
    ///    Navigation Properties:
    ///      - Lead (LeadLake): Related lead information
    ///      - RelayEmailAccount (RelayEmailAccount): Related relay email account
    /// 
    /// 2. LeadLake (ProcessZero.Domain.Entities)
    ///    Purpose: Contains lead information synced from external sources
    ///    Key Columns Used:
    ///      - Id (int): Primary key
    ///      - FirstName (string): Lead's first name
    ///      - LastName (string): Lead's last name
    ///      - Email (string): Lead's email address
    ///      - Phone (string): Lead's phone number
    ///      - Company (string): Lead's company name
    ///      - Job (string): Lead's job title
    ///      - Location (string): Lead's location
    ///      - UserId (string): Associated sales rep user ID
    /// 
    /// 3. RelayEmailAccount (ProcessZero.Domain.Entities)
    ///    Purpose: Stores Gmail account credentials and settings for relay email system
    ///    Key Columns Used:
    ///      - Id (int): Primary key
    ///      - Email (string): Gmail account email address
    ///      - AccessToken (string): OAuth access token for Gmail API
    ///      - RefreshToken (string): OAuth refresh token for token renewal
    ///      - IsActive (bool): Whether the account is active
    ///      - UserId (string): Associated sales rep user ID
    /// 
    /// 4. Contact (ProcessZero.Domain.Entities)
    ///    Purpose: Sales rep contact table (created from email replies via admin upsert)
    ///    Key Columns:
    ///      - Id (int): Primary key
    ///      - UserId (string): Sales rep user ID who owns this contact
    ///      - FirstName (string): Contact's first name
    ///      - LastName (string): Contact's last name
    ///      - Email (string): Contact's email address (used for duplicate detection)
    ///      - Phone (string): Contact's phone number
    ///      - Company (string): Contact's company
    ///      - Job (string): Contact's job title
    ///      - Location (string): Contact's location
    ///      - Status (ContactStatus enum): Contact status (Reached, FollowUp, Converted, Active, Deactivated)
    ///      - ClosedAmount (decimal): Closed deal amount
    ///      - CreatedAt (DateTime): Record creation timestamp
    ///      - UpdatedAt (DateTime): Last update timestamp
    /// 
    /// 5. ContactStatus (Enum)
    ///    Purpose: Enum for contact status values
    ///    Values: Reached, FollowUp, Converted, Active, Deactivated
    /// 
    /// 6. ReceivedEmailMessage (ProcessZero.Application.Interfaces)
    ///    Purpose: Data transfer object for email messages received from Gmail
    ///    Key Properties:
    ///      - MessageId (string): Gmail message ID
    ///      - From (string): Sender's email/name
    ///      - Subject (string): Email subject
    ///      - Body (string): Email body
    ///      - ReceivedDate (DateTime): When email was received
    ///      - IsRead (bool): Read status
    /// 
    /// 7. IRelayInboxService (ProcessZero.Application.Interfaces)
    ///    Purpose: Service interface for relay inbox operations
    ///    Key Methods:
    ///      - GetUnreadRepliesAsync: Fetch unread emails for user
    ///      - GetRepliesByLeadAsync: Fetch all emails from specific lead
    ///      - GetRepliesByRelayAccountAsync: Fetch emails from relay account (paginated)
    ///      - GetEmailReplyByIdAsync: Get specific email reply by ID
    ///      - MarkAsReadAsync: Mark single email as read
    ///      - MarkMultipleAsReadAsync: Mark multiple emails as read
    ///      - AddTagsAsync: Add categorization tags to email
    ///      - SyncEmailRepliesAsync: Sync emails from single relay account via Gmail API
    ///      - SyncAllEmailRepliesAsync: Sync emails from all active relay accounts
    ///      - SearchRepliesAsync: Search emails by subject/body/sender
    ///      - UpsertEmailRecipientContactAsync: Create or update contact from email reply
    /// 
    /// ===== RELATIONSHIPS =====
    /// 
    /// RelayEmailReply → LeadLake: ForeignKey(LeadLakeId), CascadeDelete
    /// RelayEmailReply → RelayEmailAccount: ForeignKey(RelayEmailAccountId), CascadeDelete
    /// Contact ← RelayEmailReply: Contact created from RelayEmailReply data (Admin Upsert)
    /// 
    /// ===== INDEXES FOR PERFORMANCE =====
    /// 
    /// RelayEmailReply Indexes:
    ///   - IX_RelayEmailReplies_UserId: Fast lookup by sales rep
    ///   - IX_RelayEmailReplies_LeadLakeId: Fast lookup by lead
    ///   - IX_RelayEmailReplies_RelayEmailAccountId: Fast lookup by relay account
    ///   - IX_RelayEmailReplies_FromEmail: Fast lookup by sender
    ///   - IX_RelayEmailReplies_MessageId: Prevent duplicate Gmail messages
    ///   - IX_RelayEmailReplies_IsRead: Filter unread emails
    ///   - IX_RelayEmailReplies_UserId_IsRead: Composite for user unread emails
    ///   - IX_RelayEmailReplies_LeadLakeId_ReceivedDate: Lead emails ordered by date (descending)
    /// 
    /// Contact Indexes:
    ///   - IX_Contacts_UserId: Fast lookup by sales rep owner
    ///   - IX_Contacts_Status: Filter by contact status
    /// 
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class RelayInboxController : ControllerBase
    {
        private readonly IRelayInboxService _relayInboxService;
        private readonly ILogger<RelayInboxController> _logger;

        public RelayInboxController(
            IRelayInboxService relayInboxService,
            ILogger<RelayInboxController> logger)
        {
            _relayInboxService = relayInboxService ?? throw new ArgumentNullException(nameof(relayInboxService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the current user's ID from the authentication claim.
        /// </summary>
        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        /// <summary>
        /// Gets all unread email replies for the current user.
        /// 
        /// Database Operation:
        ///   - SELECT * FROM RelayEmailReplies 
        ///   - WHERE UserId = currentUserId AND IsRead = false
        ///   - ORDER BY ReceivedDate DESC
        /// 
        /// Uses RelayEmailReply Entity:
        ///   - Filters by: UserId, IsRead
        ///   - Returns: Id, MessageId, SenderEmail, Subject, Body, ReceivedAt, IsRead, Tags, Lead (navigation)
        /// </summary>
        /// <response code="200">List of unread email replies</response>
        /// <response code="401">User not authenticated</response>
        [HttpGet("unread")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUnreadReplies(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in claims");

                var replies = await _relayInboxService.GetUnreadRepliesAsync(userId, cancellationToken);
                return Ok(replies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching unread email replies");
                return BadRequest($"Error fetching unread replies: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all email replies from a specific lead.
        /// 
        /// Uses: RelayEmailReply entity
        /// Filters: LeadLakeId matches parameter
        /// Returns: List of RelayEmailReply objects ordered by received date (descending)
        /// </summary>
        /// <param name="leadLakeId">The ID of the lead (from LeadLake entity)</param>
        /// <response code="200">List of email replies from the lead</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authenticated</response>
        [HttpGet("lead/{leadLakeId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetRepliesByLead(int leadLakeId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (leadLakeId <= 0)
                    return BadRequest("Invalid lead ID");

                var replies = await _relayInboxService.GetRepliesByLeadAsync(leadLakeId, cancellationToken);
                return Ok(replies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching email replies for lead {LeadId}", leadLakeId);
                return BadRequest($"Error fetching replies: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets email replies for a specific relay email account with pagination.
        /// 
        /// Database Operation:
        ///   - SELECT * FROM RelayEmailReplies 
        ///   - WHERE RelayEmailAccountId = relayAccountId
        ///   - ORDER BY ReceivedDate DESC
        ///   - SKIP (pageNumber - 1) * pageSize
        ///   - TAKE pageSize
        /// 
        /// Uses RelayEmailReply Entity:
        ///   - Filters by: RelayEmailAccountId
        ///   - Includes: LeadLake navigation property
        ///   - Returns: Paginated results sorted by ReceivedDate descending
        ///   - Default pagination: pageNumber=1, pageSize=20
        /// </summary>
        /// <param name="relayAccountId">The ID of the relay email account (RelayEmailAccount.Id)</param>
        /// <param name="pageNumber">Page number (optional, defaults to 1)</param>
        /// <param name="pageSize">Page size (optional, defaults to 20)</param>
        /// <response code="200">List of email replies for the relay account</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authenticated</response>
        [HttpGet("relay/{relayAccountId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetRepliesByRelayAccount(
            int relayAccountId,
            int? pageNumber = null,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (relayAccountId <= 0)
                    return BadRequest("Invalid relay account ID");

                var replies = await _relayInboxService.GetRepliesByRelayAccountAsync(
                    relayAccountId, pageNumber, pageSize, cancellationToken);

                return Ok(replies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching email replies for relay account {RelayAccountId}", relayAccountId);
                return BadRequest($"Error fetching replies: {ex.Message}");
            }
        }

        /// <summary>
        /// Marks an email reply as read.
        /// 
        /// Database Operation:
        ///   - SELECT RelayEmailReply WHERE Id = emailReplyId
        ///   - UPDATE RelayEmailReply SET IsRead = true, UpdatedAt = NOW()
        /// 
        /// Uses RelayEmailReply Entity:
        ///   - Locates: Id
        ///   - Updates: IsRead (bool), UpdatedAt (DateTime)
        /// </summary>
        /// <param name="emailReplyId">The ID of the email reply (RelayEmailReply.Id)</param>
        /// <response code="200">Email reply marked as read</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authenticated</response>
        [HttpPut("{emailReplyId:int}/mark-read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkAsRead(int emailReplyId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (emailReplyId <= 0)
                    return BadRequest("Invalid email reply ID");

                await _relayInboxService.MarkAsReadAsync(emailReplyId, cancellationToken);
                return Ok("Email reply marked as read");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking email reply as read: {EmailReplyId}", emailReplyId);
                return BadRequest($"Error marking reply as read: {ex.Message}");
            }
        }

        /// <summary>
        /// Marks multiple email replies as read.
        /// 
        /// Database Operation:
        ///   - SELECT RelayEmailReply WHERE Id IN (emailReplyIds list)
        ///   - UPDATE all selected records SET IsRead = true, UpdatedAt = NOW()
        /// 
        /// Uses RelayEmailReply Entity:
        ///   - Locates: Id (multiple)
        ///   - Updates: IsRead (bool), UpdatedAt (DateTime) for all matching records
        /// 
        /// Request Body Example:
        ///   [1, 2, 3, 4, 5]
        /// </summary>
        /// <param name="emailReplyIds">List of email reply IDs (RelayEmailReply.Id values)</param>
        /// <response code="200">Email replies marked as read</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authenticated</response>
        [HttpPut("mark-read-batch")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkMultipleAsRead(
            [FromBody] List<int> emailReplyIds,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (emailReplyIds == null || emailReplyIds.Count == 0)
                    return BadRequest("No email reply IDs provided");

                await _relayInboxService.MarkMultipleAsReadAsync(emailReplyIds, cancellationToken);
                return Ok($"Marked {emailReplyIds.Count} email replies as read");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking multiple email replies as read");
                return BadRequest($"Error marking replies as read: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds tags to an email reply for categorization.
        /// 
        /// Database Operation:
        ///   - SELECT RelayEmailReply WHERE Id = emailReplyId
        ///   - Parse existing Tags column (comma-separated string)
        ///   - Add new tags to the set (avoid duplicates)
        ///   - UPDATE RelayEmailReply SET Tags = concatenated_tags, UpdatedAt = NOW()
        /// 
        /// Uses RelayEmailReply Entity:
        ///   - Locates: Id
        ///   - Reads: Tags (string, max 1000 chars, comma-separated values)
        ///   - Updates: Tags (string), UpdatedAt (DateTime)
        /// 
        /// Tag Format: Stored as comma-separated string (e.g., "VIP,Urgent,FollowUp")
        /// Duplicates: New tags are merged into existing tags, duplicates removed
        /// 
        /// Request Body Example:
        ///   ["VIP", "Urgent", "FollowUp"]
        /// </summary>
        /// <param name="emailReplyId">The ID of the email reply (RelayEmailReply.Id)</param>
        /// <param name="tags">List of tags to add to the email reply</param>
        /// <response code="200">Tags added to email reply</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authenticated</response>
        [HttpPut("{emailReplyId:int}/tags")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AddTags(
            int emailReplyId,
            [FromBody] List<string> tags,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (emailReplyId <= 0)
                    return BadRequest("Invalid email reply ID");

                if (tags == null || tags.Count == 0)
                    return BadRequest("No tags provided");

                await _relayInboxService.AddTagsAsync(emailReplyId, tags, cancellationToken);
                return Ok("Tags added to email reply");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding tags to email reply: {EmailReplyId}", emailReplyId);
                return BadRequest($"Error adding tags: {ex.Message}");
            }
        }

        /// <summary>
        /// Syncs email replies from a relay account by fetching recent messages from Gmail.
        /// 
        /// Process:
        ///   1. Fetch RelayEmailAccount by Id to get OAuth credentials
        ///   2. Call Gmail API using AccessToken to fetch recent messages
        ///   3. For each message received:
        ///      - Extract FromEmail using regex pattern
        ///      - Query LeadLake to find matching lead by Email and UserId
        ///      - If lead found and message not already in RelayEmailReplies (check by MessageId):
        ///        * INSERT new RelayEmailReply record
        ///      - If lead not found or already synced: skip message
        /// 
        /// Database Operations:
        ///   - SELECT RelayEmailAccount WHERE Id = relayAccountId
        ///   - SELECT LeadLake WHERE Email = fromEmail AND UserId = account.UserId
        ///   - SELECT RelayEmailReply WHERE MessageId = message.MessageId (duplicate check)
        ///   - INSERT RelayEmailReply (multiple records, one per new message)
        /// 
        /// Entities Used:
        ///   - RelayEmailAccount: Source of Gmail credentials
        ///     * Uses: Id, Email, AccessToken, RefreshToken, IsActive, UserId
        ///   - LeadLake: Matching against received emails
        ///     * Uses: Id, Email, UserId (filter for correct sales rep)
        ///   - RelayEmailReply: Stores synced messages
        ///     * Inserted columns: RelayEmailAccountId, LeadLakeId, MessageId, FromEmail, Subject, Body, ReceivedDate, IsRead, UserId, Tags
        ///   - ReceivedEmailMessage: Gmail API response DTO
        /// </summary>
        /// <param name="relayAccountId">The ID of the relay email account (RelayEmailAccount.Id)</param>
        /// <param name="maxResults">Maximum number of messages to fetch (optional, defaults to 20, max 100)</param>
        /// <response code="200">Email replies synced successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authenticated</response>
        [HttpPost("relay/{relayAccountId:int}/sync")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SyncEmailReplies(
            int relayAccountId,
            int maxResults = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (relayAccountId <= 0)
                    return BadRequest("Invalid relay account ID");

                if (maxResults <= 0 || maxResults > 100)
                    return BadRequest("maxResults must be between 1 and 100");

                await _relayInboxService.SyncEmailRepliesAsync(relayAccountId, maxResults, cancellationToken);
                return Ok("Email replies synced successfully");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing email replies for relay account: {RelayAccountId}", relayAccountId);
                return BadRequest($"Error syncing replies: {ex.Message}");
            }
        }

        /// <summary>
        /// Syncs email replies from all relay email accounts by fetching recent messages from Gmail.
        /// 
        /// Process:
        ///   1. Query all active RelayEmailAccount records (IsActive = true, AccessToken != null)
        ///   2. For each account, execute SyncEmailRepliesAsync
        ///   3. If any account sync fails, continue with others and track errors
        ///   4. If all accounts fail, throw exception; otherwise return success
        /// 
        /// Database Operations:
        ///   - SELECT RelayEmailAccount WHERE IsActive = true AND AccessToken IS NOT NULL
        ///   - For each account: (same as SyncEmailReplies)
        ///     * SELECT LeadLake WHERE Email matches
        ///     * SELECT RelayEmailReply WHERE MessageId matches
        ///     * INSERT RelayEmailReply for new messages
        /// 
        /// Entities Used:
        ///   - RelayEmailAccount: All active accounts
        ///     * Filters: IsActive = true, AccessToken not null
        ///   - LeadLake, RelayEmailReply: (same as individual sync)
        /// </summary>
        /// <param name="maxResults">Maximum number of messages to fetch per account (optional, defaults to 20, max 100)</param>
        /// <response code="200">Email replies synced from all accounts successfully</response>
        /// <response code="400">Invalid request or all accounts failed to sync</response>
        /// <response code="401">User not authenticated</response>
        [HttpPost("sync-all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SyncAllEmailReplies(
            int maxResults = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (maxResults <= 0 || maxResults > 100)
                    return BadRequest("maxResults must be between 1 and 100");

                await _relayInboxService.SyncAllEmailRepliesAsync(maxResults, cancellationToken);
                return Ok("Email replies synced from all relay accounts successfully");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing email replies from all relay accounts");
                return BadRequest($"Error syncing replies from all accounts: {ex.Message}");
            }
        }

        /// <summary>
        /// Searches for email replies by subject, body, or sender email.
        /// 
        /// Database Operation:
        ///   - SELECT * FROM RelayEmailReplies 
        ///   - WHERE UserId = currentUserId 
        ///   - AND (Subject.Contains(searchTerm) OR Body.Contains(searchTerm) OR FromEmail.Contains(searchTerm))
        ///   - ORDER BY ReceivedDate DESC
        /// 
        /// Search is case-insensitive using ToLower() conversion
        /// 
        /// Uses RelayEmailReply Entity:
        ///   - Filters by: UserId, Subject, Body, FromEmail (columns searched)
        ///   - Returns: Id, MessageId, SenderEmail, Subject, Body, ReceivedAt, IsRead, Tags, Lead (navigation)
        /// </summary>
        /// <param name="searchTerm">The term to search for in Subject, Body, or FromEmail</param>
        /// <response code="200">List of matching email replies</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authenticated</response>
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SearchReplies(
            [FromQuery] string searchTerm,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return BadRequest("Search term cannot be empty");

                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in claims");

                var replies = await _relayInboxService.SearchRepliesAsync(userId, searchTerm, cancellationToken);
                return Ok(replies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching email replies");
                return BadRequest($"Error searching replies: {ex.Message}");
            }
        }

        /// <summary>
        /// Admin feature: Adds or updates an email recipient as a contact in a sales rep's contact table.
        /// If the contact already exists (by email), it updates the status. Otherwise, creates a new contact.
        /// The sales rep is determined from the RelayEmailReply.UserId field.
        /// 
        /// Database Operations:
        ///   1. SELECT RelayEmailReply by Id (includes LeadLake navigation)
        ///   2. SELECT Contact WHERE Email = relayEmailReply.FromEmail AND UserId = relayEmailReply.UserId
        ///   3. If Contact exists:
        ///      - UPDATE Contact SET Status = contactStatus, UpdatedAt = NOW()
        ///   4. If Contact does not exist:
        ///      - INSERT INTO Contact (UserId, FirstName, LastName, Email, Phone, Company, Job, Location, Status, ClosedAmount, CreatedAt, UpdatedAt)
        ///      - Values populated from RelayEmailReply and LeadLake
        /// 
        /// Entities Involved:
        ///   - RelayEmailReply: Source data for contact creation
        ///     * Uses: Id, UserId, FromEmail, Lead (for FirstName, LastName, Phone, Company, Job, Location)
        ///   - LeadLake: Referenced through RelayEmailReply navigation
        ///     * Uses: FirstName, LastName, Phone, Company, Job, Location (defaults to "Unknown" if null)
        ///   - Contact: Target entity for upsert (create or update)
        ///     * Sets: Id (auto), UserId, FirstName, LastName, Email, Phone, Company, Job, Location, Status, ClosedAmount (0), CreatedAt, UpdatedAt
        ///   - ContactStatus: Enum used for Status column
        /// 
        /// Duplicate Detection: Contact identified by combination of Email + UserId
        /// </summary>
        /// <param name="emailReplyId">The ID of the email reply to convert (RelayEmailReply.Id)</param>
        /// <param name="contactStatus">Initial contact status (optional, defaults to Active) - ContactStatus enum value</param>
        /// <response code="200">Contact created or updated successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authenticated</response>
        [HttpPost("admin/upsert-contact")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpsertEmailRecipientContact(
            [FromQuery] int emailReplyId,
            [FromQuery] string contactStatus = "Active",
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (emailReplyId <= 0)
                    return BadRequest("Invalid email reply ID");

                if (!Enum.TryParse<ContactStatus>(contactStatus, true, out var status))
                    return BadRequest($"Invalid contact status. Must be one of: {string.Join(", ", Enum.GetNames(typeof(ContactStatus)))}");

                // Fetch the relay email reply
                var relayEmailReply = await _relayInboxService.GetEmailReplyByIdAsync(emailReplyId, cancellationToken);

                if (relayEmailReply == null)
                    return BadRequest($"Email reply with ID {emailReplyId} not found");

                var contact = await _relayInboxService.UpsertEmailRecipientContactAsync(
                    relayEmailReply,
                    status,
                    cancellationToken);

                if (contact == null)
                    return BadRequest("Failed to upsert contact from email reply");

                return Ok(new
                {
                    message = "Contact upserted successfully",
                    contact = new
                    {
                        id = contact.Id,
                        firstName = contact.FirstName,
                        lastName = contact.LastName,
                        email = contact.Email,
                        company = contact.Company,
                        status = contact.Status.ToString()
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting email recipient as contact");
                return BadRequest($"Error upserting contact: {ex.Message}");
            }
        }
    }
}

