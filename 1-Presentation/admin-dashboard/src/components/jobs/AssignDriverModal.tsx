'use client';

import { Job, Driver } from '@/types';
import { XMarkIcon } from '@heroicons/react/24/outline';

interface AssignDriverModalProps {
  job: Job;
  drivers: Driver[];
  onAssign: (driverId: number) => void;
  onCancel: () => void;
  isLoading: boolean;
}

export function AssignDriverModal({
  job,
  drivers,
  onAssign,
  onCancel,
  isLoading
}: AssignDriverModalProps) {
  const availableDrivers = drivers.filter(driver => driver.isActive);

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex items-center justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
        <div className="fixed inset-0 transition-opacity" aria-hidden="true">
          <div className="absolute inset-0 bg-gray-500 opacity-75"></div>
        </div>

        <div className="relative inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
          <div className="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg leading-6 font-medium text-gray-900">
                Assign Driver to Job
              </h3>
              <button
                onClick={onCancel}
                className="text-gray-400 hover:text-gray-600"
              >
                <XMarkIcon className="h-6 w-6" />
              </button>
            </div>

            <div className="mb-4">
              <h4 className="text-sm font-medium text-gray-900">Job Details</h4>
              <p className="text-sm text-gray-600">{job.title}</p>
              <p className="text-xs text-gray-500">
                {job.pickupAddress} → {job.dropoffAddress}
              </p>
            </div>

            <div>
              <h4 className="text-sm font-medium text-gray-900 mb-3">Available Drivers</h4>
              <div className="space-y-2 max-h-60 overflow-y-auto">
                {availableDrivers.length === 0 ? (
                  <p className="text-sm text-gray-500">No available drivers</p>
                ) : (
                  availableDrivers.map((driver) => (
                    <div
                      key={driver.id}
                      className="flex items-center justify-between p-3 border border-gray-200 rounded-md hover:bg-gray-50"
                    >
                      <div>
                        <p className="text-sm font-medium text-gray-900">
                          {driver.firstName} {driver.lastName}
                        </p>
                        <p className="text-xs text-gray-500">
                          License: {driver.licenseNumber}
                        </p>
                      </div>
                      <button
                        onClick={() => onAssign(driver.id)}
                        disabled={isLoading}
                        className="inline-flex items-center px-3 py-1 border border-transparent text-xs font-medium rounded text-white bg-blue-600 hover:bg-blue-700 disabled:opacity-50"
                      >
                        {isLoading ? 'Assigning...' : 'Assign'}
                      </button>
                    </div>
                  ))
                )}
              </div>
            </div>
          </div>

          <div className="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
            <button
              type="button"
              onClick={onCancel}
              className="w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm"
            >
              Cancel
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}