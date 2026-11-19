import type { Page, WebSocketRoute } from '@playwright/test';

/** SignalR's JSON protocol ends every message with this record separator. */
const RECORD_SEPARATOR = '\u001e';

interface MockSocket {
  route: WebSocketRoute;
  handshakeDone: boolean;
  closed: boolean;
}

export interface SignalRMock {
  /** Number of sockets that completed the SignalR handshake and have not closed. */
  openConnections(): number;
  /** The Authorization header of every negotiate request, in order; undefined when the request had none. */
  negotiateAuthorizations(): (string | undefined)[];
  /** The URL of every WebSocket the client opened, in order. */
  socketUrls(): string[];
  /** Invoke a client method (for example `LocationUpdate`) as the server would. Needs an open connection. */
  invoke(target: string, ...args: unknown[]): void;
}

/**
 * Replaces the SignalR server at the network level: the negotiate request and the WebSocket are
 * answered by the test, so the app runs its real SignalR client unchanged. Call it before the page
 * loads (or reload afterwards), and only in the tests that need live updates.
 */
export async function mockSignalR(page: Page): Promise<SignalRMock> {
  const sockets: MockSocket[] = [];
  const negotiateAuthorizations: (string | undefined)[] = [];

  await page.route('**/realtime/negotiate**', async route => {
    // The client sends credentials, so CORS needs the exact origin rather than a wildcard
    const headers = {
      'access-control-allow-origin': route.request().headers()['origin'] ?? '',
      'access-control-allow-credentials': 'true',
      'access-control-allow-headers': 'x-requested-with, x-signalr-user-agent, content-type, authorization',
      'access-control-allow-methods': 'POST, OPTIONS',
    };
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    negotiateAuthorizations.push(route.request().headers()['authorization']);
    await route.fulfill({
      status: 200,
      headers,
      contentType: 'application/json',
      body: JSON.stringify({
        negotiateVersion: 1,
        connectionId: 'test-connection',
        connectionToken: 'test-token',
        availableTransports: [{ transport: 'WebSockets', transferFormats: ['Text', 'Binary'] }],
      }),
    });
  });

  await page.routeWebSocket(/\/realtime\?/, route => {
    const socket: MockSocket = { route, handshakeDone: false, closed: false };
    sockets.push(socket);

    route.onMessage(message => {
      // The first message is the handshake request; answer it, then the client treats the connection as started
      if (!socket.handshakeDone && typeof message === 'string' && message.includes('"protocol"')) {
        socket.handshakeDone = true;
        route.send(`{}${RECORD_SEPARATOR}`);
      }
    });
    route.onClose(() => {
      socket.closed = true;
    });
  });

  const open = () => sockets.filter(socket => socket.handshakeDone && !socket.closed);

  return {
    openConnections: () => open().length,
    negotiateAuthorizations: () => [...negotiateAuthorizations],
    socketUrls: () => sockets.map(socket => socket.route.url()),
    invoke(target, ...args) {
      const socket = open().at(-1);
      if (!socket) {
        throw new Error(`No open SignalR connection to invoke ${target} on`);
      }
      socket.route.send(JSON.stringify({ type: 1, target, arguments: args }) + RECORD_SEPARATOR);
    },
  };
}
