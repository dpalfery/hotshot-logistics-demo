'use client';

import { useState } from 'react';
import { Job, JobStatus, JobPriority } from '@/types';
import { XMarkIcon } from '@heroicons/react/24/outline';

interface JobFormProps {
  job?: Job;
  onSubmit: (job: Partial<Job>) => void;
  onCancel: () => void;
  isLoading: boolean;
}

export function JobForm({ job, onSubmit, onCancel, isLoading }: JobFormProps) {
  // The form is mounted once per create/edit session (the parent renders it
  // conditionally), so the job prop is fixed for the component's lifetime and
  // can seed the initial state directly.
  const [formData, setFormData] = useState<Partial<Job>>(
    job
      ? {
          title: job.title,
          pickupAddress: job.pickupAddress,
          dropoffAddress: job.dropoffAddress,
          amount: job.amount,
          scheduledPickupTime: job.scheduledPickupTime,
          specialInstructions: job.specialInstructions,
          customerId: job.customerId,
          status: job.status,
          priority: job.priority,
        }
      : {
          title: '',
          pickupAddress: '',
          dropoffAddress: '',
          amount: 0,
          scheduledPickupTime: '',
          specialInstructions: '',
          customerId: '',
          status: JobStatus.Pending,
          priority: JobPriority.Normal,
        }
  );

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit(formData);
  };

  const handleChange = <K extends keyof Job>(field: K, value: Job[K]) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex items-center justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
        <div className="fixed inset-0 transition-opacity" aria-hidden="true">
          <div className="absolute inset-0 bg-gray-500 opacity-75"></div>
        </div>

        <div className="inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
          <div className="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg leading-6 font-medium text-gray-900">
                {job ? 'Edit Job' : 'Create New Job'}
              </h3>
              <button
                onClick={onCancel}
                className="text-gray-400 hover:text-gray-600"
              >
                <XMarkIcon className="h-6 w-6" />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">Title</label>
                <input
                  type="text"
                  required
                  value={formData.title}
                  onChange={(e) => handleChange('title', e.target.value)}
                  className="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700">Pickup Address</label>
                <input
                  type="text"
                  required
                  value={formData.pickupAddress}
                  onChange={(e) => handleChange('pickupAddress', e.target.value)}
                  className="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700">Dropoff Address</label>
                <input
                  type="text"
                  required
                  value={formData.dropoffAddress}
                  onChange={(e) => handleChange('dropoffAddress', e.target.value)}
                  className="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700">Amount</label>
                  <input
                    type="number"
                    step="0.01"
                    required
                    value={formData.amount}
                    onChange={(e) => handleChange('amount', parseFloat(e.target.value))}
                    className="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700">Priority</label>
                  <select
                    value={formData.priority}
                    onChange={(e) => handleChange('priority', e.target.value as JobPriority)}
                    className="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500"
                  >
                    <option value="Low">Low</option>
                    <option value="Normal">Normal</option>
                    <option value="High">High</option>
                    <option value="Urgent">Urgent</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700">Scheduled Pickup Time</label>
                <input
                  type="datetime-local"
                  required
                  value={formData.scheduledPickupTime}
                  onChange={(e) => handleChange('scheduledPickupTime', e.target.value)}
                  className="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700">Customer ID</label>
                <input
                  type="text"
                  required
                  value={formData.customerId}
                  onChange={(e) => handleChange('customerId', e.target.value)}
                  className="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700">Special Instructions</label>
                <textarea
                  rows={3}
                  value={formData.specialInstructions}
                  onChange={(e) => handleChange('specialInstructions', e.target.value)}
                  className="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
            </form>
          </div>

          <div className="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
            <button
              type="submit"
              onClick={handleSubmit}
              disabled={isLoading}
              className="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:ml-3 sm:w-auto sm:text-sm disabled:opacity-50"
            >
              {isLoading ? 'Saving...' : (job ? 'Update Job' : 'Create Job')}
            </button>
            <button
              type="button"
              onClick={onCancel}
              className="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm"
            >
              Cancel
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}