'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiService } from '@/services/api';
import { Job, JobStatus } from '@/types';
import { PlusIcon, PencilIcon, TruckIcon } from '@heroicons/react/24/outline';
import { JobForm } from './JobForm';
import { AssignDriverModal } from './AssignDriverModal';

export function JobsManagement() {
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [editingJob, setEditingJob] = useState<Job | null>(null);
  const [assigningJob, setAssigningJob] = useState<Job | null>(null);
  const queryClient = useQueryClient();

  const { data: jobsResult, isLoading } = useQuery({
    queryKey: ['jobs'],
    queryFn: () => apiService.getJobs(),
  });

  const { data: drivers } = useQuery({
    queryKey: ['drivers'],
    queryFn: () => apiService.getDrivers(),
  });

  const createJobMutation = useMutation({
    mutationFn: (job: Partial<Job>) => apiService.createJob(job),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['jobs'] });
      setShowCreateForm(false);
    },
  });

  const updateJobMutation = useMutation({
    mutationFn: ({ id, job }: { id: string; job: Partial<Job> }) =>
      apiService.updateJob(id, job),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['jobs'] });
      setEditingJob(null);
    },
  });

  const assignDriverMutation = useMutation({
    mutationFn: ({ jobId, driverId }: { jobId: string; driverId: number }) =>
      apiService.assignDriver(jobId, driverId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['jobs'] });
      setAssigningJob(null);
    },
  });

  const handleCreateJob = (jobData: Partial<Job>) => {
    createJobMutation.mutate(jobData);
  };

  const handleUpdateJob = (jobData: Partial<Job>) => {
    if (editingJob) {
      updateJobMutation.mutate({ id: editingJob.id, job: jobData });
    }
  };

  const handleAssignDriver = (driverId: number) => {
    if (assigningJob) {
      assignDriverMutation.mutate({ jobId: assigningJob.id, driverId });
    }
  };

  const getStatusColor = (status: JobStatus) => {
    switch (status) {
      case JobStatus.Pending:
        return 'bg-yellow-100 text-yellow-800';
      case JobStatus.Assigned:
        return 'bg-blue-100 text-blue-800';
      case JobStatus.InProgress:
        return 'bg-green-100 text-green-800';
      case JobStatus.Completed:
        return 'bg-gray-100 text-gray-800';
      case JobStatus.Cancelled:
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  if (isLoading) {
    return <div className="text-center py-8">Loading jobs...</div>;
  }

  const jobs = jobsResult?.items || [];

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Job Management</h1>
          <p className="text-gray-600">Create, assign, and track delivery jobs</p>
        </div>
        <button
          onClick={() => setShowCreateForm(true)}
          className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700"
        >
          <PlusIcon className="h-5 w-5 mr-2" />
          Create Job
        </button>
      </div>

      {/* Jobs Table */}
      <div className="bg-white shadow overflow-hidden sm:rounded-md">
        <div className="px-4 py-5 sm:p-6">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Job Details
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Status
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Driver
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Amount
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {jobs.map((job) => (
                  <tr key={job.id}>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div>
                        <div className="text-sm font-medium text-gray-900">{job.title}</div>
                        <div className="text-sm text-gray-500">
                          {job.pickupAddress} → {job.dropoffAddress}
                        </div>
                        <div className="text-xs text-gray-400">
                          {new Date(job.scheduledPickupTime).toLocaleDateString()}
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getStatusColor(job.status)}`}>
                        {job.status}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {job.assignedDriverId ? `Driver #${job.assignedDriverId}` : 'Unassigned'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      ${job.amount.toFixed(2)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                      <button
                        onClick={() => setEditingJob(job)}
                        aria-label="Edit job"
                        data-testid="edit-job-button"
                        className="text-blue-600 hover:text-blue-900"
                      >
                        <PencilIcon className="h-5 w-5" />
                      </button>
                      {job.status === JobStatus.Pending && (
                        <button
                          onClick={() => setAssigningJob(job)}
                          aria-label="Assign driver"
                          data-testid="assign-driver-button"
                          className="text-green-600 hover:text-green-900"
                        >
                          <TruckIcon className="h-5 w-5" />
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* Modals */}
      {showCreateForm && (
        <JobForm
          onSubmit={handleCreateJob}
          onCancel={() => setShowCreateForm(false)}
          isLoading={createJobMutation.isPending}
        />
      )}

      {editingJob && (
        <JobForm
          job={editingJob}
          onSubmit={handleUpdateJob}
          onCancel={() => setEditingJob(null)}
          isLoading={updateJobMutation.isPending}
        />
      )}

      {assigningJob && drivers && (
        <AssignDriverModal
          job={assigningJob}
          drivers={drivers}
          onAssign={handleAssignDriver}
          onCancel={() => setAssigningJob(null)}
          isLoading={assignDriverMutation.isPending}
        />
      )}
    </div>
  );
}