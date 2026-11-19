'use client';

import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { apiService } from '@/services/api';
import { signalRService } from '@/services/signalr';
import { LocationUpdate, NotificationMessage } from '@/types';
import { MapIcon, BellIcon } from '@heroicons/react/24/outline';

const LIVE_ITEM_LIMIT = 10;

export function TrackingDashboard() {
  const [locationUpdates, setLocationUpdates] = useState<LocationUpdate[]>([]);
  const [notifications, setNotifications] = useState<NotificationMessage[]>([]);
  const [liveUpdatesFailed, setLiveUpdatesFailed] = useState(false);

  const { data: jobsResult, isPending: jobsPending, isError: jobsError } = useQuery({
    queryKey: ['jobs'],
    queryFn: () => apiService.getJobs(),
    retry: false,
  });

  useEffect(() => {
    // StrictMode runs this effect, its cleanup and this effect again: a connection attempt that
    // the cleanup has already cancelled must not register handlers or report a failure
    let cancelled = false;

    const connectSignalR = async () => {
      try {
        await signalRService.connect();
        if (cancelled) return;

        signalRService.onLocationUpdate((update) => {
          setLocationUpdates(prev => [update, ...prev.slice(0, LIVE_ITEM_LIMIT - 1)]);
        });

        signalRService.onNotification((notification) => {
          setNotifications(prev => [notification, ...prev.slice(0, LIVE_ITEM_LIMIT - 1)]);
        });
      } catch (error) {
        if (cancelled) return;
        console.error('Failed to connect to SignalR:', error);
        setLiveUpdatesFailed(true);
      }
    };

    connectSignalR();

    return () => {
      cancelled = true;
      signalRService.disconnect();
    };
  }, []);

  const activeJobs = jobsResult?.items.filter(job =>
    job.status === 'InProgress' || job.status === 'Assigned'
  ) || [];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Real-Time Tracking</h1>
        <p className="text-gray-600">Live maps, job monitoring, and notifications</p>
        {liveUpdatesFailed && (
          <p role="alert" className="mt-2 text-sm text-red-600">
            Live updates are unavailable: the connection to the server failed.
          </p>
        )}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Map Placeholder */}
        <div className="bg-white p-6 rounded-lg shadow">
          <div className="flex items-center mb-4">
            <MapIcon className="h-6 w-6 text-gray-400 mr-2" />
            <h3 className="text-lg font-medium text-gray-900">Live Map</h3>
          </div>
          <div className="bg-gray-100 h-64 rounded-lg flex items-center justify-center">
            <p className="text-gray-500">Map integration would be implemented here</p>
          </div>
        </div>

        {/* Active Jobs */}
        <div className="bg-white p-6 rounded-lg shadow">
          <h3 className="text-lg font-medium text-gray-900 mb-4">Active Jobs</h3>
          <div className="space-y-3">
            {jobsPending ? (
              <p className="text-gray-500">Loading active jobs...</p>
            ) : jobsError ? (
              <p role="alert" className="text-red-600">Error loading active jobs</p>
            ) : activeJobs.length === 0 ? (
              <p className="text-gray-500">No active jobs</p>
            ) : (
              activeJobs.map((job) => (
                <div key={job.id} className="border border-gray-200 rounded-lg p-3">
                  <div className="flex justify-between items-start">
                    <div>
                      <p className="font-medium text-gray-900">{job.title}</p>
                      <p className="text-sm text-gray-600">
                        {job.pickupAddress} → {job.dropoffAddress}
                      </p>
                      <p className="text-xs text-gray-500">
                        Driver: {job.assignedDriverId ? `Driver #${job.assignedDriverId}` : 'Unassigned'}
                      </p>
                    </div>
                    <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                      job.status === 'InProgress'
                        ? 'bg-green-100 text-green-800'
                        : 'bg-blue-100 text-blue-800'
                    }`}>
                      {job.status}
                    </span>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Location Updates */}
        <div className="bg-white p-6 rounded-lg shadow">
          <h3 className="text-lg font-medium text-gray-900 mb-4">Recent Location Updates</h3>
          <div className="space-y-3 max-h-64 overflow-y-auto">
            {locationUpdates.length === 0 ? (
              <p className="text-gray-500">No recent location updates</p>
            ) : (
              locationUpdates.map((update) => (
                <div key={`${update.jobId}-${update.driverId}-${update.timestamp}`} className="border border-gray-200 rounded-lg p-3" data-testid="location-update">
                  <p className="text-sm font-medium text-gray-900">
                    Job #{update.jobId} - Driver #{update.driverId}
                  </p>
                  <p className="text-xs text-gray-600">
                    {update.location.latitude.toFixed(4)}, {update.location.longitude.toFixed(4)}
                  </p>
                  <p className="text-xs text-gray-500">
                    {new Date(update.timestamp).toLocaleTimeString()}
                  </p>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Notifications */}
        <div className="bg-white p-6 rounded-lg shadow">
          <div className="flex items-center mb-4">
            <BellIcon className="h-6 w-6 text-gray-400 mr-2" />
            <h3 className="text-lg font-medium text-gray-900">Notifications</h3>
          </div>
          <div className="space-y-3 max-h-64 overflow-y-auto">
            {notifications.length === 0 ? (
              <p className="text-gray-500">No recent notifications</p>
            ) : (
              notifications.map((notification) => (
                <div key={notification.id} className="border border-gray-200 rounded-lg p-3" data-testid="notification">
                  <div className="flex justify-between items-start">
                    <div>
                      <p className="text-sm font-medium text-gray-900">{notification.title}</p>
                      <p className="text-sm text-gray-600">{notification.message}</p>
                    </div>
                    <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                      notification.read
                        ? 'bg-gray-100 text-gray-800'
                        : 'bg-blue-100 text-blue-800'
                    }`}>
                      {notification.read ? 'Read' : 'New'}
                    </span>
                  </div>
                  <p className="text-xs text-gray-500 mt-1">
                    {new Date(notification.timestamp).toLocaleTimeString()}
                  </p>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
}