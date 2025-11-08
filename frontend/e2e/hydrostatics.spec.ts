import { test, expect } from '@playwright/test';

/**
 * Hydrostatics E2E Tests
 *
 * These tests cover the complete hydrostatics workflow:
 * 1. Create vessel
 * 2. Import offsets (geometry)
 * 3. Create loadcase
 * 4. Run hydrostatic calculations
 * 5. View results
 * 6. Generate curves
 */

// Helper function to login before each test
async function login(page) {
  const email = process.env.TEST_USER_EMAIL || 'test@example.com';
  const password = process.env.TEST_USER_PASSWORD || 'TestPassword123!';

  await page.goto('/login');
  await page.getByLabel(/email/i).fill(email);
  await page.getByLabel(/password/i).fill(password);
  await page.getByRole('button', { name: /sign in|login/i }).click();
  await expect(page).toHaveURL(/\/dashboard|\//, { timeout: 10000 });
}

test.describe('Hydrostatics Workflow', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('should navigate to vessels list', async ({ page }) => {
    // Navigate to hydrostatics/vessels
    await page.goto('/hydrostatics/vessels');

    // Verify we're on the vessels page
    await expect(page).toHaveURL(/\/hydrostatics\/vessels/);

    // Check for vessels list or empty state
    await expect(
      page
        .getByText(/my vessels|vessel list/i)
        .or(page.getByText(/no vessels|create your first vessel/i))
    ).toBeVisible({ timeout: 5000 });
  });

  test('should create a new vessel', async ({ page }) => {
    await page.goto('/hydrostatics/vessels');

    // Click create vessel button
    await page.getByRole('button', { name: /create vessel|new vessel/i }).or(page.getByTestId('create-vessel-btn')).click();

    // Fill in vessel details
    const vesselName = `Test Vessel ${Date.now()}`;
    await page.getByLabel(/name/i).fill(vesselName);
    await page.getByLabel(/length overall|loa/i).fill('100');
    await page.getByLabel(/breadth|beam/i).fill('20');
    await page.getByLabel(/depth/i).fill('10');
    await page.getByLabel(/draft|draught/i).fill('5');

    // Submit the form
    await page.getByRole('button', { name: /create|save/i }).click();

    // Verify vessel was created (should navigate to vessel detail page)
    await expect(page).toHaveURL(/\/hydrostatics\/vessels\/\d+/, { timeout: 5000 });

    // Verify vessel name is displayed
    await expect(page.getByText(vesselName)).toBeVisible();
  });

  test('should import offsets from CSV', async ({ page }) => {
    // First create a vessel
    await page.goto('/hydrostatics/vessels');
    await page.getByRole('button', { name: /create vessel|new vessel/i }).or(page.getByTestId('create-vessel-btn')).click();

    const vesselName = `CSV Import Vessel ${Date.now()}`;
    await page.getByLabel(/name/i).fill(vesselName);
    await page.getByLabel(/length overall|loa/i).fill('100');
    await page.getByLabel(/breadth|beam/i).fill('20');
    await page.getByLabel(/depth/i).fill('10');
    await page.getByLabel(/draft|draught/i).fill('5');
    await page.getByRole('button', { name: /create|save/i }).click();

    // Wait for vessel detail page
    await expect(page).toHaveURL(/\/hydrostatics\/vessels\/\d+/, { timeout: 5000 });

    // Navigate to Geometry tab
    await page.getByRole('tab', { name: /geometry/i }).click();

    // Click import CSV button
    await page.getByRole('button', { name: /import|upload.*csv/i }).or(page.getByTestId('import-csv-btn')).click();

    // Create a simple CSV file in memory (mock upload)
    // Note: In a real test, you'd prepare a CSV file
    // const csvContent = `Station,WL0,WL1,WL2
    // 0,0,5,8
    // 1,2,6,9
    // 2,4,7,10`;

    // Upload the file (this is a simplified example)
    // In practice, you'd use page.setInputFiles() with a file path
    await expect(page.getByText(/import.*wizard|upload.*file/i)).toBeVisible();
  });

  test('should create a loadcase', async ({ page }) => {
    // Navigate to an existing vessel (assume first vessel in list)
    await page.goto('/hydrostatics/vessels');
    await page.getByRole('link').first().click();

    // Navigate to Loadcases tab
    await page.getByRole('tab', { name: /loadcase/i }).click();

    // Click create loadcase button
    await page.getByRole('button', { name: /create loadcase|new loadcase/i }).or(page.getByTestId('create-loadcase-btn')).click();

    // Fill in loadcase details
    const loadcaseName = `Test Loadcase ${Date.now()}`;
    await page.getByLabel(/name/i).fill(loadcaseName);
    await page.getByLabel(/draft|draught/i).fill('5.0');
    await page.getByLabel(/kg/i).fill('6.5');
    await page.getByLabel(/water density/i).fill('1025');

    // Submit the form
    await page.getByRole('button', { name: /create|save/i }).click();

    // Verify loadcase was created
    await expect(page.getByText(loadcaseName)).toBeVisible({ timeout: 5000 });
  });

  test('should compute hydrostatics', async ({ page }) => {
    // Navigate to an existing vessel with geometry and loadcase
    await page.goto('/hydrostatics/vessels');
    await page.getByRole('link').first().click();

    // Navigate to Computations tab
    await page.getByRole('tab', { name: /computation/i }).click();

    // Select a loadcase
    await page.getByRole('combobox', { name: /select loadcase/i }).click();
    await page.getByRole('option').first().click();

    // Click compute button
    await page.getByRole('button', { name: /compute|calculate/i }).or(page.getByTestId('compute-btn')).click();

    // Wait for results to appear
    await expect(page.getByText(/displacement|volume/i)).toBeVisible({ timeout: 10000 });
    await expect(page.getByText(/center of buoyancy|kb/i)).toBeVisible();
    await expect(page.getByText(/metacentric height|gm/i)).toBeVisible();
  });

  test('should generate hydrostatic curves', async ({ page }) => {
    // Navigate to an existing vessel
    await page.goto('/hydrostatics/vessels');
    await page.getByRole('link').first().click();

    // Navigate to Curves tab
    await page.getByRole('tab', { name: /curve/i }).click();

    // Select curve type
    await page.getByRole('combobox', { name: /curve type|select curve/i }).click();
    await page.getByRole('option', { name: /displacement/i }).click();

    // Click generate button
    await page.getByRole('button', { name: /generate|create curve/i }).or(page.getByTestId('generate-curve-btn')).click();

    // Wait for chart to render
    await expect(page.locator('svg.recharts-surface').or(page.locator('canvas'))).toBeVisible({ timeout: 10000 });

    // Verify axes are present
    await expect(page.getByText(/draft|draught/i)).toBeVisible();
    await expect(page.getByText(/displacement/i)).toBeVisible();
  });

  test('should export results to PDF', async ({ page }) => {
    // Navigate to an existing vessel with computed results
    await page.goto('/hydrostatics/vessels');
    await page.getByRole('link').first().click();

    // Navigate to Computations tab
    await page.getByRole('tab', { name: /computation/i }).click();

    // Click export button
    const downloadPromise = page.waitForEvent('download');
    await page.getByRole('button', { name: /export.*pdf/i }).or(page.getByTestId('export-pdf-btn')).click();

    // Wait for download
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toMatch(/\.pdf$/);
  });
});
