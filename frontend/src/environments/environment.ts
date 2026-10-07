export const environment = {
  production: true,
  // The Render web service. Requests go straight to it (not through a Vercel rewrite) so the API's
  // per-IP rate limit sees each user's own IP.
  apiUrl: 'https://resume-analyzer-api-h5qd.onrender.com/api'
};
