# Deploying Resume Analyzer

The app runs on three free services:

| Part | Service | URL |
|---|---|---|
| Database | [Neon](https://neon.tech) (PostgreSQL) | — |
| API | [Render](https://render.com) web service (Docker) | `https://resume-analyzer-api-h5qd.onrender.com` |
| Frontend | [Vercel](https://vercel.com) | `https://<your-project>.vercel.app` |

Do the steps in this order: the API needs the database URL, and the API's CORS setting needs the frontend URL.

> **Secrets:** API keys and the database URL go only into the Render dashboard. Never commit them,
> and never paste them into chats or issues. Use fresh keys for production, not ones used during development.

---

## 1. Database (Neon)

1. Sign up at https://neon.tech and create a project, e.g. `resume-analyzer`.
   Pick the region closest to where you'll run the API (e.g. AWS US West / Oregon, or AWS Europe / Frankfurt).
2. On the project dashboard, click **Connect**.
3. **Turn off "Connection pooling"** so you get the *direct* connection string (the host has no `-pooler` in it).
   EF Core migrations need a direct connection.
4. Copy the connection string. It looks like:
   `postgresql://neondb_owner:...@ep-xxxx.eu-central-1.aws.neon.tech/neondb?sslmode=require&channel_binding=require`

You don't need to create tables: the API applies its migrations automatically on startup.

## 2. API (Render)

1. Sign up at https://render.com and connect your GitHub account.
2. **New → Web Service**, then pick the `resume-analyzer` repository.
3. Settings:

   | Setting | Value |
   |---|---|
   | Name | `resume-analyzer-api` (Render adds a suffix if the name is taken; see step 6) |
   | Region | Same region as the Neon database |
   | Branch | `main` |
   | Language | **Docker** |
   | Root Directory | `backend` |
   | Dockerfile Path | `./Dockerfile` |
   | Docker Build Context Directory | `.` |
   | Instance Type | **Free** |
   | Health Check Path (under Advanced) | `/api/health` |

   With Root Directory set to `backend`, Render only redeploys when something under `backend/` changes.

4. **Environment variables** (under Advanced, or the Environment tab later):

   | Key | Value |
   |---|---|
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `DATABASE_URL` | The Neon connection string from step 1 |
   | `Database__MigrateOnStartup` | `true` |
   | `Ai__Provider` | `Gemini` |
   | `Gemini__ApiKey` | A **new** key from https://aistudio.google.com/apikey |
   | `Cors__AllowedOrigins__0` | Leave out for now; added in step 4 |

   To use Claude instead, set `Ai__Provider` to `Claude` and add `Anthropic__ApiKey`.
   Optional: `RateLimiting__Analyses__PermitLimit` (default `10` analyses per hour per IP).

5. Click **Deploy Web Service**. The first build takes a few minutes. In the logs, look for:
   - `Applying 1 database migration(s): ..._InitialCreate`
   - `Now listening on: http://0.0.0.0:10000`
6. Note the service URL at the top of the page. Render adds a suffix when the name is already taken
   (this project's is `https://resume-analyzer-api-h5qd.onrender.com`). `apiUrl` in
   `frontend/src/environments/environment.ts` must match it exactly, so if you recreate the service,
   update that file and merge the change before step 3.
7. Check it: open `https://<your-api>.onrender.com/api/health`. You should see
   `{"status":"ok", ..., "database":{"canConnect":true}}`.

## 3. Frontend (Vercel)

1. Sign up at https://vercel.com with GitHub.
2. **Add New → Project**, then import the `resume-analyzer` repository.
3. Set **Root Directory** to `frontend`. Vercel detects Angular; `frontend/vercel.json` supplies the
   build command, the output folder and the rewrite that makes page refreshes work.
4. Click **Deploy**.
5. Copy the production domain, e.g. `https://resume-analyzer.vercel.app`.

## 4. Connect the two (CORS)

The API only accepts browser requests from origins it knows.

1. In Render, open the service → **Environment** and add
   `Cors__AllowedOrigins__0` = your Vercel domain, e.g. `https://resume-analyzer.vercel.app`
   (with `https://`, without a trailing slash).
2. Save. Render redeploys automatically.

## 5. Check the deployment

1. Open the Vercel URL. The home page should say **API connected ✓**
   (after up to a minute if the API was asleep; a "Waking up the server" notice appears meanwhile).
2. Go to **Analyze**, upload a PDF resume with a job description, and submit. You should land on the result page.
3. Check **History** and **Dashboard** show the new analysis.
4. Refresh the browser on `/history`. It should reload the page, not show a 404.

---

## Day-to-day

- **Deploys:** merging to `main` redeploys both. Render only rebuilds when `backend/` changes.
- **Pull requests:** CI (`.github/workflows/ci.yml`) builds and tests both apps on every PR.
  Vercel also builds a preview site for each PR. Preview domains aren't in the CORS list, so their
  API calls fail unless you add the preview domain as `Cors__AllowedOrigins__1`.
- **New migrations:** add them as usual (`dotnet ef migrations add ...`). They're applied on the next
  deploy because `Database__MigrateOnStartup` is `true`.
- **Logs:** Render dashboard → service → **Logs**. Resume and job description text are never logged.

## Free-tier limits

- **Render:** the API sleeps after 15 minutes without traffic and takes about a minute to wake.
  The frontend pings the API when the site opens, so it's usually awake by the time someone submits.
  750 free instance hours per month is enough for one always-on service.
- **Neon:** the free tier doesn't expire, but has storage and compute limits. Fine for this app's size.
- **Gemini:** the free tier has per-minute and per-day request limits. When they're hit, users see
  "The free AI quota is used up for now".

## Troubleshooting

| Symptom | Likely cause and fix |
|---|---|
| Render deploy fails with `Gemini:ApiKey is missing` | Add `Gemini__ApiKey` (two underscores) in Render's Environment tab. |
| Render logs show `No database connection is configured` | `DATABASE_URL` isn't set. |
| Render logs show a database authentication or SSL error | Re-copy the Neon string; make sure it's the direct (non-pooled) one. |
| Home page says **API not reachable ✗**, and the browser console shows a CORS error | `Cors__AllowedOrigins__0` doesn't exactly match the Vercel domain (check `https://`, no trailing slash). |
| Home page says **API not reachable ✗**, no CORS error | Wrong `apiUrl` in `environment.ts`, or the Render service is down. Check `/api/health` directly. |
| Refreshing `/history` shows Vercel's 404 page | Root Directory isn't `frontend`, so `vercel.json` wasn't used. |
| Analysis fails with "The AI service rejected the server's credentials" | The Gemini key in Render is wrong or revoked. |
| Analysis fails with "too many requests" after a few tries | The per-IP rate limit (10/hour by default). Raise `RateLimiting__Analyses__PermitLimit` if needed. |
