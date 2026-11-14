import { test, expect } from '@playwright/test';

// Window extensions used by the auth test-mode mocks
declare global {
  interface Window {
    __BYPASS_AUTH__?: boolean;
    msalInstance?: unknown;
    useMsal?: unknown;
    useIsAuthenticated?: unknown;
    useMsalAuthentication?: unknown;
  }
}

test.describe('MSAL Authentication Flow', () => {

  test.beforeEach(async ({ page }) => {
    // Set test mode bypass flag to skip authentication
    await page.addInitScript(() => {
      window.__BYPASS_AUTH__ = true;
    });

    // Mock the environment variables for MSAL configuration
    await page.addInitScript(() => {
      window.sessionStorage.setItem('playwright-mock-env', JSON.stringify({
        NEXT_PUBLIC_AZURE_CLIENT_ID: 'mock-client-id',
        NEXT_PUBLIC_AZURE_TENANT_ID: 'mock-tenant-id',
      }));
    });

    // Mock MSAL authentication to avoid interference
    await page.addInitScript(() => {
      // Mock MSAL instance
      const mockMsalInstance = {
        getActiveAccount: () => ({ username: 'test@example.com' }),
        getAllAccounts: () => [{ username: 'test@example.com' }],
        setActiveAccount: () => {},
        addEventCallback: () => {},
        acquireTokenSilent: async () => ({ accessToken: 'test-token' }),
        acquireTokenPopup: async () => ({ accessToken: 'test-token' }),
      };

      // Replace global MSAL instance
      window.msalInstance = mockMsalInstance;

      // Mock Azure MSAL React hooks
      window.useMsal = () => ({
        instance: mockMsalInstance,
        inProgress: 'none',
        accounts: [{ username: 'test@example.com' }]
      });

      window.useIsAuthenticated = () => true;
      window.useMsalAuthentication = () => ({});
    });
  });

  test('should allow access to protected routes in test mode', async ({ page }) => {
    await page.goto('/jobs');
    // In test mode, should not redirect to login
    await expect(page).toHaveURL('/jobs');
  });

  test('should handle successful login and redirect', async ({ page }) => {
    // This test will require mocking the MSAL redirect response.
    // For now, we will just check if the login button is present.
    await page.goto('/login');
    const loginButton = page.getByRole('button', { name: 'Login' });
    await expect(loginButton).toBeVisible();
  });

  test('should manage and persist authentication state', async () => {
    // This test would involve mocking a login, then reloading the page
    // and ensuring the user is still authenticated.
    // Placeholder for now.
  });

  test('should handle logout correctly', async () => {
    // This test would involve mocking a login, then clicking the logout
    // button and verifying the user is redirected to the login page.
    // Placeholder for now.
  });

});