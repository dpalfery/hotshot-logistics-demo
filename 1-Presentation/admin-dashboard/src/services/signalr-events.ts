/**
 * Names of the events the API's `/realtime` hub sends to clients, as declared by
 * `IRealtimeHubClient` in `HotshotLogistics.Contracts.Hubs`. The client must listen for the server's
 * spelling: a handler registered under any other name never fires.
 */
export const JOB_STATUS_UPDATED_EVENT = 'JobStatusUpdated';
