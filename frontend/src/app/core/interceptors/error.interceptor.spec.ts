import { HttpClient, HttpErrorResponse, HttpHeaders, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MessageService } from 'primeng/api';

import { describeHttpError, errorInterceptor } from './error.interceptor';

describe('describeHttpError', () => {
  const error = (status: number, body: unknown = null, headers?: HttpHeaders) =>
    new HttpErrorResponse({ status, error: body, headers });

  it('joins validation messages for 400', () => {
    const result = describeHttpError(error(400, {
      title: 'One or more validation errors occurred.',
      errors: { jobTitle: ['The jobTitle field is required.'], resume: ['The resume field is required.'] }
    }));

    expect(result?.detail).toBe('The jobTitle field is required. The resume field is required.');
  });

  it('uses the problem detail for a resume error (400)', () => {
    const result = describeHttpError(error(400, { title: 'Invalid resume file', detail: 'The file is not a valid PDF.' }));

    expect(result).toEqual({ summary: 'Invalid resume file', detail: 'The file is not a valid PDF.' });
  });

  it('uses the API detail for 429 when present', () => {
    const result = describeHttpError(error(429, { detail: 'Please try again in 12 minutes.' }));

    expect(result?.detail).toBe('Please try again in 12 minutes.');
  });

  it('falls back to Retry-After for 429 without a detail', () => {
    const result = describeHttpError(error(429, null, new HttpHeaders({ 'Retry-After': '120' })));

    expect(result?.detail).toContain('2 minutes');
  });

  it('shows the AI failure detail for 502', () => {
    const result = describeHttpError(error(502, { detail: 'The free AI quota is used up for now.' }));

    expect(result).toEqual({ summary: 'AI analysis failed', detail: 'The free AI quota is used up for now.' });
  });

  it('reports an unreachable server for status 0', () => {
    expect(describeHttpError(error(0))?.summary).toBe('Server unreachable');
  });

  it('leaves 404 to the page', () => {
    expect(describeHttpError(error(404))).toBeNull();
  });

  it('parses a problem body that arrives as a string', () => {
    expect(describeHttpError(error(502, '{"detail":"From a string"}'))?.detail).toBe('From a string');
  });
});

describe('errorInterceptor', () => {
  let http: HttpClient;
  let testing: HttpTestingController;
  let messages: jasmine.SpyObj<MessageService>;

  beforeEach(() => {
    messages = jasmine.createSpyObj<MessageService>('MessageService', ['add']);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: MessageService, useValue: messages }
      ]
    });
    http = TestBed.inject(HttpClient);
    testing = TestBed.inject(HttpTestingController);
  });

  it('shows a toast and still passes the error on', () => {
    let received: HttpErrorResponse | undefined;
    http.get('/api/analyses').subscribe({ error: e => (received = e) });

    testing.expectOne('/api/analyses').flush(
      { title: 'AI analysis failed', detail: 'The AI service has no remaining credit.' },
      { status: 502, statusText: 'Bad Gateway' });

    expect(messages.add).toHaveBeenCalledWith(jasmine.objectContaining({
      severity: 'error',
      detail: 'The AI service has no remaining credit.'
    }));
    expect(received?.status).toBe(502);
  });

  it('does not toast a 404', () => {
    http.get('/api/analyses/x').subscribe({ error: () => undefined });

    testing.expectOne('/api/analyses/x').flush(null, { status: 404, statusText: 'Not Found' });

    expect(messages.add).not.toHaveBeenCalled();
  });
});
