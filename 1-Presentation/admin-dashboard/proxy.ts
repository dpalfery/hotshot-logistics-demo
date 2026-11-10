import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

// Production HTTPS enforcement proxy
export function proxy(request: NextRequest) {
  const response = NextResponse.next();

  // HTTPS enforcement in production
  if (process.env.NODE_ENV === 'production') {
    const forwardedProto = request.headers.get('x-forwarded-proto');
    const host = request.headers.get('host') || '';

    // Redirect HTTP to HTTPS
    if (forwardedProto === 'http' || (!forwardedProto && request.url.startsWith('http://'))) {
      const httpsUrl = `https://${host}${request.nextUrl.pathname}${request.nextUrl.search}`;
      return NextResponse.redirect(httpsUrl, 301);
    }

    // Additional security headers for production
    response.headers.set('Strict-Transport-Security', 'max-age=31536000; includeSubDomains; preload');
    response.headers.set('X-Frame-Options', 'DENY');
    response.headers.set('X-Content-Type-Options', 'nosniff');
    response.headers.set('X-XSS-Protection', '1; mode=block');
    response.headers.set('Referrer-Policy', 'strict-origin-when-cross-origin');

    // Enhanced CSP for production
    response.headers.set('Content-Security-Policy',
      "default-src 'self'; " +
      "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://*.microsoftonline.com https://*.msecnd.net; " +
      "style-src 'self' 'unsafe-inline' https://*.microsoftonline.com; " +
      "img-src 'self' data: https: blob:; " +
      "connect-src 'self' https://*.microsoftonline.com https://*.msecnd.net; " +
      "frame-src 'self' https://*.microsoftonline.com; " +
      "font-src 'self' data: https://*.microsoftonline.com"
    );
  }

  // Authentication bypass for static files and API routes that don't require auth
  const { pathname } = request.nextUrl;

  // Skip authentication for:
  // - Static files (favicon, images, etc.)
  // - API routes (handled by backend)
  // - Login page
  // - Public assets
  if (
    pathname.startsWith('/_next/') ||
    pathname.startsWith('/favicon.ico') ||
    pathname.startsWith('/api/') ||
    pathname === '/login' ||
    pathname.startsWith('/public/') ||
    pathname.includes('.')
  ) {
    return response;
  }

  // For protected routes, let the AuthProvider handle authentication
  // This middleware focuses on HTTPS and security headers only
  return response;
}

// Configure which paths the proxy should run on
export const config = {
  matcher: [
    /*
     * Match all request paths except for the ones starting with:
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon.ico (favicon file)
     * - public folder
     */
    '/((?!_next/static|_next/image|favicon.ico|public/).*)',
  ],
};