'use client';

import { useEffect } from 'react';
import { useMsal, useIsAuthenticated } from '@azure/msal-react';
import { useRouter, usePathname } from 'next/navigation';
import { InteractionStatus } from '@azure/msal-browser';
import { msalInstance } from '@/lib/providers';

interface AuthProviderProps {
  children: React.ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();

  const isDevelopment = process.env.NODE_ENV === 'development';
  const isTestMode = typeof window !== 'undefined' &&
    (window as typeof window & { __BYPASS_AUTH__?: boolean }).__BYPASS_AUTH__ === true;

  // Auth readiness is derived from MSAL's initialization status instead of
  // being flipped inside an effect: InteractionStatus.None means MSAL has
  // finished initializing, and auth is bypassed entirely in development or
  // test mode.
  const isAuthReady =
    isDevelopment || isTestMode || inProgress === InteractionStatus.None;

  useEffect(() => {
    if (isDevelopment || isTestMode) {
      return;
    }

    if (inProgress === InteractionStatus.None) {
      const accounts = msalInstance.getAllAccounts();
      if (accounts.length > 0) {
        msalInstance.setActiveAccount(accounts[0]);
      }
    }
  }, [inProgress, isDevelopment, isTestMode]);

  useEffect(() => {
    if (!isDevelopment && !isTestMode && isAuthReady && !isAuthenticated && pathname !== '/login') {
      const redirectUri = pathname !== '/' ? `?redirect_uri=${encodeURIComponent(pathname)}` : '';
      router.push(`/login${redirectUri}`);
    }
  }, [isAuthenticated, isAuthReady, isTestMode, isDevelopment, pathname, router]);

  if (isDevelopment || isTestMode) {
    return <>{children}</>;
  }

  if (!isAuthReady || (inProgress !== InteractionStatus.None && inProgress !== InteractionStatus.HandleRedirect)) {
    return <div>Loading...</div>;
  }

  return <>{children}</>;
}