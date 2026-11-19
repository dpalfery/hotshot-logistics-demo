/**
 * Names the API's `/realtime` hub (`RealtimeHub`) uses, as declared by `IRealtimeHubClient` in
 * `HotshotLogistics.Contracts.Hubs` and by the hub's own methods. The client must use the server's
 * spelling: a handler registered under any other name never fires, and invoking an unknown method fails.
 */

/** Sent to the admin dashboard group on every job status change, as `(jobId: string, status: number)`. */
export const JOB_STATUS_UPDATED_EVENT = 'JobStatusUpdated';

/** Sent to the admin dashboard group when a driver's status changes, as `(driverId: number, status: number)`. */
export const DRIVER_STATUS_CHANGED_EVENT = 'DriverStatusChanged';

/** Hub methods a client invokes to start and stop receiving one job's events. */
export const JOIN_JOB_TRACKING_METHOD = 'JoinJobTracking';
export const LEAVE_JOB_TRACKING_METHOD = 'LeaveJobTracking';
