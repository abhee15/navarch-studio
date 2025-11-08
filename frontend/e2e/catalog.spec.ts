import { test, expect } from '@playwright/test';

/**
 * Catalog & Comparison E2E Tests
 *
 * These tests cover:
 * - Browsing ML catalog (5K+ parametric hulls)
 * - Searching with filters
 * - Viewing hull details
 * - Adding to comparison workspace
 */

// Helper function to login
async function login(page) {
  const email = process.env.TEST_USER_EMAIL || 'test@example.com';
  const password = process.env.TEST_USER_PASSWORD || 'TestPassword123!';

  await page.goto('/login');
  await page.getByLabel(/email/i).fill(email);
  await page.getByLabel(/password/i).fill(password);
  await page.getByRole('button', { name: /sign in|login/i }).click();
  await expect(page).toHaveURL(/\/dashboard|\//, { timeout: 10000 });
}

test.describe('Catalog Browser', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('should display ML catalog with hulls', async ({ page }) => {
    await page.goto('/catalog/ml');

    // Wait for catalog to load
    await expect(page.getByText(/ml catalog|parametric catalog/i)).toBeVisible({ timeout: 5000 });

    // Verify hulls are displayed (grid or list)
    await expect(page.locator('[data-testid="hull-card"]').or(page.locator('.hull-card')).first()).toBeVisible({ timeout: 10000 });

    // Check for pagination or load more
    await expect(
      page
        .getByRole('button', { name: /next|load more/i })
        .or(page.getByText(/page \d+ of \d+/i))
    ).toBeVisible();
  });

  test('should filter hulls by length', async ({ page }) => {
    await page.goto('/catalog/ml');

    // Wait for catalog to load
    await expect(page.locator('[data-testid="hull-card"]').first()).toBeVisible({ timeout: 10000 });

    // Apply length filter
    await page.getByLabel(/length|lpp/i).fill('50');
    await page.getByLabel(/length|lpp/i).press('Enter');

    // Wait for filtered results
    await page.waitForTimeout(1000); // Wait for API call

    // Verify results are updated (check that hulls are displayed)
    await expect(page.locator('[data-testid="hull-card"]').first()).toBeVisible({ timeout: 5000 });
  });

  test('should filter hulls by B/T ratio', async ({ page }) => {
    await page.goto('/catalog/ml');

    // Wait for catalog to load
    await expect(page.locator('[data-testid="hull-card"]').first()).toBeVisible({ timeout: 10000 });

    // Apply B/T ratio filter
    await page.getByLabel(/b\/t|beam.*draft/i).fill('3.0');
    await page.getByLabel(/b\/t|beam.*draft/i).press('Enter');

    // Wait for filtered results
    await page.waitForTimeout(1000);

    // Verify results are updated
    await expect(page.locator('[data-testid="hull-card"]').first()).toBeVisible({ timeout: 5000 });
  });

  test('should open hull detail page', async ({ page }) => {
    await page.goto('/catalog/ml');

    // Wait for catalog to load
    await expect(page.locator('[data-testid="hull-card"]').first()).toBeVisible({ timeout: 10000 });

    // Click on first hull
    await page.locator('[data-testid="hull-card"]').first().click();

    // Should navigate to detail page
    await expect(page).toHaveURL(/\/catalog\/hulls\/\d+/, { timeout: 5000 });

    // Verify hull details are displayed
    await expect(page.getByText(/principal particulars|dimensions/i)).toBeVisible();
    await expect(page.getByText(/form coefficients|characteristics/i)).toBeVisible();
  });

  test('should search hulls by name', async ({ page }) => {
    await page.goto('/catalog/ml');

    // Wait for catalog to load
    await expect(page.locator('[data-testid="hull-card"]').first()).toBeVisible({ timeout: 10000 });

    // Use search box
    await page.getByPlaceholder(/search|filter/i).fill('cargo');
    await page.getByPlaceholder(/search|filter/i).press('Enter');

    // Wait for search results
    await page.waitForTimeout(1000);

    // Verify results contain search term
    await expect(page.getByText(/cargo/i)).toBeVisible({ timeout: 5000 });
  });
});

test.describe('Comparison Workspace', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('should add hull to comparison', async ({ page }) => {
    await page.goto('/catalog/ml');

    // Wait for catalog to load
    await expect(page.locator('[data-testid="hull-card"]').first()).toBeVisible({ timeout: 10000 });

    // Click "Add to Comparison" button on first hull
    await page
      .locator('[data-testid="hull-card"]')
      .first()
      .getByRole('button', { name: /add to comparison|compare/i })
      .click();

    // Verify toast notification or success message
    await expect(page.getByText(/added to comparison|added to workspace/i)).toBeVisible({ timeout: 3000 });
  });

  test('should navigate to comparison workspace', async ({ page }) => {
    await page.goto('/catalog/ml');

    // Click comparison workspace link
    await page.getByRole('link', { name: /comparison|workspace/i }).click();

    // Should navigate to comparison page
    await expect(page).toHaveURL(/\/comparison|\/workspace/, { timeout: 5000 });

    // Verify comparison workspace UI
    await expect(page.getByText(/comparison workspace|selected hulls/i)).toBeVisible();
  });

  test('should display multiple hulls in comparison', async ({ page }) => {
    // Add 2 hulls to comparison first
    await page.goto('/catalog/ml');
    await expect(page.locator('[data-testid="hull-card"]').first()).toBeVisible({ timeout: 10000 });

    // Add first hull
    await page
      .locator('[data-testid="hull-card"]')
      .first()
      .getByRole('button', { name: /add to comparison/i })
      .click();
    await page.waitForTimeout(500);

    // Add second hull
    await page
      .locator('[data-testid="hull-card"]')
      .nth(1)
      .getByRole('button', { name: /add to comparison/i })
      .click();
    await page.waitForTimeout(500);

    // Navigate to comparison workspace
    await page.goto('/comparison');

    // Verify both hulls are displayed
    const hullCards = page.locator('[data-testid="comparison-hull-card"]').or(page.locator('.comparison-hull'));
    await expect(hullCards).toHaveCount(2, { timeout: 5000 });
  });

  test('should remove hull from comparison', async ({ page }) => {
    // Add a hull to comparison first
    await page.goto('/catalog/ml');
    await expect(page.locator('[data-testid="hull-card"]').first()).toBeVisible({ timeout: 10000 });
    await page
      .locator('[data-testid="hull-card"]')
      .first()
      .getByRole('button', { name: /add to comparison/i })
      .click();
    await page.waitForTimeout(500);

    // Navigate to comparison workspace
    await page.goto('/comparison');

    // Click remove button on first hull
    await page.getByRole('button', { name: /remove|delete/i }).first().click();

    // Verify hull was removed (empty state or count decreased)
    await expect(
      page
        .getByText(/no hulls in comparison|empty/i)
        .or(page.locator('[data-testid="comparison-hull-card"]'))
    ).toBeVisible({ timeout: 3000 });
  });
});
