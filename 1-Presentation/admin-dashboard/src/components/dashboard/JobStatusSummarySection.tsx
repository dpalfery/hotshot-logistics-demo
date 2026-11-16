import { useJobStatusSummary } from '@/hooks/useJobStatusSummary';
import { buildJobStatusCardItems } from './jobStatusCardItems';
import { JobStatusCards } from './JobStatusCards';

/** Fetch the status summary and render its loading, error or success state. */
export function JobStatusSummarySection() {
  const { data, isLoading, isError } = useJobStatusSummary();
  if (isLoading) {
    return (
      <div data-testid="job-status-cards-loading" aria-busy="true" className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
        {['pending', 'assigned', 'enroute', 'received'].map(key => (
          <div key={key} className="bg-white p-6 rounded-lg shadow animate-pulse">
            <div className="h-8 bg-gray-200 rounded" />
            <div className="h-8 bg-gray-200 rounded mt-4" />
          </div>
        ))}
      </div>
    );
  }
  if (isError || !data) return <p role="alert">Unable to load job status counts.</p>;
  return <JobStatusCards items={buildJobStatusCardItems(data)} />;
}
