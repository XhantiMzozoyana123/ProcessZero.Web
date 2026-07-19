# 🎯 Quick Start Guide - Extract Service

## ✅ Status: Ready to Use

Everything is installed and configured. Your Extract Service is operational!

---

## 🚀 Start Using Right Now

### Step 1: Run Your Application
```powershell
dotnet run --project ProcessZero.Web/ProcessZero.Web.csproj
```

### Step 2: Get Admin Token
- Navigate to your auth endpoint and login as admin
- Copy the JWT bearer token

### Step 3: Test Health Check
```bash
curl -X GET "http://localhost:5000/api/extract/health"

# Expected response:
{
  "status": "healthy",
  "service": "ExtractService"
}
```

### Step 4: Start Scraping
```bash
# Replace YOUR_TOKEN with your JWT admin token
curl -X POST "http://localhost:5000/api/extract/scrape?keyword=software%20developer&location=New%20York&pages=1" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json"
```

---

## 📋 API Reference

### POST /api/extract/scrape
Scrapes Yellow Pages and saves leads to database.

**Query Parameters:**
- `keyword` (required): Search term (e.g., "software developer")
- `location` (required): Geographic location (e.g., "New York")
- `pages` (optional): Number of pages to scrape (1-5, default: 1)

**Authorization:** Admin role required

**Success Response (200 OK):**
```json
{
  "message": "Successfully scraped 20 leads",
  "leads": [
    {
      "id": 1,
      "firstName": "John",
      "lastName": "Smith",
      "email": "john@company.com",
      "phone": "(555) 123-4567",
      "company": "Tech Corp",
      "job": "Manager",
      "location": "New York",
      "industry": "Technology",
      "intent": "Medium",
      "createdAt": "2025-05-11T02:30:00Z",
      "updatedAt": "2025-05-11T02:30:00Z"
    }
  ]
}
```

**Error Responses:**
- `400 Bad Request` - Missing keyword or location
- `401 Unauthorized` - Not authenticated or not admin
- `500 Internal Server Error` - Scraping failed

---

### GET /api/extract/health
Health check endpoint (public).

**Success Response (200 OK):**
```json
{
  "status": "healthy",
  "service": "ExtractService"
}
```

---

## 🎓 Example Searches

### Example 1: Software Developers in New York
```bash
curl -X POST "http://localhost:5000/api/extract/scrape?keyword=software+developer&location=New+York&pages=1" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Example 2: Accountants in Los Angeles (2 Pages)
```bash
curl -X POST "http://localhost:5000/api/extract/scrape?keyword=accountant&location=Los+Angeles&pages=2" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Example 3: Consultants in Chicago (Max 5 Pages)
```bash
curl -X POST "http://localhost:5000/api/extract/scrape?keyword=consultant&location=Chicago&pages=5" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## 📊 What You Get

Each lead in the response includes:

```json
{
  "id": 123,                          // Database ID
  "firstName": "John",                // Extracted name
  "lastName": "Smith",                // Extracted name
  "email": "john@company.com",        // Business email
  "phone": "(555) 123-4567",          // Business phone
  "company": "Tech Solutions Inc",    // Business name
  "job": "Senior Developer",          // Inferred job title
  "location": "New York",             // Geographic location
  "industry": "Technology",           // Auto-classified industry
  "intent": "Medium",                 // Lead intent (High/Medium/Low)
  "createdAt": "2025-05-11T...",     // When added to database
  "updatedAt": "2025-05-11T..."      // Last updated
}
```

---

## ✨ Key Features

✓ **Automatic scraping** - Yellow Pages search results
✓ **Data extraction** - Email, phone, name, location, job, industry
✓ **Industry classification** - 11 industry categories
✓ **Database saving** - Automatic persistence to LeadLakes
✓ **Duplicate prevention** - Skips existing emails
✓ **Multi-page support** - Scrape up to 5 pages
✓ **Error handling** - Graceful recovery from failures
✓ **Security** - Admin-only access with JWT tokens
✓ **Logging** - Complete audit trail

---

## 🐛 Troubleshooting

### Issue: "401 Unauthorized"
**Solution:** Ensure your JWT token is:
- Valid and not expired
- For an admin user
- Passed in the Authorization header as Bearer token

### Issue: "400 Bad Request"
**Solution:** Verify:
- `keyword` parameter is provided
- `location` parameter is provided
- Both are URL-encoded if they contain spaces

### Issue: "No leads found"
**Solution:**
- Verify keyword and location are valid on yellowpages.com
- Try more generic search terms
- Try different locations
- Start with `pages=1` to debug

### Issue: Timeout or slow response
**Solution:**
- Browser startup takes ~30 seconds first time
- Each page takes 15-20 seconds
- Be patient on first run
- Try reducing pages parameter

---

## 📈 Performance Tips

1. **Start small** - Use `pages=1` first
2. **Monitor performance** - Watch the application logs
3. **Adjust delays** - Consider reducing pages if too slow
4. **Check database** - Verify leads are being saved
5. **Use generic terms** - More results per search

---

## 🎯 Common Searches to Try

```
Software Developer, New York, pages=1
Accountant, Los Angeles, pages=1
Consultant, Chicago, pages=2
Manager, San Francisco, pages=1
Engineer, Boston, pages=1
Designer, Seattle, pages=1
Director, Miami, pages=1
President, Denver, pages=1
```

---

## 📚 Documentation

- `IMPLEMENTATION_COMPLETE.md` - Full implementation overview
- `PLAYWRIGHT_FINAL_VERIFICATION.md` - Detailed verification
- `PLAYWRIGHT_SETUP.md` - Setup troubleshooting
- `ExtractController.cs` - API controller documentation
- `ExtractService.cs` - Service implementation documentation

---

## ✅ Checklist Before Going Live

- [ ] Application runs without errors
- [ ] Admin user account created
- [ ] JWT token can be generated
- [ ] Health endpoint responds
- [ ] First scrape completes successfully
- [ ] Leads appear in database
- [ ] No errors in application logs
- [ ] Response times are acceptable

---

## 🎉 You're Ready!

Your Extract Service is fully operational. Start scraping leads now!

```bash
# Test it:
curl -X GET "http://localhost:5000/api/extract/health"

# Then start scraping:
curl -X POST "http://localhost:5000/api/extract/scrape?keyword=developer&location=NYC&pages=1" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

**Status:** ✅ Ready to Use
**Build:** ✓ Successful  
**Browser:** ✓ Installed
**Service:** ✓ Operational

🚀 Happy Scraping!
