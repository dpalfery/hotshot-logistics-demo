import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';
import { LocationUpdate, NotificationMessage } from '@/types';
import { apiService } from './api';
import {
  DRIVER_STATUS_CHANGED_EVENT,
  JOB_STATUS_UPDATED_EVENT,
  JOIN_JOB_TRACKING_METHOD,
  LEAVE_JOB_TRACKING_METHOD,
} from './signalr-events';

const SIGNALR_URL = resolveSignalRUrl();

function resolveSignalRUrl(): string {
  const configured = (process.env.NEXT_PUBLIC_SIGNALR_URL || 'https://localhost:5001/realtime').trim();
  if (configured.endsWith('/realtime')) {
    return configured;
  }

  return `${configured.replace(/\/$/, '')}/realtime`;
}

export class SignalRService {
  private connection: HubConnection | null = null;
  private isConnected = false;

  async connect(): Promise<void> {
    if (this.connection) {
      return;
    }

    const connection = new HubConnectionBuilder()
      // The hub refuses anonymous connections. The client sends this token as the Authorization header on
      // negotiate and as the access_token query value on the WebSocket, which cannot set headers.
      .withUrl(SIGNALR_URL, { accessTokenFactory: async () => (await apiService.getAuthToken()) ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();
    this.connection = connection;

    try {
      await connection.start();
    } catch (error) {
      // A connection that disconnect() replaced or removed while it was starting is not a failure
      if (this.connection === connection) {
        this.connection = null;
      }
      throw error;
    }

    if (this.connection === connection) {
      this.isConnected = true;
    }
  }

  async disconnect(): Promise<void> {
    const connection = this.connection;
    if (!connection) {
      return;
    }

    // Drop the reference before awaiting, so a connect() that follows straight away (React
    // StrictMode mounts, unmounts and mounts again) builds a new connection instead of
    // returning early and then losing it when this stop completes.
    this.connection = null;
    this.isConnected = false;
    await connection.stop();
  }

  // Location tracking methods
  onLocationUpdate(callback: (update: LocationUpdate) => void): void {
    if (this.connection) {
      this.connection.on('LocationUpdate', callback);
    }
  }

  offLocationUpdate(callback: (update: LocationUpdate) => void): void {
    if (this.connection) {
      this.connection.off('LocationUpdate', callback);
    }
  }

  // Job status updates; the server sends the job id and the new status as its number
  onJobStatusUpdated(callback: (jobId: string, status: number) => void): void {
    if (this.connection) {
      this.connection.on(JOB_STATUS_UPDATED_EVENT, callback);
    }
  }

  offJobStatusUpdated(callback: (jobId: string, status: number) => void): void {
    if (this.connection) {
      this.connection.off(JOB_STATUS_UPDATED_EVENT, callback);
    }
  }

  // Notifications
  onNotification(callback: (notification: NotificationMessage) => void): void {
    if (this.connection) {
      this.connection.on('Notification', callback);
    }
  }

  offNotification(callback: (notification: NotificationMessage) => void): void {
    if (this.connection) {
      this.connection.off('Notification', callback);
    }
  }

  // Driver status changes; the server sends the driver id and the new status as its number
  onDriverStatusChanged(callback: (driverId: number, status: number) => void): void {
    if (this.connection) {
      this.connection.on(DRIVER_STATUS_CHANGED_EVENT, callback);
    }
  }

  offDriverStatusChanged(callback: (driverId: number, status: number) => void): void {
    if (this.connection) {
      this.connection.off(DRIVER_STATUS_CHANGED_EVENT, callback);
    }
  }

  // Join/Leave the group of one job, to receive that job's events
  async joinJobTracking(jobId: string): Promise<void> {
    if (this.connection && this.isConnected) {
      await this.connection.invoke(JOIN_JOB_TRACKING_METHOD, jobId);
    }
  }

  async leaveJobTracking(jobId: string): Promise<void> {
    if (this.connection && this.isConnected) {
      await this.connection.invoke(LEAVE_JOB_TRACKING_METHOD, jobId);
    }
  }

  get isConnectionActive(): boolean {
    return this.isConnected;
  }
}

export const signalRService = new SignalRService();