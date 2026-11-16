import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { apiService } from '@/services/api';
import type { JobStatusCounts } from '@/types';

const count = z.number().int().nonnegative();
const schema = z.object({ pending: count, assigned: count, enRoute: count, received: count });

/** SignalR refresh seam: invalidate this key when job status updates arrive. */
export const jobStatusCountsQueryKey = ['job-status-counts'] as const;

/** Loads validated counts; unavailable data is never replaced with fabricated counts. */
export function useJobStatusCounts() {
  const { data, isLoading, isError } = useQuery<JobStatusCounts>({
    queryKey: jobStatusCountsQueryKey,
    queryFn: async () => schema.parse(await apiService.getJobStatusCounts()),
    retry: false,
  });
  return { counts: data, isLoading, isError };
}
