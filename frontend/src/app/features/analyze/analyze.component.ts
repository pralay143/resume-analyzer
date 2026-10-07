import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TextareaModule } from 'primeng/textarea';
import { finalize } from 'rxjs';

import { AnalysisService } from '../../core/services/analysis.service';

export const MAX_RESUME_BYTES = 5 * 1024 * 1024;
export const MAX_JOB_DESCRIPTION_LENGTH = 20_000;

/** Like Validators.required, but also rejects whitespace-only text. */
function notBlank(control: AbstractControl<string>): ValidationErrors | null {
  return control.value?.trim() ? null : { required: true };
}

@Component({
  selector: 'app-analyze',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    CardModule,
    InputTextModule,
    MessageModule,
    ProgressSpinnerModule,
    TextareaModule
  ],
  templateUrl: './analyze.component.html',
  styleUrl: './analyze.component.scss'
})
export class AnalyzeComponent {
  private readonly analysisService = inject(AnalysisService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly maxJobDescriptionLength = MAX_JOB_DESCRIPTION_LENGTH;

  readonly form = inject(FormBuilder).nonNullable.group({
    jobTitle: ['', [notBlank, Validators.maxLength(200)]],
    companyName: ['', [Validators.maxLength(200)]],
    jobDescription: ['', [notBlank, Validators.maxLength(MAX_JOB_DESCRIPTION_LENGTH)]]
  });

  readonly resume = signal<File | null>(null);
  readonly fileError = signal<string | null>(null);
  readonly submitting = signal(false);

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectFile(input.files?.[0] ?? null);
    // Clearing the input lets the same file be chosen again after a fix.
    input.value = '';
  }

  selectFile(file: File | null): void {
    const error = file ? validateResumeFile(file) : null;
    this.fileError.set(error);
    this.resume.set(error ? null : file);
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (!this.resume() && !this.fileError()) {
      this.fileError.set('Choose your resume as a PDF file.');
    }

    const resume = this.resume();
    if (this.form.invalid || !resume || this.submitting()) {
      return;
    }

    const { jobTitle, companyName, jobDescription } = this.form.getRawValue();
    this.submitting.set(true);
    this.form.disable();

    this.analysisService
      .create({ resume, jobTitle, companyName, jobDescription })
      .pipe(
        // Leaving the page cancels the request.
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.submitting.set(false);
          this.form.enable();
        })
      )
      .subscribe({
        next: analysis => this.router.navigate(['/analysis', analysis.id]),
        // The error interceptor already shows the API's message; the form keeps its values for a retry.
        error: () => undefined
      });
  }

  isInvalid(name: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[name];
    return control.invalid && control.touched;
  }
}

/** Mirrors the API's checks, so obvious mistakes are caught before a slow upload. */
export function validateResumeFile(file: File): string | null {
  const isPdf = file.name.toLowerCase().endsWith('.pdf') || file.type === 'application/pdf';
  if (!isPdf) {
    return 'Only PDF files are supported. Please choose a .pdf file.';
  }
  if (file.size === 0) {
    return 'The selected file is empty.';
  }
  if (file.size > MAX_RESUME_BYTES) {
    return 'The file is larger than 5 MB. Please choose a smaller PDF.';
  }
  return null;
}
