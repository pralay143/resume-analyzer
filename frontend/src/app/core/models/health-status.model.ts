export interface HealthStatus {
  status: string;
  timestamp: string;
  database: {
    canConnect: boolean;
  };
}
