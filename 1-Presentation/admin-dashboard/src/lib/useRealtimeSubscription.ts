'use client';

import { useEffect, useState } from 'react';
import { signalRService, SignalRService } from '@/services/signalr';

// One connection serves every mounted consumer. The last one to leave closes it.
let activeSubscribers = 0;

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

    activeSubscribers += 1;

    const connect = async () => {
      try {
        await signalRService.connect();
        if (cancelled) {
          if (activeSubscribers === 0) {
            void signalRService.disconnect().catch(error => {
              console.error('Failed to disconnect from SignalR:', error);
            });
          }
          return;
        }
        setConnectionFailed(false);
        unsubscribe = subscribe(signalRService);
      } catch (error) {
        if (cancelled) return;
        console.error('Failed to connect to SignalR:', error);
        setConnectionFailed(true);
      }
    };

    void connect();

    return () => {
      cancelled = true;
      unsubscribe?.();
      activeSubscribers -= 1;
      if (activeSubscribers > 0) return;

      activeSubscribers = 0;
      void signalRService.disconnect().catch(error => {
        console.error('Failed to disconnect from SignalR:', error);
      });
    };
  }, [subscribe]);

  return connectionFailed;
}
