import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { MessageService } from 'primeng/api';
import { of } from 'rxjs';

import { AppComponent } from './app.component';
import { HealthService } from './core/services/health.service';

describe('AppComponent', () => {
  let healthService: jasmine.SpyObj<HealthService>;

  beforeEach(async () => {
    healthService = jasmine.createSpyObj<HealthService>('HealthService', ['check']);
    healthService.check.and.returnValue(of({ status: 'ok', timestamp: '', database: { canConnect: true } }));

    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        MessageService,
        { provide: HealthService, useValue: healthService }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders the brand and the navigation links', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('.brand')?.textContent).toContain('Resume Analyzer');
    const links = Array.from(compiled.querySelectorAll('nav a')).map(a => a.getAttribute('href'));
    expect(links).toEqual(['/analyze', '/history', '/dashboard']);
  });

  it('pings the API on startup to wake a sleeping server', () => {
    TestBed.createComponent(AppComponent).detectChanges();

    expect(healthService.check).toHaveBeenCalledTimes(1);
  });
});
