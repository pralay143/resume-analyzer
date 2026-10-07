import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { Analysis } from '../../core/models/analysis.models';
import { AnalysisService } from '../../core/services/analysis.service';
import { AnalysisDetailComponent } from './analysis-detail.component';

describe('AnalysisDetailComponent', () => {
  let fixture: ComponentFixture<AnalysisDetailComponent>;
  let analysisService: jasmine.SpyObj<AnalysisService>;

  const analysis: Analysis = {
    id: 'a1',
    jobTitle: 'Senior Angular Developer',
    companyName: 'Contoso',
    resumeFileName: 'cv.pdf',
    jobDescription: 'Required: Angular, TypeScript. Nice to have: AWS.',
    matchScore: 62,
    matchedSkills: ['Angular', 'RxJS'],
    missingRequiredSkills: ['TypeScript'],
    missingPreferredSkills: [],
    suggestions: ['If you have TypeScript experience, add it.', 'Quantify your results.'],
    summary: 'Good frontend match.',
    aiModel: 'gemini-3.8-flash',
    inputTokens: 600,
    outputTokens: 500,
    createdAt: '2026-10-07T10:00:00Z'
  };

  const el = (testId: string) =>
    (fixture.nativeElement as HTMLElement).querySelector(`[data-testid="${testId}"]`);

  async function render(id: string): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [AnalysisDetailComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        ConfirmationService,
        { provide: AnalysisService, useValue: analysisService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AnalysisDetailComponent);
    fixture.componentRef.setInput('id', id);
    fixture.detectChanges();
  }

  beforeEach(() => {
    analysisService = jasmine.createSpyObj<AnalysisService>('AnalysisService', ['getById', 'delete']);
  });

  it('renders the score, skills, suggestions and summary', async () => {
    analysisService.getById.and.returnValue(of(analysis));

    await render('a1');

    expect(analysisService.getById).toHaveBeenCalledWith('a1');
    expect(el('job-title')?.textContent).toContain('Senior Angular Developer');
    expect(el('score')?.textContent).toContain('62% match');
    expect(el('matched')?.textContent).toContain('Angular');
    expect(el('matched')?.textContent).toContain('RxJS');
    expect(el('missing-required')?.textContent).toContain('TypeScript');
    expect(el('missing-preferred')?.textContent).toContain('None');
    expect(el('suggestions')?.querySelectorAll('li').length).toBe(2);
    expect(el('summary')?.textContent).toContain('Good frontend match.');
  });

  it('shows "not found" for a 404', async () => {
    analysisService.getById.and.returnValue(throwError(() => new HttpErrorResponse({ status: 404 })));

    await render('missing');

    expect(el('not-found')).not.toBeNull();
    expect(el('job-title')).toBeNull();
  });

  it('shows a load error for other failures', async () => {
    analysisService.getById.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500 })));

    await render('a1');

    expect(el('load-error')).not.toBeNull();
  });
});
