import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { MessageService } from 'primeng/api';

import { SLOW_REQUEST_MS, WAKE_TOAST_KEY, slowRequestInterceptor } from './slow-request.interceptor';

describe('slowRequestInterceptor', () => {
  let http: HttpClient;
  let testing: HttpTestingController;
  let messages: jasmine.SpyObj<MessageService>;

  beforeEach(() => {
    messages = jasmine.createSpyObj<MessageService>('MessageService', ['add', 'clear']);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([slowRequestInterceptor])),
        provideHttpClientTesting(),
        { provide: MessageService, useValue: messages }
      ]
    });
    http = TestBed.inject(HttpClient);
    testing = TestBed.inject(HttpTestingController);
  });

  it('shows nothing for a fast GET', fakeAsync(() => {
    http.get('/api/health').subscribe();
    tick(1000);
    testing.expectOne('/api/health').flush({});
    tick(SLOW_REQUEST_MS);

    expect(messages.add).not.toHaveBeenCalled();
  }));

  it('shows one notice for slow GETs and clears it when the last one finishes', fakeAsync(() => {
    http.get('/api/health').subscribe();
    http.get('/api/analyses/stats').subscribe();
    tick(SLOW_REQUEST_MS + 1);

    expect(messages.add).toHaveBeenCalledOnceWith(jasmine.objectContaining({ key: WAKE_TOAST_KEY, sticky: true }));

    testing.expectOne('/api/health').flush({});
    expect(messages.clear).not.toHaveBeenCalled();

    testing.expectOne('/api/analyses/stats').flush({});
    expect(messages.clear).toHaveBeenCalledOnceWith(WAKE_TOAST_KEY);
  }));

  it('ignores slow POSTs, which are expected to take a while', fakeAsync(() => {
    http.post('/api/analyses', {}).subscribe();
    tick(SLOW_REQUEST_MS * 4);
    testing.expectOne('/api/analyses').flush({});

    expect(messages.add).not.toHaveBeenCalled();
  }));

  it('clears the notice when a slow request fails', fakeAsync(() => {
    http.get('/api/health').subscribe({ error: () => undefined });
    tick(SLOW_REQUEST_MS + 1);
    testing.expectOne('/api/health').flush(null, { status: 502, statusText: 'Bad Gateway' });

    expect(messages.clear).toHaveBeenCalledOnceWith(WAKE_TOAST_KEY);
  }));
});
