import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { AnalysisService } from './analysis.service';

describe('AnalysisService', () => {
  const baseUrl = `${environment.apiUrl}/analyses`;
  let service: AnalysisService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AnalysisService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('create posts multipart form data with trimmed fields', () => {
    const resume = new File(['%PDF-1.7'], 'cv.pdf', { type: 'application/pdf' });

    service.create({ resume, jobTitle: '  Angular Dev ', companyName: ' Contoso ', jobDescription: 'Angular, RxJS' }).subscribe();

    const req = http.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    const body = req.request.body as FormData;
    expect(body instanceof FormData).toBeTrue();
    expect((body.get('resume') as File).name).toBe('cv.pdf');
    expect(body.get('jobTitle')).toBe('Angular Dev');
    expect(body.get('companyName')).toBe('Contoso');
    expect(body.get('jobDescription')).toBe('Angular, RxJS');
    req.flush({ id: 'new-id' });
  });

  it('create leaves out a blank company name', () => {
    const resume = new File(['%PDF-1.7'], 'cv.pdf');

    service.create({ resume, jobTitle: 'Dev', companyName: '   ', jobDescription: 'JD' }).subscribe();

    const req = http.expectOne(baseUrl);
    expect((req.request.body as FormData).has('companyName')).toBeFalse();
    req.flush({});
  });

  it('getPage sends page and pageSize', () => {
    service.getPage(2, 10).subscribe();

    const req = http.expectOne(r => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    req.flush({ items: [], page: 2, pageSize: 10, totalCount: 0, totalPages: 0 });
  });

  it('getById, delete and getStats call the right URLs', () => {
    service.getById('abc').subscribe();
    service.delete('abc').subscribe();
    service.getStats().subscribe();

    http.expectOne({ method: 'GET', url: `${baseUrl}/abc` }).flush({ id: 'abc' });
    http.expectOne({ method: 'DELETE', url: `${baseUrl}/abc` }).flush(null);
    http.expectOne({ method: 'GET', url: `${baseUrl}/stats` })
      .flush({ totalAnalyses: 0, averageScore: 0, scoreTrend: [], topMissingSkills: [] });
  });
});
