import { test, expect } from '@playwright/test';

const EMAIL = process.env.TEST_USER_EMAIL || 'test@example.com';
const PASSWORD = process.env.TEST_USER_PASSWORD || 'TestPassword123!';

test.describe('Hull Sizing Wizard', () => {
  test('should respect taxonomy selections and generate consistent ShipD variants', async ({ page }) => {
    // Login
    await page.goto('/login');
    await page.getByLabel(/email/i).fill(EMAIL);
    await page.getByLabel(/password/i).fill(PASSWORD);
    await page.getByRole('button', { name: /sign in|login/i }).click();
    await expect(page).toHaveURL(/dashboard/, { timeout: 15_000 });

    // Open Hull Sizing app
    await page.getByRole('button', { name: /open hull sizing/i }).click();
    await expect(page).toHaveURL(/\/sizing\/missions/, { timeout: 10_000 });

    // Start new brief
    await page.getByRole('button', { name: /new brief/i }).click();
    await expect(page).toHaveURL(/\/sizing\/wizard/, { timeout: 10_000 });
    await expect(page.getByText('Hull Sizing Wizard')).toBeVisible();

    // Validate category options
    const categoryOptions = await page.locator('select#missionCategory option').allTextContents();
    expect(categoryOptions.map((opt) => opt.trim())).toEqual(
      expect.arrayContaining(['Commercial', 'Government', 'Recreational', 'Research'])
    );

    // Select Recreational -> Yacht
    await page.selectOption('select#missionCategory', { value: 'Recreational' });
    await page.selectOption('select#missionType', { value: 'yacht' });

    // Verify mission types react to category change
    const recreationalTypes = await page.locator('select#missionType option').allTextContents();
    expect(recreationalTypes.some((opt) => opt.includes('Yacht'))).toBeTruthy();

    await page.selectOption('select#missionCategory', { value: 'Government' });
    const governmentTypes = await page.locator('select#missionType option').allTextContents();
    expect(governmentTypes.some((opt) => opt.toLowerCase().includes('cutter'))).toBeTruthy();

    // Switch back to Recreational -> Yacht for the run
    await page.selectOption('select#missionCategory', { value: 'Recreational' });
    await page.selectOption('select#missionType', { value: 'yacht' });

    // Populate required Step 1 fields
    const missionName = `Playwright Yacht ${Date.now()}`;
    await page.fill('input#name', missionName);
    await page.selectOption('select#cargoBasis', { value: 'weight' });
    await page.fill('input#cargoWeight', '850');

    // Continue to Step 2
    await page.getByRole('button', { name: /next: hull families/i }).click();
    await expect(page.getByText('Hull Families')).toBeVisible();

    // Capture options for yacht
    const yachtBowOptions = await page.locator('select#bowFamily option').allTextContents();
    expect(yachtBowOptions).toEqual(expect.arrayContaining(['wave_piercing', 'fine_entry']));

    // Go back, switch vessel type, verify options change, then revert
    await page.getByRole('button', { name: /← previous/i }).click();
    await page.selectOption('select#missionType', { value: 'fishing_recreational' });
    await page.getByRole('button', { name: /next: hull families/i }).click();
    const fishingBowOptions = await page.locator('select#bowFamily option').allTextContents();
    expect(fishingBowOptions).toEqual(expect.arrayContaining(['straight_raked', 'fine_entry']));
    expect(fishingBowOptions).not.toEqual(yachtBowOptions);

    // Revert to yacht selections
    await page.getByRole('button', { name: /← previous/i }).click();
    await page.selectOption('select#missionType', { value: 'yacht' });
    await page.getByRole('button', { name: /next: hull families/i }).click();

    // Choose explicit families
    await page.selectOption('select#bowFamily', { value: 'wave_piercing' });
    await page.selectOption('select#midshipFamily', { value: 'deep_v_midship' });
    await page.selectOption('select#sternFamily', { value: 'transom_stern' });

    // Proceed to speed/environment
    await page.getByRole('button', { name: /next: speed & environment/i }).click();
    await page.fill('#serviceSpeedKn', '24');
    await page.fill('#seaMarginPct', '15');
    await page.fill('#envHsM', '3.5');
    await page.fill('#envTzS', '7.5');
    await page.fill('#enduranceNm', '4500');
    await page.getByRole('button', { name: /next: constraints/i }).click();

    // Constraints step (optional), proceed
    await page.getByRole('button', { name: /next: options & review/i }).click();

    // Verify summary reflects chosen families
    await expect(page.getByText(/wave_piercing/i)).toBeVisible();
    await expect(page.getByText(/deep_v_midship/i)).toBeVisible();
    await expect(page.getByText(/transom_stern/i)).toBeVisible();

    // Generate hulls
    await page.getByRole('button', { name: /generate hulls/i }).click();
    await expect(page).toHaveURL(/\/sizing\/runs\//, { timeout: 120_000 });

    // Wait for candidates to load
    await expect(page.getByTestId('candidate-card-1')).toBeVisible({ timeout: 120_000 });

    const hullFamilies = await page.locator('[data-testid^="candidate-card-"]').evaluateAll((cards) =>
      cards.map((card) => card.getAttribute('data-hull-family'))
    );
    const bowFamilies = await page.locator('[data-testid^="candidate-card-"]').evaluateAll((cards) =>
      cards.map((card) => card.getAttribute('data-bow-family'))
    );
    const midshipFamilies = await page.locator('[data-testid^="candidate-card-"]').evaluateAll((cards) =>
      cards.map((card) => card.getAttribute('data-midship-family'))
    );
    const sternFamilies = await page.locator('[data-testid^="candidate-card-"]').evaluateAll((cards) =>
      cards.map((card) => card.getAttribute('data-stern-family'))
    );

    expect(new Set(hullFamilies.filter(Boolean))).toEqual(new Set(['yacht_disp']));
    expect(new Set(bowFamilies.filter(Boolean))).toEqual(new Set(['wave_piercing']));
    expect(new Set(midshipFamilies.filter(Boolean))).toEqual(new Set(['deep_v_midship']));
    expect(new Set(sternFamilies.filter(Boolean))).toEqual(new Set(['transom_stern']));
  });
});

