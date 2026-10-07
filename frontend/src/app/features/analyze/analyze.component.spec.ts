import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Router, provideRouter } from '@angular/router';
import { Subject } from 'rxjs';

import { Analysis } from '../../core/models/analysis.models';
import { AnalysisService } from '../../core/services/analysis.service';
import { AnalyzeComponent, MAX_RESUME_BYTES, validateResumeFile } from './analyze.component';

describe('AnalyzeComponent', () => {
  let fixture: ComponentFixture<AnalyzeComponent>;
  let component: AnalyzeComponent;
  let analysisService: jasmine.SpyObj<AnalysisService>;
  let router: Router;

  const pdf = () => new File(['%PDF-1.7 resume'], 'cv.pdf', { type: 'application/pdf' });
  const el = (testId: string) =>
    (fixture.nativeElement as HTMLElement).querySelector(`[data-testid="${testId}"]`);

  beforeEach(async () => {
    analysisService = jasmine.createSpyObj<AnalysisService>('AnalysisService', ['create']);
    await TestBed.configureTestingModule({
      imports: [AnalyzeComponent],
      providers: [provideRouter([]), provideNoopAnimations(), { provide: AnalysisService, useValue: analysisService }]
    }).compileComponents();

    fixture = TestBed.createComponent(AnalyzeComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
    fixture.detectChanges();
  });

  function fillValidForm(): void {
    component.form.setValue({
      jobTitle: 'Senior Angular Developer',
      companyName: 'Contoso',
      jobDescription: 'Required: Angular, TypeScript, RxJS.'
    });
    component.selectFile(pdf());
  }

  it('does not submit an empty form and asks for a PDF', () => {
    component.submit();
    fixture.detectChanges();

    expect(analysisService.create).not.toHaveBeenCalled();
    expect(component.form.controls.jobTitle.hasError('required')).toBeTrue();
    expect(component.form.controls.jobDescription.hasError('required')).toBeTrue();
    expect(el('file-error')?.textContent).toContain('Choose your resume');
  });

  it('treats a whitespace-only job title as missing', () => {
    component.form.controls.jobTitle.setValue('   ');

    expect(component.form.controls.jobTitle.hasError('required')).toBeTrue();
  });

  it('rejects a job description over 20,000 characters', () => {
    component.form.controls.jobDescription.setValue('a'.repeat(20_001));

    expect(component.form.controls.jobDescription.hasError('maxlength')).toBeTrue();
  });

  it('shows the chosen file name', () => {
    component.selectFile(pdf());
    fixture.detectChanges();

    expect(el('file-name')?.textContent).toContain('cv.pdf');
    expect(el('file-error')).toBeNull();
  });

  it('rejects a non-PDF file', () => {
    component.selectFile(new File(['hello'], 'cv.docx'));
    fixture.detectChanges();

    expect(component.resume()).toBeNull();
    expect(el('file-error')?.textContent).toContain('Only PDF files');
  });

  it('shows progress while analyzing, then opens the result', () => {
    const response = new Subject<Analysis>();
    analysisService.create.and.returnValue(response);
    fillValidForm();

    component.submit();
    fixture.detectChanges();

    expect(analysisService.create).toHaveBeenCalledOnceWith(jasmine.objectContaining({
      jobTitle: 'Senior Angular Developer',
      companyName: 'Contoso'
    }));
    expect(component.submitting()).toBeTrue();
    expect(component.form.disabled).toBeTrue();
    expect(el('progress')).not.toBeNull();

    response.next({ id: 'analysis-1' } as Analysis);
    response.complete();
    fixture.detectChanges();

    expect(router.navigate).toHaveBeenCalledWith(['/analysis', 'analysis-1']);
    expect(component.submitting()).toBeFalse();
  });

  it('keeps the form values when the request fails', () => {
    const response = new Subject<Analysis>();
    analysisService.create.and.returnValue(response);
    fillValidForm();

    component.submit();
    response.error(new Error('502'));
    fixture.detectChanges();

    expect(router.navigate).not.toHaveBeenCalled();
    expect(component.form.enabled).toBeTrue();
    expect(component.form.controls.jobTitle.value).toBe('Senior Angular Developer');
    expect(component.resume()?.name).toBe('cv.pdf');
  });
});

describe('validateResumeFile', () => {
  it('accepts a PDF by extension or MIME type', () => {
    expect(validateResumeFile(new File(['x'], 'CV.PDF'))).toBeNull();
    expect(validateResumeFile(new File(['x'], 'resume', { type: 'application/pdf' }))).toBeNull();
  });

  it('rejects empty files', () => {
    expect(validateResumeFile(new File([], 'cv.pdf'))).toContain('empty');
  });

  it('rejects files over 5 MB', () => {
    const big = new File([new Uint8Array(MAX_RESUME_BYTES + 1)], 'cv.pdf');

    expect(validateResumeFile(big)).toContain('5 MB');
  });
});
