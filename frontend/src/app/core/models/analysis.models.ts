// Mirrors the backend DTOs in backend/src/ResumeAnalyzer.Api/Models/Dtos/AnalysisDtos.cs.

export interface Analysis {
  id: string;
  jobTitle: string;
  companyName: string | null;
  resumeFileName: string;
  jobDescription: string;
  matchScore: number;
  matchedSkills: string[];
  missingRequiredSkills: string[];
  missingPreferredSkills: string[];
  suggestions: string[];
  summary: string;
  aiModel: string;
  inputTokens: number;
  outputTokens: number;
  createdAt: string;
}

export interface AnalysisListItem {
  id: string;
  jobTitle: string;
  companyName: string | null;
  matchScore: number;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ScoreTrendPoint {
  date: string;
  score: number;
}

export interface SkillCount {
  skill: string;
  count: number;
}

export interface AnalysisStats {
  totalAnalyses: number;
  averageScore: number;
  scoreTrend: ScoreTrendPoint[];
  topMissingSkills: SkillCount[];
}

export interface CreateAnalysisRequest {
  resume: File;
  jobTitle: string;
  companyName?: string | null;
  jobDescription: string;
}

/** RFC 9457 problem details, as returned by the API for every error. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  /** Present on validation errors (400): field name to messages. */
  errors?: Record<string, string[]>;
}
