# AI Resume Analyzer & Job Match System

[![CI](https://github.com/pralay143/resume-analyzer/actions/workflows/ci.yml/badge.svg)](https://github.com/pralay143/resume-analyzer/actions/workflows/ci.yml)

Upload a resume and a job description to get a match score, matched and missing skills,
and specific suggestions to improve the resume.

**Live:** _coming soon_ · API: `https://resume-analyzer-api.onrender.com`

## Features
- [x] PDF resume upload, with text extraction that handles two-column layouts
- [x] Job description input
- [x] AI skill extraction (Google Gemini by default, Anthropic Claude optional)
- [x] Match percentage (required skills count double)
- [x] Matched and missing skills
- [x] Resume improvement suggestions
- [x] Analysis history
- [x] Dashboard with score trend and most often missing skills

## Tech Stack
- **Frontend:** Angular 18, PrimeNG, SCSS, Chart.js
- **Backend:** ASP.NET Core 10 Web API, EF Core, PdfPig
- **Database:** PostgreSQL
- **AI:** Google Gemini (default) or Claude, selected by configuration
- **DevOps:** Docker, Render, Vercel, Neon, GitHub Actions

## Run locally

**Prerequisites:** .NET 10 SDK, Node.js 22, PostgreSQL running on `localhost:5432`.

### Backend
```bash
cd backend/src/ResumeAnalyzer.Api
cp appsettings.Development.example.json appsettings.Development.json   # then set your DB password
dotnet user-secrets set "Gemini:ApiKey" "<your-key>"                    # from https://aistudio.google.com/apikey
cd ../..
dotnet ef database update --project src/ResumeAnalyzer.Api
dotnet run --project src/ResumeAnalyzer.Api
```
The API runs on http://localhost:5080, with API docs at http://localhost:5080/scalar.

### Frontend
```bash
cd frontend
npm install
npx ng serve
```
Open http://localhost:4200.

### Tests
```bash
cd backend && dotnet test        # integration tests need the local PostgreSQL
cd frontend && npx ng test --watch=false --browsers=ChromeHeadless
```

## Deployment
See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md).
