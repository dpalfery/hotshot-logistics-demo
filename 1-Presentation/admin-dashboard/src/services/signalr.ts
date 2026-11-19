import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';
import { LocationUpdate, NotificationMessage } from '@/types';

const SIGNALR_URL = resolveSignalRUrl();

function resolveSignalRUrl(): string {
  const configured = (process.env.NEXT_PUBLIC_SIGNALR_URL || 'https://localhost:5001/realtime').trim();
  if (configured.endsWith('/realtime')) {
    return configured;
  }

  return `${configured.replace(/\/$/, '')}/realtime`;
}

class SignalRService {
  private connection: HubConnection | null = null;
  private isConnected = false;

  async connect(): Promise<void> {
    if (this.connection) {
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(SIGNALR_URL)
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

  // Job status updates
  onJobStatusUpdate(callback: (jobId: string, status: string) => void): void {
    if (this.connection) {
      this.connection.on('JobStatusUpdate', callback);
    }
  }

  offJobStatusUpdate(callback: (jobId: string, status: string) => void): void {
    if (this.connection) {
      this.connection.off('JobStatusUpdate', callback);
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

  // Driver availability updates
  onDriverStatusUpdate(callback: (driverId: number, isAvailable: boolean) => void): void {
    if (this.connection) {
      this.connection.on('DriverStatusUpdate', callback);
    }
  }

  offDriverStatusUpdate(callback: (driverId: number, isAvailable: boolean) => void): void {
    if (this.connection) {
      this.connection.off('DriverStatusUpdate', callback);
    }
  }

  // Send location update (for testing or manual updates)
  async sendLocationUpdate(update: LocationUpdate): Promise<void> {
    if (this.connection && this.isConnected) {
      await this.connection.invoke('SendLocationUpdate', update);
    }
  }

  // Join/Leave groups for specific job tracking
  async joinJobGroup(jobId: string): Promise<void> {
    if (this.connection && this.isConnected) {
      await this.connection.invoke('JoinJobGroup', jobId);
    }
  }

  async leaveJobGroup(jobId: string): Promise<void> {
    if (this.connection && this.isConnected) {
      await this.connection.invoke('LeaveJobGroup', jobId);
    }
  }

  get isConnectionActive(): boolean {
    return this.isConnected;
  }
}

export const signalRService = new SignalRService();