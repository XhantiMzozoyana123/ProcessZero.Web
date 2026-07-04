# Survey API - Complete Implementation Guide

## Overview
This is a **Market Research Survey System** with **AI-gated lead qualification**. The frontend (React, Vue, Angular, mobile app, etc.) submits survey data to these API endpoints, and only qualified respondents are added to the LeadLake for sales outreach.

---

## 🔵 ENDPOINT 1: Get Survey Definition

### Request
```
GET /api/survey
```

### Response (200 OK)
```json
{
  "title": "Market Research: B2B SaaS Pain Points Q4 2026",
  "description": "Help us understand your business challenges to create better solutions",
  "questions": [
	{
	  "id": 1,
	  "text": "What is your biggest operational challenge today?",
	  "isRequired": true
	},
	{
	  "id": 2,
	  "text": "What tools/systems are you currently using to solve this?",
	  "isRequired": true
	},
	{
	  "id": 3,
	  "text": "How much time/money do you spend on this monthly?",
	  "isRequired": true
	},
	{
	  "id": 4,
	  "text": "What would an ideal solution look like?",
	  "isRequired": true
	},
	{
	  "id": 5,
	  "text": "How soon do you need a solution?",
	  "isRequired": false
	}
  ]
}
```

### Frontend Usage (JavaScript)
```javascript
async function loadSurvey() {
  const response = await fetch('/api/survey');
  const survey = await response.json();

  // Display survey.title
  // Display survey.description
  // Create form fields for each question in survey.questions
  // Collect contact info (email, firstName, lastName, phone, etc.)
}
```

---

## 🟢 ENDPOINT 2: Submit Survey Response (THE MAIN ONE)

### Request
```
POST /api/survey/submit
Content-Type: application/json
```

### Request Body (SurveyResponseSubmissionDto)
```json
{
  "respondent": {
	"email": "jane@company.com",
	"firstName": "Jane",
	"lastName": "Doe",
	"phone": "+1-555-0123",
	"company": "Acme Manufacturing Corp",
	"job": "Operations Manager",
	"industry": "Manufacturing"
  },
  "answers": [
	"Our scheduling process is completely manual - we use spreadsheets and manual coordination",
	"Currently using Excel, email, and some legacy scheduling software from 2015",
	"We spend approximately $15,000/month in labor costs just on scheduling coordination",
	"An ideal solution would automate our scheduling, integrate with our accounting system, and provide real-time visibility",
	"ASAP - we need this within the next quarter"
  ]
}
```

### Response (200 OK)
```json
{
  "id": 42,
  "email": "jane@company.com",
  "firstName": "Jane",
  "lastName": "Doe",
  "phone": "+1-555-0123",
  "company": "Acme Manufacturing Corp",
  "job": "Operations Manager",
  "industry": "Manufacturing",
  "answers": [
	"Our scheduling process is completely manual - we use spreadsheets and manual coordination",
	"Currently using Excel, email, and some legacy scheduling software from 2015",
	"We spend approximately $15,000/month in labor costs just on scheduling coordination",
	"An ideal solution would automate our scheduling, integrate with our accounting system, and provide real-time visibility",
	"ASAP - we need this within the next quarter"
  ],
  "submittedAt": "2026-07-04T23:45:30Z"
}
```

### What Happens Behind the Scenes

1. **Create/Get Respondent**
   - Query: `SELECT * FROM SurveyRespondents WHERE Email = 'jane@company.com'`
   - If not found: INSERT new respondent (first-time submission)
   - If found: Use existing respondent (they're submitting again)

2. **Create Response Record**
   - INSERT into SurveyResponses table:
	 - `SurveyRespondentId`: 1 (FK to respondent)
	 - `AnswersJson`: Serialized JSON array of answers
	 - `SubmittedAt`: Current UTC timestamp
	 - `UserId`: From JWT claims

3. **AI Qualification (LLM)**
   - Calls `ILLMService.GenerateTextAsync()` with prompt:
   ```
   Analyze the following survey responses to determine if this person has real, actionable pain points...

   Respondent: Jane Doe, Operations Manager at Acme Manufacturing Corp, Manufacturing industry

   Q1: "Our scheduling process is completely manual..."
   Q2: "Currently using Excel, email, and some legacy scheduling software from 2015"
   Q3: "We spend approximately $15,000/month in labor costs just on scheduling coordination"
   Q4: "An ideal solution would automate our scheduling..."
   Q5: "ASAP - we need this within the next quarter"

   Respond with ONLY: 'QUALIFY' or 'REJECT'
   ```

4. **Conditional LeadLake Insertion**
   - If LLM returns "QUALIFY":
	 - INSERT into LeadLakes:
	   - `Email`: jane@company.com
	   - `FirstName`: Jane
	   - `LastName`: Doe
	   - `Phone`: +1-555-0123
	   - `Company`: Acme Manufacturing Corp
	   - `Job`: Operations Manager
	   - `Industry`: Manufacturing (mapped to enum)
	   - `Intent`: High
   - If LLM returns "REJECT":
	 - No LeadLake entry created
	 - But SurveyResponse is still saved (useful for admin analysis)

### Frontend Implementation (Complete Example)

```javascript
// 1. Load survey
async function initializeSurvey() {
  const response = await fetch('/api/survey');
  const survey = await response.json();
  renderSurveyForm(survey);
}

// 2. Render form
function renderSurveyForm(survey) {
  const form = document.getElementById('surveyForm');

  // Add contact information section
  form.innerHTML = `
	<h2>${survey.title}</h2>
	<p>${survey.description}</p>

	<h3>Your Information</h3>
	<div class="form-group">
	  <label for="email">Email Address *</label>
	  <input type="email" id="email" required />
	</div>
	<div class="form-group">
	  <label for="firstName">First Name *</label>
	  <input type="text" id="firstName" required />
	</div>
	<div class="form-group">
	  <label for="lastName">Last Name *</label>
	  <input type="text" id="lastName" required />
	</div>
	<div class="form-group">
	  <label for="phone">Phone *</label>
	  <input type="tel" id="phone" required />
	</div>
	<div class="form-group">
	  <label for="company">Company</label>
	  <input type="text" id="company" />
	</div>
	<div class="form-group">
	  <label for="job">Job Title</label>
	  <input type="text" id="job" />
	</div>
	<div class="form-group">
	  <label for="industry">Industry</label>
	  <select id="industry">
		<option value="">-- Select --</option>
		<option value="Technology">Technology</option>
		<option value="Finance">Finance</option>
		<option value="Healthcare">Healthcare</option>
		<option value="Manufacturing">Manufacturing</option>
		<option value="Retail">Retail</option>
		<option value="Education">Education</option>
		<option value="Other">Other</option>
	  </select>
	</div>

	<h3>Survey Questions</h3>
  `;

  // Add survey questions
  survey.questions.forEach((question, index) => {
	const required = question.isRequired ? '*' : '';
	form.innerHTML += `
	  <div class="form-group">
		<label for="answer${index}">
		  Q${index + 1}: ${question.text} ${required}
		</label>
		<textarea 
		  id="answer${index}" 
		  ${question.isRequired ? 'required' : ''}
		  placeholder="Type your answer here..."
		></textarea>
	  </div>
	`;
  });

  form.innerHTML += '<button type="submit">Submit Survey</button>';
}

// 3. Handle form submission
document.getElementById('surveyForm').addEventListener('submit', async (e) => {
  e.preventDefault();

  // Get survey
  const surveyResponse = await fetch('/api/survey');
  const survey = await surveyResponse.json();

  // Collect respondent data
  const respondent = {
	email: document.getElementById('email').value,
	firstName: document.getElementById('firstName').value,
	lastName: document.getElementById('lastName').value,
	phone: document.getElementById('phone').value,
	company: document.getElementById('company').value || '',
	job: document.getElementById('job').value || '',
	industry: document.getElementById('industry').value || ''
  };

  // Collect answers (one per question)
  const answers = survey.questions.map((q, index) => {
	return document.getElementById(`answer${index}`).value;
  });

  // Build submission
  const submission = {
	respondent: respondent,
	answers: answers
  };

  try {
	// Submit to API
	const submitResponse = await fetch('/api/survey/submit', {
	  method: 'POST',
	  headers: {
		'Content-Type': 'application/json'
	  },
	  body: JSON.stringify(submission)
	});

	if (submitResponse.ok) {
	  const result = await submitResponse.json();
	  showSuccessMessage(
		`Thank you, ${result.firstName}! Your responses have been recorded.`
	  );
	  resetForm();
	} else {
	  showErrorMessage('Failed to submit survey. Please try again.');
	}
  } catch (error) {
	console.error('Error:', error);
	showErrorMessage('Network error. Please try again.');
  }
});
```

---

## 🔴 ENDPOINT 3: Get All Survey Responses (Admin Only)

### Request
```
GET /api/survey/admin/responses
Authorization: Bearer <JWT_TOKEN>
X-Authorization-Policy: Admin
```

### Response (200 OK)
```json
{
  "title": "Market Research: B2B SaaS Pain Points Q4 2026",
  "totalResponses": 42,
  "collectedFrom": "2026-06-15T10:00:00Z",
  "collectedTo": "2026-07-04T23:59:59Z",
  "responses": [
	{
	  "id": 42,
	  "email": "jane@company.com",
	  "firstName": "Jane",
	  "lastName": "Doe",
	  "phone": "+1-555-0123",
	  "company": "Acme Manufacturing Corp",
	  "job": "Operations Manager",
	  "industry": "Manufacturing",
	  "answers": [
		"Our scheduling process is completely manual...",
		"Currently using Excel, email...",
		"We spend approximately $15,000/month...",
		"An ideal solution would automate our scheduling...",
		"ASAP - we need this within the next quarter"
	  ],
	  "submittedAt": "2026-07-04T23:45:30Z"
	},
	{
	  "id": 41,
	  "email": "john@company.com",
	  "firstName": "John",
	  "lastName": "Smith",
	  "phone": "+1-555-0456",
	  "company": "TechCorp Solutions",
	  "job": "CTO",
	  "industry": "Technology",
	  "answers": [
		"Data integration across multiple systems is our biggest challenge...",
		"We have multiple APIs and custom solutions...",
		"We spend $50,000/month on infrastructure and integration teams...",
		"A unified data platform with real-time sync...",
		"Within 3 months"
	  ],
	  "submittedAt": "2026-07-04T22:15:00Z"
	}
  ]
}
```

### Use Case
- Admin dashboard displays all survey submissions
- Export to CSV for analysis
- Use aggregated data for AI insights
- Identify trends and common pain points

---

## 🟡 ENDPOINT 4: Get Single Response (Admin Only)

### Request
```
GET /api/survey/admin/responses/42
Authorization: Bearer <JWT_TOKEN>
X-Authorization-Policy: Admin
```

### Response (200 OK)
```json
{
  "id": 42,
  "email": "jane@company.com",
  "firstName": "Jane",
  "lastName": "Doe",
  "phone": "+1-555-0123",
  "company": "Acme Manufacturing Corp",
  "job": "Operations Manager",
  "industry": "Manufacturing",
  "answers": [
	"Our scheduling process is completely manual - we use spreadsheets and manual coordination",
	"Currently using Excel, email, and some legacy scheduling software from 2015",
	"We spend approximately $15,000/month in labor costs just on scheduling coordination",
	"An ideal solution would automate our scheduling, integrate with our accounting system, and provide real-time visibility",
	"ASAP - we need this within the next quarter"
  ],
  "submittedAt": "2026-07-04T23:45:30Z"
}
```

### Use Case
- Admin deep-dive into individual response
- Read full context and pain points
- Manually review before outreach

---

## 🟠 ENDPOINT 5: Upload Survey (Admin Only)

### Request
```
PUT /api/survey/admin
Authorization: Bearer <JWT_TOKEN>
X-Authorization-Policy: Admin
Content-Type: application/json
```

### Request Body (SurveyDto)
```json
{
  "title": "Market Research: B2B SaaS Pain Points Q4 2026",
  "description": "Help us understand your business challenges to create better solutions for your organization",
  "questions": [
	{
	  "id": 1,
	  "text": "What is your biggest operational challenge today?",
	  "isRequired": true
	},
	{
	  "id": 2,
	  "text": "What tools/systems are you currently using to solve this?",
	  "isRequired": true
	},
	{
	  "id": 3,
	  "text": "How much time/money do you spend on this monthly?",
	  "isRequired": true
	},
	{
	  "id": 4,
	  "text": "What would an ideal solution look like?",
	  "isRequired": true
	},
	{
	  "id": 5,
	  "text": "How soon do you need a solution?",
	  "isRequired": false
	}
  ]
}
```

### Response (204 No Content)

### What Happens
- Survey serialized to JSON
- Stored in `SurveyQuestions.QuestionsJson`
- Indexed by `UploadedAt` for fast retrieval
- Public users see this survey when they access `/api/survey`
- Previous surveys kept in database (audit trail)

### Admin Frontend Example
```javascript
async function uploadSurvey(surveyData) {
  const response = await fetch('/api/survey/admin', {
	method: 'PUT',
	headers: {
	  'Content-Type': 'application/json',
	  'Authorization': `Bearer ${getJWT()}`
	},
	body: JSON.stringify(surveyData)
  });

  if (response.ok) {
	console.log('Survey uploaded successfully');
  }
}
```

---

## 🟤 ENDPOINT 6: Get Survey (Admin Only)

### Request
```
GET /api/survey/admin/survey
Authorization: Bearer <JWT_TOKEN>
X-Authorization-Policy: Admin
```

### Response (200 OK)
```json
{
  "title": "Market Research: B2B SaaS Pain Points Q4 2026",
  "description": "Help us understand your business challenges...",
  "questions": [
	{
	  "id": 1,
	  "text": "What is your biggest operational challenge today?",
	  "isRequired": true
	},
	{
	  "id": 2,
	  "text": "What tools/systems are you currently using to solve this?",
	  "isRequired": true
	},
	{
	  "id": 3,
	  "text": "How much time/money do you spend on this monthly?",
	  "isRequired": true
	},
	{
	  "id": 4,
	  "text": "What would an ideal solution look like?",
	  "isRequired": true
	},
	{
	  "id": 5,
	  "text": "How soon do you need a solution?",
	  "isRequired": false
	}
  ]
}
```

### Use Case
- Admin dashboard shows current survey
- Admin can edit and re-upload survey
- Allows A/B testing different survey versions

---

## 📊 Database Schema Summary

### SurveyQuestions Table
```sql
CREATE TABLE SurveyQuestions (
	Id INT PRIMARY KEY AUTO_INCREMENT,
	Title VARCHAR(255),
	Description VARCHAR(1000),
	QuestionsJson LONGTEXT,  -- Serialized SurveyDto
	UploadedAt DATETIME(6),
	UserId VARCHAR(450),      -- Admin who uploaded
	CreatedAt DATETIME(6),
	UpdatedAt DATETIME(6),
	INDEX IX_SurveyQuestions_UploadedAt (UploadedAt DESC)
);
```

### SurveyRespondents Table
```sql
CREATE TABLE SurveyRespondents (
	Id INT PRIMARY KEY AUTO_INCREMENT,
	Email VARCHAR(255) UNIQUE,
	FirstName VARCHAR(100),
	LastName VARCHAR(100),
	Phone VARCHAR(20),
	Company VARCHAR(255),
	Job VARCHAR(255),
	Industry VARCHAR(100),
	UserId VARCHAR(450),
	CreatedAt DATETIME(6),
	UpdatedAt DATETIME(6),
	INDEX IX_SurveyRespondents_Email (Email),
	INDEX IX_SurveyRespondents_UserId (UserId)
);
```

### SurveyResponses Table
```sql
CREATE TABLE SurveyResponses (
	Id INT PRIMARY KEY AUTO_INCREMENT,
	SurveyRespondentId INT NOT NULL,
	AnswersJson LONGTEXT,  -- Serialized answers
	SubmittedAt DATETIME(6),
	UserId VARCHAR(450),
	CreatedAt DATETIME(6),
	UpdatedAt DATETIME(6),
	FOREIGN KEY (SurveyRespondentId) REFERENCES SurveyRespondents(Id) ON DELETE CASCADE,
	INDEX IX_SurveyResponses_RespondentId_SubmittedAt (SurveyRespondentId, SubmittedAt DESC)
);
```

### LeadLakes Table (Auto-populated)
```sql
CREATE TABLE LeadLakes (
	Id INT PRIMARY KEY AUTO_INCREMENT,
	FirstName VARCHAR(100),
	LastName VARCHAR(100),
	Email VARCHAR(255),
	Phone VARCHAR(20),
	Company VARCHAR(255),
	Job VARCHAR(255),
	Location VARCHAR(255),
	Industry ENUM('Technology','Finance','Healthcare','Manufacturing','Retail','Education','Transportation','Entertainment','Hospitality','Other'),
	Intent ENUM('High','Medium','Low'),
	UserId VARCHAR(450),
	CreatedAt DATETIME(6),
	UpdatedAt DATETIME(6),
	INDEX IX_LeadLakes_Email (Email),
	INDEX IX_LeadLakes_UserId (UserId),
	INDEX IX_LeadLakes_UserId_Email (UserId, Email)
);
```

---

## 🔑 Key Points

### Contact Information Collection
- **Email** (required): Unique identifier, duplicate detection
- **FirstName** (required): Personalization for sales
- **LastName** (required): Personalization for sales
- **Phone** (required): Sales contact method
- **Company** (optional): Context for pain points
- **Job** (optional): Role context for analysis
- **Industry** (optional): Mapped to LeadLakeIndustry enum

### AI Qualification Flow
1. User submits responses + contact info
2. SurveyService calls ILLMService
3. LLM analyzes answers + respondent profile
4. Returns "QUALIFY" or "REJECT"
5. If qualified → Added to LeadLake automatically
6. If rejected → Response saved, no lead created

### Error Handling
- LLM failure doesn't block survey submission
- Survey response always saved
- LeadLake insertion is optional/conditional
- Graceful degradation

---

## 🚀 Complete Frontend Implementation Checklist

- [ ] GET /api/survey to load survey definition
- [ ] Render contact information form fields
- [ ] Render survey questions dynamically
- [ ] Validate all required fields before submission
- [ ] POST /api/survey/submit with complete payload
- [ ] Handle success/error responses
- [ ] Show confirmation message
- [ ] Reset form after successful submission

---

## 📝 Example: Complete End-to-End Flow

```
1. User visits your app/website
   ↓
2. Frontend GETs /api/survey
   ↓
3. Display survey title, description, questions
   ↓
4. User fills in:
   - Email: jane@company.com
   - FirstName: Jane
   - LastName: Doe
   - Phone: +1-555-0123
   - Company: Acme Corp
   - Job: Operations Manager
   - Industry: Manufacturing
   - Q1 Answer: "Manual scheduling..."
   - Q2 Answer: "Excel and email..."
   - Q3 Answer: "$15,000/month..."
   - Q4 Answer: "Automated solution..."
   - Q5 Answer: "ASAP"
   ↓
5. User clicks Submit
   ↓
6. Frontend POSTs /api/survey/submit with full payload
   ↓
7. Backend:
   - Creates SurveyRespondent (Jane Doe contact)
   - Creates SurveyResponse (all answers)
   - Calls LLM to analyze pain points
   - LLM returns "QUALIFY"
   - Inserts Jane into LeadLakes table
   ↓
8. Returns 200 OK with SurveyResponseResultDto
   ↓
9. Frontend shows: "Thank you Jane! Your survey has been recorded."
   ↓
10. Sales team sees Jane in LeadLake dashboard
	- Email: jane@company.com
	- Phone: +1-555-0123
	- Company: Acme Corp
	- Industry: Manufacturing
	- Intent: High
	↓
11. Sales team reaches out to Jane with targeted solution
```

---

## ✅ Verification Checklist

- [x] All contact fields collected from client
- [x] All survey responses captured with answers
- [x] LLM validates pain points
- [x] Qualified leads auto-added to LeadLake
- [x] Database migration applied
- [x] API endpoints documented
- [x] Error handling in place
- [x] Admin controls for survey management

