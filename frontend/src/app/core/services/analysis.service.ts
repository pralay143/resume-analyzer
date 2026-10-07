import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  Analysis,
  AnalysisListItem,
  AnalysisStats,
  CreateAnalysisRequest,
  PagedResult
} from '../models/analysis.models';

@Injectable({ providedIn: 'root' })
export class AnalysisService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/analyses`;

  create(request: CreateAnalysisRequest): Observable<Analysis> {
    const form = new FormData();
    form.append('resume', request.resume, request.resume.name);
    form.append('jobTitle', request.jobTitle.trim());
    if (request.companyName?.trim()) {
      form.append('companyName', request.companyName.trim());
    }
    form.append('jobDescription', request.jobDescription);

    return this.http.post<Analysis>(this.baseUrl, form);
  }

  getPage(page: number, pageSize: number): Observable<PagedResult<AnalysisListItem>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<AnalysisListItem>>(this.baseUrl, { params });
  }

  getById(id: string): Observable<Analysis> {
    return this.http.get<Analysis>(`${this.baseUrl}/${encodeURIComponent(id)}`);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${encodeURIComponent(id)}`);
  }

  getStats(): Observable<AnalysisStats> {
    return this.http.get<AnalysisStats>(`${this.baseUrl}/stats`);
  }
}
