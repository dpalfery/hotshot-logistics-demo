import { useQuery } from '@tanstack/react-query';
import { apiService } from '@/services/api';

export function useJobStatusSummary() {
  return useQuery({
    queryKey: ['jobs', 'status-summary'],
    queryFn: () => apiService.getJobStatusSummary(),
  });
}
