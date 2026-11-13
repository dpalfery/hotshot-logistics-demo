// Azure AD Configuration Validation
const clientId = process.env.NEXT_PUBLIC_AZURE_CLIENT_ID;
const tenantId = process.env.NEXT_PUBLIC_AZURE_TENANT_ID;

// Enhanced validation for production only
if (process.env.NODE_ENV === 'production') {
  if (!clientId || clientId === 'your-client-id-here') {
    throw new Error(
      'NEXT_PUBLIC_AZURE_CLIENT_ID is not configured. Please set your Azure AD client ID in environment variables. ' +
      'Get this value from your Azure AD app registration in the Azure portal.'
    );
  }

  if (!tenantId || tenantId === 'your-tenant-id-here') {
    throw new Error(
      'NEXT_PUBLIC_AZURE_TENANT_ID is not configured. Please set your Azure AD tenant ID in environment variables. ' +
      'Get this value from your Azure AD app registration in the Azure portal.'
    );
  }

  // Validate GUID format for production
  const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
  if (!guidRegex.test(clientId)) {
    throw new Error('NEXT_PUBLIC_AZURE_CLIENT_ID is not a valid GUID format');
  }

  if (!guidRegex.test(tenantId)) {
    throw new Error('NEXT_PUBLIC_AZURE_TENANT_ID is not a valid GUID format');
  }
}

import { Configuration } from '@azure/msal-browser';

// Production-ready MSAL configuration
export const msalConfig: Configuration = {
  auth: {
    clientId: clientId!,
    authority: `https://login.microsoftonline.com/${tenantId}`,
    redirectUri: typeof window !== 'undefined' ? window.location.origin : '/',
    postLogoutRedirectUri: typeof window !== 'undefined' ? window.location.origin : '/',
    // navigateToLoginRequestUrl was removed in @azure/msal-browser v5;
    // returning to the original request URL is now the built-in behavior.
  },
  cache: {
    cacheLocation: 'sessionStorage',
    // storeAuthStateInCookie and secureCookies were removed in
    // @azure/msal-browser v5; cookie security is handled by the library.
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        // Log authentication events in production for debugging
        if (process.env.NODE_ENV === 'production') {
          if (level <= 2 && !containsPii) { // Log errors and warnings only
            console.log(`MSAL: ${message}`);
          }
        } else {
          // Log all events in development
          console.log(`MSAL ${level}: ${message}`);
        }
      },
      logLevel: process.env.NODE_ENV === 'production' ? 2 : 3, // Warning in prod, Info in dev
    },
    // windowHashTimeout, iframeHashTimeout and loadFrameTimeout were removed
    // in @azure/msal-browser v5 in favor of the bridge timeout below.
    iframeBridgeTimeout: 90000, // 90 seconds for iframe redirects
  },
};

// Enhanced login request with additional scopes for production
export const loginRequest = {
  scopes: [
    'User.Read',
    'openid',
    'profile',
    'email'
  ],
  prompt: process.env.NODE_ENV === 'production' ? 'select_account' : 'consent',
};

// Graph API configuration for user profile data
export const graphConfig = {
  graphMeEndpoint: 'https://graph.microsoft.com/v1.0/me',
  graphUserPhotoEndpoint: 'https://graph.microsoft.com/v1.0/me/photo/$value',
};

// Authentication utility functions
export const authUtils = {
  // Get the current domain for redirect URI configuration
  getCurrentDomain: (): string => {
    if (typeof window === 'undefined') return '';

    const protocol = window.location.protocol;
    const hostname = window.location.hostname;

    // In production, ensure HTTPS
    if (process.env.NODE_ENV === 'production' && protocol !== 'https:') {
      console.warn('Authentication attempted over non-HTTPS connection in production');
    }

    return `${protocol}//${hostname}${window.location.port ? ':' + window.location.port : ''}`;
  },

  // Validate redirect URI for security
  isValidRedirectUri: (uri: string): boolean => {
    try {
      const url = new URL(uri);
      const currentDomain = authUtils.getCurrentDomain();

      // In production, only allow HTTPS
      if (process.env.NODE_ENV === 'production' && url.protocol !== 'https:') {
        return false;
      }

      // Only allow redirects to the same domain or trusted domains
      const allowedDomains = [
        currentDomain,
        'https://login.microsoftonline.com',
        'https://login.live.com'
      ];

      return allowedDomains.some(domain => url.origin === domain);
    } catch {
      return false;
    }
  },

  // Get authentication token for API calls
  getAuthToken: async (): Promise<string | null> => {
    try {
      // This would be implemented with MSAL token acquisition
      // For now, return null as this needs to be implemented in the service layer
      return null;
    } catch (error) {
      console.error('Failed to get auth token:', error);
      return null;
    }
  }
};