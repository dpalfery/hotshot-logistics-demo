import Link from 'next/link';
import { Job, JobStatus } from '@/types';

const RECENT_JOB_COUNT = 5;

export type RecentJobsState =
  | { status: 'loading' }
  | { status: 'error' }
  | { status: 'data'; jobs: Job[] };

function statusClassName(status: string): string {
  switch (status) {
    case JobStatus.Pending:
      return 'bg-yellow-100 text-yellow-800';
    case JobStatus.Assigned:
      return 'bg-blue-100 text-blue-800';
    case JobStatus.EnRoute:
      return 'bg-green-100 text-green-800';
    default:
      return 'bg-gray-100 text-gray-800';
  }
}

/** The latest jobs the API returned, with its own loading, error and empty states. */
export function RecentJobs({ state }: { state: RecentJobsState }) {
  return (
    <div className="bg-white shadow rounded-lg" data-testid="recent-jobs">
      <div className="px-4 py-5 sm:p-6">
        <h3 className="text-lg leading-6 font-medium text-gray-900">Recent Jobs</h3>
        <div className="mt-5">
          {state.status === 'loading' && (
            <div className="space-y-3 animate-pulse" data-testid="recent-jobs-loading">
              <div className="h-12 rounded-lg bg-gray-100" />
              <div className="h-12 rounded-lg bg-gray-100" />
            </div>
          )}
          {state.status === 'error' && (
            <p role="alert" className="text-sm text-red-600">Error loading recent jobs</p>
          )}
          {state.status === 'data' && state.jobs.length === 0 && (
            <p className="text-sm text-gray-500">No recent jobs</p>
          )}
          {state.status === 'data' && state.jobs.length > 0 && (
            <ul className="space-y-3">
              {state.jobs.slice(0, RECENT_JOB_COUNT).map(job => (
                <li key={job.id} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                  <div>
                    <p className="text-sm font-medium text-gray-900">{job.title}</p>
                    <p className="text-sm text-gray-500">{job.pickupAddress} → {job.dropoffAddress}</p>
                  </div>
                  <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${statusClassName(job.status)}`}>
                    {job.status}
                  </span>
                </li>
              ))}
            </ul>
          )}
          <div className="mt-4">
            <Link href="/jobs" className="text-sm text-blue-600 hover:text-blue-500">
              View All Jobs →
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
