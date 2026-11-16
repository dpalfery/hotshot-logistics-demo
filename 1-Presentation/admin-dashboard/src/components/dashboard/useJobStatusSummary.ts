import { useQuery } from '@tanstack/react-query';
import { apiService } from '@/services/api';

export const JOB_STATUS_SUMMARY_QUERY_KEY = ['job-status-summary'] as const;

export function useJobStatusSummary() {
  return useQuery({
    queryKey: JOB_STATUS_SUMMARY_QUERY_KEY,
    queryFn: () => apiService.getJobStatusSummary(),
    retry: false,
  });
}
