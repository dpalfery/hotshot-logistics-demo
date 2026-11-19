'use client';

import { useEffect, useState } from 'react';
import { signalRService, SignalRService } from '@/services/signalr';

/**
 * Keeps the SignalR connection open while the calling component is mounted and registers its
 * handlers on it. `subscribe` registers the handlers and returns the function that removes them;
 * pass a stable function (`useCallback`), because a new one reconnects.
 *
 * @returns true when the connection could not be opened, so the view can say live updates are off.
 */
export function useRealtimeSubscription(subscribe: (service: SignalRService) => () => void): boolean {
  const [connectionFailed, setConnectionFailed] = useState(false);

  useEffect(() => {
    // StrictMode runs this effect, its cleanup and this effect again: a connection attempt that
    // the cleanup has already cancelled must not register handlers or report a failure
    let cancelled = false;
    let unsubscribe: (() => void) | undefined;

    const connect = async () => {
      try {
        await signalRService.connect();
        if (cancelled) return;
        unsubscribe = subscribe(signalRService);
      } catch (error) {
        if (cancelled) return;
        console.error('Failed to connect to SignalR:', error);
        setConnectionFailed(true);
      }
    };

    connect();

    return () => {
      cancelled = true;
      unsubscribe?.();
      signalRService.disconnect();
    };
  }, [subscribe]);

  return connectionFailed;
}
