import { test, expect } from '@playwright/test';

/**
 * Authentication E2E Tests
 *
 * These tests cover the complete authentication flow including:
 * - User login
 * - Session management
 * - Error handling
 * - Logout
 */

test.describe('Authentication', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the app
    await page.goto('/');
  });

  test('should display login page for unauthenticated users', async ({ page }) => {
    // Check if we're on the login page
    await expect(page).toHaveURL(/\/login/);

    // Verify login form elements exist
    await expect(page.getByLabel(/email/i)).toBeVisible();
    await expect(page.getByLabel(/password/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /sign in|login/i })).toBeVisible();
  });

  test('should show error for invalid credentials', async ({ page }) => {
    await page.goto('/login');

    // Fill in invalid credentials
    await page.getByLabel(/email/i).fill('invalid@example.com');
    await page.getByLabel(/password/i).fill('wrongpassword');

    // Click login button
    await page.getByRole('button', { name: /sign in|login/i }).click();

    // Wait for error message
    await expect(page.getByText(/invalid|incorrect|failed/i)).toBeVisible({ timeout: 5000 });
  });

  test('should successfully login with valid credentials', async ({ page }) => {
    // Use test credentials from environment variables
    const email = process.env.TEST_USER_EMAIL || 'test@example.com';
    const password = process.env.TEST_USER_PASSWORD || 'TestPassword123!';

    await page.goto('/login');

    // Fill in credentials
    await page.getByLabel(/email/i).fill(email);
    await page.getByLabel(/password/i).fill(password);

    // Click login button
    await page.getByRole('button', { name: /sign in|login/i }).click();

    // Wait for redirect to dashboard
    await expect(page).toHaveURL(/\/dashboard|\//, { timeout: 10000 });

    // Verify user is logged in (check for user menu or profile)
    await expect(
      page.getByRole('button', { name: /profile|account|user/i }).or(page.getByTestId('user-menu'))
    ).toBeVisible({ timeout: 5000 });
  });

  test('should persist session after page reload', async ({ page }) => {
    // Login first
    const email = process.env.TEST_USER_EMAIL || 'test@example.com';
    const password = process.env.TEST_USER_PASSWORD || 'TestPassword123!';

    await page.goto('/login');
    await page.getByLabel(/email/i).fill(email);
    await page.getByLabel(/password/i).fill(password);
    await page.getByRole('button', { name: /sign in|login/i }).click();

    // Wait for successful login
    await expect(page).toHaveURL(/\/dashboard|\//, { timeout: 10000 });

    // Reload the page
    await page.reload();

    // User should still be logged in (should not redirect to login)
    await expect(page).not.toHaveURL(/\/login/);
    await expect(
      page.getByRole('button', { name: /profile|account|user/i }).or(page.getByTestId('user-menu'))
    ).toBeVisible({ timeout: 5000 });
  });

  test('should logout successfully', async ({ page }) => {
    // Login first
    const email = process.env.TEST_USER_EMAIL || 'test@example.com';
    const password = process.env.TEST_USER_PASSWORD || 'TestPassword123!';

    await page.goto('/login');
    await page.getByLabel(/email/i).fill(email);
    await page.getByLabel(/password/i).fill(password);
    await page.getByRole('button', { name: /sign in|login/i }).click();

    // Wait for successful login
    await expect(page).toHaveURL(/\/dashboard|\//, { timeout: 10000 });

    // Click user menu
    await page.getByRole('button', { name: /profile|account|user/i }).or(page.getByTestId('user-menu')).click();

    // Click logout button
    await page.getByRole('menuitem', { name: /logout|sign out/i }).or(page.getByTestId('logout-button')).click();

    // Should redirect to login page
    await expect(page).toHaveURL(/\/login/, { timeout: 5000 });
  });

  test('should navigate to signup page', async ({ page }) => {
    await page.goto('/login');

    // Click signup link
    await page.getByRole('link', { name: /sign up|create account|register/i }).click();

    // Should navigate to signup page
    await expect(page).toHaveURL(/\/signup|\/register/);

    // Verify signup form elements
    await expect(page.getByLabel(/email/i)).toBeVisible();
    await expect(page.getByLabel(/password/i)).toBeVisible();
  });
});
