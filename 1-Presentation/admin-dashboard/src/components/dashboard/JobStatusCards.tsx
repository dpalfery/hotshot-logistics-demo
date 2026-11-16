import { ClipboardList, Hourglass, Inbox, Truck } from 'lucide-react';
import { useJobStatusSummary } from '@/hooks/useJobStatusSummary';
import type { JobStatusSummary } from '@/types';
import { JobStatusCard } from './JobStatusCard';

const statuses = [
  { label: 'Pending', field: 'pending', colorClass: 'bg-job-status-pending', icon: Hourglass },
  { label: 'Assigned', field: 'assigned', colorClass: 'bg-job-status-assigned', icon: ClipboardList },
  { label: 'EnRoute', field: 'enRoute', colorClass: 'bg-job-status-enroute', icon: Truck },
  { label: 'Received', field: 'received', colorClass: 'bg-job-status-received', icon: Inbox },
] as const satisfies ReadonlyArray<{ label: string; field: keyof JobStatusSummary; colorClass: string; icon: typeof Hourglass }>;

/** Loads and displays the dashboard's job counts by status. */
export function JobStatusCards() {
  const { data, isLoading, isError } = useJobStatusSummary();

  return (
    <section aria-label="Job status" className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4" data-testid="job-status-cards">
      {isLoading && statuses.map(({ field }) => (
        <div key={field} className="bg-white p-6 rounded-lg shadow animate-pulse" aria-label="Loading job status">
          <div className="h-4 bg-gray-200 rounded w-3/4 mb-2" />
          <div className="h-8 bg-gray-200 rounded w-1/2" />
        </div>
      ))}
      {isError && <p role="alert">Unable to load job status counts</p>}
      {!isError && data && statuses.map(({ label, field, colorClass, icon: Icon }) => (
        <JobStatusCard key={field} label={label} count={data[field]} colorClass={colorClass} icon={<Icon aria-hidden="true" />} />
      ))}
    </section>
  );
}
