'use client';

import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import { apiService } from '@/services/api';
import { JobStatus } from '@/types';
import { JobStatusSummarySection } from './JobStatusSummarySection';

export function DashboardOverview() {
  const { data: jobs, isLoading: jobsLoading } = useQuery({
    queryKey: ['jobs'],
    queryFn: () => apiService.getJobs(),
    retry: false,
  });

  const { data: drivers, isLoading: driversLoading } = useQuery({
    queryKey: ['drivers'],
    queryFn: () => apiService.getDrivers(),
    retry: false,
  });

  const { data: invoices, isLoading: invoicesLoading } = useQuery({
    queryKey: ['invoices'],
    queryFn: () => apiService.getInvoices(),
    retry: false,
  });

  const stats = {
    totalJobs: jobs?.totalCount || 0,
    // Active jobs are: Pending, Assigned, InProgress - not Completed or Cancelled
    activeJobs: jobs?.items.filter(job =>
      job.status === JobStatus.Pending ||
      job.status === JobStatus.Assigned ||
      job.status === JobStatus.InProgress
    ).length || 0,
    pendingJobs: jobs?.items.filter(job => job.status === JobStatus.Pending).length || 0,
    totalDrivers: drivers?.length || 0,
    activeDrivers: drivers?.filter(driver => driver.isActive).length || 0,
    totalInvoices: invoices?.totalCount || 0,
    // Since we're getting overdue invoices directly from the API, just count them
    overdueInvoices: invoices?.items?.length || 0,
  };

  if (jobsLoading || driversLoading || invoicesLoading) {
    return (
      <div className="space-y-6">
        <div className="animate-pulse">
          <div className="h-8 bg-gray-200 rounded w-1/4 mb-4"></div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="bg-white p-6 rounded-lg shadow">
                <div className="h-4 bg-gray-200 rounded w-3/4 mb-2"></div>
                <div className="h-8 bg-gray-200 rounded w-1/2"></div>
              </div>
            ))}
          </div>
        </div>
      </div>
    );
  }

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
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6" data-testid="stats-container">
        <div className="bg-white p-6 rounded-lg shadow" data-testid="stats-card">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <div className="w-8 h-8 bg-blue-500 rounded-md flex items-center justify-center">
                <span className="text-white text-sm font-bold">J</span>
              </div>
            </div>
            <div className="ml-4">
              <dt className="text-sm font-medium text-gray-500 truncate">Total Jobs</dt>
              <dd className="text-2xl font-semibold text-gray-900" data-testid="metric-total-jobs">{stats.totalJobs}</dd>
            </div>
          </div>
        </div>

        <div className="bg-white p-6 rounded-lg shadow" data-testid="stats-card">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <div className="w-8 h-8 bg-green-500 rounded-md flex items-center justify-center">
                <span className="text-white text-sm font-bold">A</span>
              </div>
            </div>
            <div className="ml-4">
              <dt className="text-sm font-medium text-gray-500 truncate">Active Jobs</dt>
              <dd className="text-2xl font-semibold text-gray-900" data-testid="metric-active-jobs">{stats.activeJobs}</dd>
            </div>
          </div>
        </div>

        <div className="bg-white p-6 rounded-lg shadow" data-testid="stats-card">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <div className="w-8 h-8 bg-yellow-500 rounded-md flex items-center justify-center">
                <span className="text-white text-sm font-bold">D</span>
              </div>
            </div>
            <div className="ml-4">
              <dt className="text-sm font-medium text-gray-500 truncate">Active Drivers</dt>
              <dd className="text-2xl font-semibold text-gray-900" data-testid="metric-active-drivers">{stats.activeDrivers}</dd>
            </div>
          </div>
        </div>

        <div className="bg-white p-6 rounded-lg shadow" data-testid="stats-card">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <div className="w-8 h-8 bg-red-500 rounded-md flex items-center justify-center">
                <span className="text-white text-sm font-bold">O</span>
              </div>
            </div>
            <div className="ml-4">
              <dt className="text-sm font-medium text-gray-500 truncate">Overdue Invoices</dt>
              <dd className="text-2xl font-semibold text-gray-900" data-testid="metric-overdue-invoices">{stats.overdueInvoices}</dd>
            </div>
          </div>
        </div>
      </div>

            <JobStatusSummarySection />

            {/* Recent Activity */}
            <div className="bg-white shadow rounded-lg">
              <div className="px-4 py-5 sm:p-6">
                <h3 className="text-lg leading-6 font-medium text-gray-900">Recent Jobs</h3>
                <div className="mt-5">
                  <div className="space-y-3">
                    <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                      <div>
                        <p className="text-sm font-medium text-gray-900">Urgent Delivery</p>
                        <p className="text-sm text-gray-500">123 Main St → 456 Oak Ave</p>
                      </div>
                      <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                        InProgress
                      </span>
                    </div>
                    <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                      <div>
                        <p className="text-sm font-medium text-gray-900">Standard Delivery</p>
                        <p className="text-sm text-gray-500">789 Pine St → 321 Elm Ave</p>
                      </div>
                      <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
                        Pending
                      </span>
                    </div>
                  </div>
                  <div className="mt-4">
                    <a href="/jobs" className="text-sm text-blue-600 hover:text-blue-500">
                      View All Jobs →
                    </a>
                  </div>
                </div>
              </div>
            </div>

            {/* Quick Actions */}
            <div className="bg-white shadow rounded-lg">
              <div className="px-4 py-5 sm:p-6">
                <h3 className="text-lg leading-6 font-medium text-gray-900">Quick Actions</h3>
                <div className="mt-5 grid grid-cols-1 gap-3 sm:grid-cols-3">
                  <button className="inline-flex items-center justify-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700">
                    Create New Job
                  </button>
                  <button className="inline-flex items-center justify-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50">
                    Add Driver
                  </button>
                  <button className="inline-flex items-center justify-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50">
                    Generate Invoice
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}