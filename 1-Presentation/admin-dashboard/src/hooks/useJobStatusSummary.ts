import { useQuery } from '@tanstack/react-query';
import { apiService } from '@/services/api';

/** Shared key for fetching and invalidating the dashboard status totals. */
export const jobStatusSummaryQueryKey = ['jobs', 'status-summary'] as const;

export function useJobStatusSummary() {
  return useQuery({
    queryKey: jobStatusSummaryQueryKey,
    queryFn: () => apiService.getJobStatusSummary(),
    retry: false,
  });
}
