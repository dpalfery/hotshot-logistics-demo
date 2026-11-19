'use client';

import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import { apiService } from '@/services/api';
import { JobStatus } from '@/types';
import { StatCard, StatValue } from './StatCard';
import { RecentJobs, RecentJobsState } from './RecentJobs';

export function DashboardOverview() {
  const jobsQuery = useQuery({
    queryKey: ['jobs'],
    queryFn: () => apiService.getJobs(),
    retry: false,
  });

  const driversQuery = useQuery({
    queryKey: ['drivers'],
    queryFn: () => apiService.getDrivers(),
    retry: false,
  });

  const invoicesQuery = useQuery({
    queryKey: ['invoices'],
    queryFn: () => apiService.getInvoices(),
    retry: false,
  });

  const statFor = <T,>(
    query: { isPending: boolean; isError: boolean; data: T | undefined },
    count: (data: T) => number
  ): StatValue => {
    if (query.isError) return { status: 'error' };
    if (query.isPending || query.data === undefined) return { status: 'loading' };
    return { status: 'data', value: count(query.data) };
  };

  const totalJobs = statFor(jobsQuery, result => result.totalCount);
  // Active jobs are: Pending, Assigned, InProgress - not Completed or Cancelled
  const activeJobs = statFor(jobsQuery, result =>
    result.items.filter(job =>
      job.status === JobStatus.Pending ||
      job.status === JobStatus.Assigned ||
      job.status === JobStatus.InProgress
    ).length
  );
  const activeDrivers = statFor(driversQuery, drivers => drivers.filter(driver => driver.isActive).length);
  // The API returns only overdue invoices, so the count is the number of items
  const overdueInvoices = statFor(invoicesQuery, result => result.items.length);

  const failedStatistics = [
    jobsQuery.isError ? 'jobs' : null,
    driversQuery.isError ? 'drivers' : null,
    invoicesQuery.isError ? 'invoices' : null,
  ].filter(name => name !== null);

  const recentJobs: RecentJobsState = jobsQuery.isError
    ? { status: 'error' }
    : jobsQuery.data
      ? { status: 'data', jobs: jobsQuery.data.items }
      : { status: 'loading' };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Navigation Sidebar */}
      <nav className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex">
              <div className="flex-shrink-0 flex items-center">
                <h2 className="text-xl font-semibold text-gray-900">Hotshot Logistics</h2>
              </div>
              <div className="sm:ml-6 sm:flex sm:space-x-8">
                <Link href="/" className="bg-blue-50 text-blue-700 border-r-2 border-blue-700 inline-flex items-center px-1 pt-1 text-sm font-medium">
                  Dashboard
                </Link>
                <Link href="/jobs" className="text-gray-500 hover:text-gray-700 inline-flex items-center px-1 pt-1 text-sm font-medium">
                  Jobs
                </Link>
                <Link href="/drivers" className="text-gray-500 hover:text-gray-700 inline-flex items-center px-1 pt-1 text-sm font-medium">
                  Drivers
                </Link>
                <Link href="/billing" className="text-gray-500 hover:text-gray-700 inline-flex items-center px-1 pt-1 text-sm font-medium">
                  Billing
                </Link>
                <Link href="/tracking" className="text-gray-500 hover:text-gray-700 inline-flex items-center px-1 pt-1 text-sm font-medium">
                  Tracking
                </Link>
              </div>
            </div>
            <div className="hidden sm:ml-4 sm:flex sm:items-center">
              <div className="text-sm text-gray-700">
                <span className="font-medium">Admin User</span>
                <span className="text-gray-500 ml-2">admin@hotshotlogistics.com</span>
              </div>
            </div>
          </div>
        </div>
      </nav>

      <div className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          <div className="space-y-6">
            <div>
              <h1 className="text-2xl font-bold text-gray-900">Dashboard Overview</h1>
              <p className="text-gray-600">Welcome to Hotshot Logistics Admin Dashboard</p>
            </div>

            {/* Stats Cards */}
            {failedStatistics.length > 0 && (
              <p role="alert" className="text-sm text-red-600">
                Error loading statistics ({failedStatistics.join(', ')})
              </p>
            )}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6" data-testid="stats-container">
              <StatCard label="Total Jobs" initial="J" iconClassName="bg-blue-500" testId="metric-total-jobs" stat={totalJobs} />
              <StatCard label="Active Jobs" initial="A" iconClassName="bg-green-500" testId="metric-active-jobs" stat={activeJobs} />
              <StatCard label="Active Drivers" initial="D" iconClassName="bg-yellow-500" testId="metric-active-drivers" stat={activeDrivers} />
              <StatCard label="Overdue Invoices" initial="O" iconClassName="bg-red-500" testId="metric-overdue-invoices" stat={overdueInvoices} />
            </div>

            <RecentJobs state={recentJobs} />

            {/* Quick Actions */}
            <div className="bg-white shadow rounded-lg">
              <div className="px-4 py-5 sm:p-6">
                <h3 className="text-lg leading-6 font-medium text-gray-900">Quick Actions</h3>
                <div className="mt-5 grid grid-cols-1 gap-3 sm:grid-cols-3">
                  <Link href="/jobs?action=create" className="inline-flex items-center justify-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700">
                    Create New Job
                  </Link>
                  <Link href="/drivers?action=add" className="inline-flex items-center justify-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50">
                    Add Driver
                  </Link>
                  <Link href="/billing?action=generate" className="inline-flex items-center justify-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50">
                    Generate Invoice
                  </Link>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}