import { useQuery } from '@tanstack/react-query';
import { apiService } from '@/services/api';
import type { JobStatusCard } from './JobStatusCards';

const statusPresentation = [
  { status: 'Pending', label: 'Pending', color: '#3B82F6', icon: 'Hourglass' },
  { status: 'Assigned', label: 'Assigned', color: '#0EA5E9', icon: 'ClipboardList' },
  { status: 'EnRoute', label: 'En Route', color: '#0284C7', icon: 'Truck' },
  { status: 'Received', label: 'Received', color: '#0369A1', icon: 'Inbox' },
];

/** Fetches the summary and presents known statuses in dashboard order. */
export function useJobStatusSummary() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['job-status-summary'],
    queryFn: () => apiService.getJobStatusSummary(),
  });

  const statuses: JobStatusCard[] = statusPresentation.flatMap(({ status, ...presentation }) => {
    const match = data?.find(item => item.status === status);
    return match ? [{ ...presentation, count: match.count }] : [];
  });

  return { statuses, isLoading, isError };
}
