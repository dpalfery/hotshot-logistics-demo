import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // `next dev` otherwise writes an untracked AGENTS.md of generic Next.js agent rules into this
  // directory on every run.
  agentRules: false,

  // Production HTTPS enforcement
  async headers() {
    return [
      {
        // Apply security headers to all routes
        source: '/(.*)',
        headers: [
          // HTTPS enforcement
          {
            key: 'Strict-Transport-Security',
            value: 'max-age=31536000; includeSubDomains; preload'
          },
          // Prevent clickjacking
          {
            key: 'X-Frame-Options',
            value: 'DENY'
          },
          // Prevent MIME type sniffing
          {
            key: 'X-Content-Type-Options',
            value: 'nosniff'
          },
          // XSS protection
          {
            key: 'X-XSS-Protection',
            value: '1; mode=block'
          },
          // Referrer policy
          {
            key: 'Referrer-Policy',
            value: 'strict-origin-when-cross-origin'
          },
          // Content Security Policy for Azure AD authentication
          {
            key: 'Content-Security-Policy',
            value: [
              "default-src 'self'",
              "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://*.microsoftonline.com https://*.msecnd.net",
              "style-src 'self' 'unsafe-inline' https://*.microsoftonline.com",
              "img-src 'self' data: https: blob:",
              // Allow connections to backend API in development
              process.env.NODE_ENV === 'development' 
                ? "connect-src 'self' https://*.microsoftonline.com https://*.msecnd.net https://localhost:5001 http://localhost:5000"
                : "connect-src 'self' https://*.microsoftonline.com https://*.msecnd.net",
              "frame-src 'self' https://*.microsoftonline.com",
              "font-src 'self' data: https://*.microsoftonline.com"
            ].join('; ')
          }
        ]
      }
    ];
  },

  // Redirect HTTP to HTTPS in production
  async redirects() {
    return [
      {
        source: '/.well-known/acme-challenge/:path*',
        destination: '/.well-known/acme-challenge/:path*',
        permanent: false,
      },
      // Redirect HTTP to HTTPS for all other routes
      {
        source: '/(.*)',
        has: [
          {
            type: 'header',
            key: 'x-forwarded-proto',
            value: 'http',
          }
        ],
        destination: 'https://:splat',
        permanent: true,
      }
    ];
  },

  // Production optimizations
  compiler: {
    removeConsole: process.env.NODE_ENV === 'production',
  },

  // Enable experimental features for better security
  experimental: {
    scrollRestoration: true,
  },
};

export default nextConfig;
