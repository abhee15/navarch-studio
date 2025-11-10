# E2E Tests (Playwright)

**End-to-end tests for NavArch Studio frontend**

## Overview

This directory contains Playwright-based E2E tests that validate complete user workflows from UI to backend.

## Test Files

```
e2e/
├── auth.spec.ts           # Authentication workflows
├── hydrostatics.spec.ts   # Hydrostatics complete workflow
├── catalog.spec.ts        # Catalog browsing and comparison
├── resistance.spec.ts     # Resistance analysis (TODO)
├── sizing.spec.ts         # Hull sizing workflow (TODO)
└── README.md             # This file
```

## Running Tests

### Prerequisites

```bash
# Install Playwright browsers
npx playwright install

# Or install specific browser
npx playwright install chromium
```

### Run All Tests

```bash
# Headless mode (CI)
npm run test:e2e

# Headed mode (visible browser)
npm run test:e2e:headed

# Interactive UI mode (recommended for development)
npm run test:e2e:ui
```

### Run Specific Test

```bash
# Run specific file
npx playwright test e2e/auth.spec.ts

# Run specific test by name
npx playwright test --grep "should successfully login"

# Run in debug mode
npm run test:e2e:debug
```

### View Report

```bash
# Generate and open HTML report
npm run test:e2e:report

# Or manually
npx playwright show-report
```

## Environment Configuration

Create `.env` file in `frontend/` directory:

```env
# Base URL for tests
BASE_URL=http://localhost:5173

# Test user credentials (Cognito)
TEST_USER_EMAIL=test@example.com
TEST_USER_PASSWORD=TestPassword123!
```

### Testing Against Deployed Environment

```bash
# Test against dev environment
BASE_URL=https://[cloudfront-domain] npx playwright test

# Test against staging
BASE_URL=https://staging.[domain] npx playwright test
```

## Test Structure

### Authentication Tests (auth.spec.ts)

- ✅ Display login page
- ✅ Show error for invalid credentials
- ✅ Successfully login with valid credentials
- ✅ Persist session after reload
- ✅ Logout successfully
- ✅ Navigate to signup page

### Hydrostatics Tests (hydrostatics.spec.ts)

- ✅ Navigate to vessels list
- ✅ Create new vessel
- ✅ Import offsets from CSV
- ✅ Create loadcase
- ✅ Compute hydrostatics
- ✅ Generate curves
- ✅ Export results to PDF

### Catalog Tests (catalog.spec.ts)

- ✅ Display ML catalog
- ✅ Filter by length
- ✅ Filter by B/T ratio
- ✅ Open hull detail page
- ✅ Search by name
- ✅ Add hull to comparison
- ✅ View comparison workspace
- ✅ Remove hull from comparison

## Writing Tests

### Basic Structure

```typescript
import { test, expect } from '@playwright/test';

test.describe('Feature Name', () => {
  test.beforeEach(async ({ page }) => {
    // Setup before each test
    await page.goto('/');
  });

  test('should do something', async ({ page }) => {
    // Arrange - Set up test conditions
    // Act - Perform actions
    // Assert - Verify results
  });
});
```

### Best Practices

✅ **Use data-testid attributes:**

```typescript
// Good: Explicit test identifier
await page.click('[data-testid="create-vessel-btn"]');

// Avoid: Brittle selectors
await page.click('.btn-primary'); // Changes with CSS
```

✅ **Wait for elements explicitly:**

```typescript
// Good: Wait for element
await expect(page.getByText('Vessel created')).toBeVisible({ timeout: 5000 });

// Avoid: Hard-coded delays
await page.waitForTimeout(3000); // Flaky!
```

✅ **Use semantic selectors:**

```typescript
// Good: Role-based selectors
await page.getByRole('button', { name: /create|save/i }).click();

// Good: Label-based selectors
await page.getByLabel(/email/i).fill('test@example.com');
```

✅ **Create reusable helpers:**

```typescript
// Helper function for login
async function login(page) {
  await page.goto('/login');
  await page.getByLabel(/email/i).fill(process.env.TEST_USER_EMAIL);
  await page.getByLabel(/password/i).fill(process.env.TEST_USER_PASSWORD);
  await page.getByRole('button', { name: /sign in/i }).click();
  await expect(page).toHaveURL(/\/dashboard/);
}

// Use in tests
test.beforeEach(async ({ page }) => {
  await login(page);
});
```

### Handling Async Operations

```typescript
// Wait for API response
await page.waitForResponse(response => 
  response.url().includes('/api/v1/vessels') && response.status() === 200
);

// Wait for navigation
await Promise.all([
  page.waitForNavigation(),
  page.click('a[href="/hydrostatics"]')
]);

// Wait for network idle
await page.waitForLoadState('networkidle');
```

### Debugging

```typescript
// Take screenshot
await page.screenshot({ path: 'debug.png', fullPage: true });

// Print page content
console.log(await page.content());

// Pause test execution
await page.pause();

// Enable verbose logging
DEBUG=pw:api npx playwright test
```

## Common Patterns

### Login Flow

```typescript
async function login(page) {
  const email = process.env.TEST_USER_EMAIL || 'test@example.com';
  const password = process.env.TEST_USER_PASSWORD || 'TestPassword123!';

  await page.goto('/login');
  await page.getByLabel(/email/i).fill(email);
  await page.getByLabel(/password/i).fill(password);
  await page.getByRole('button', { name: /sign in|login/i }).click();
  await expect(page).toHaveURL(/\/dashboard|\//, { timeout: 10000 });
}
```

### Form Submission

```typescript
// Fill form
await page.getByLabel(/vessel name/i).fill('Test Vessel');
await page.getByLabel(/length/i).fill('100');
await page.getByLabel(/breadth/i).fill('20');

// Submit
await page.getByRole('button', { name: /create|save/i }).click();

// Verify success
await expect(page.getByText(/vessel created|success/i)).toBeVisible();
```

### File Upload

```typescript
// Set file input
await page.setInputFiles('input[type="file"]', 'path/to/test-data.csv');

// Or use file from buffer
const buffer = Buffer.from('Station,WL0,WL1\n0,0,5\n1,2,6');
await page.setInputFiles('input[type="file"]', {
  name: 'test-offsets.csv',
  mimeType: 'text/csv',
  buffer: buffer
});
```

### Waiting for Downloads

```typescript
const downloadPromise = page.waitForEvent('download');
await page.click('button:has-text("Export PDF")');
const download = await downloadPromise;

// Verify filename
expect(download.suggestedFilename()).toMatch(/\.pdf$/);

// Save file
await download.saveAs('/path/to/save/file.pdf');
```

## Troubleshooting

### Tests Timing Out

```typescript
// Increase timeout for specific test
test('slow operation', async ({ page }) => {
  test.setTimeout(60000); // 60 seconds
  // ... test code
});

// Or in config (playwright.config.ts)
export default defineConfig({
  timeout: 30000, // 30 seconds per test
});
```

### Flaky Tests

**Common causes:**

1. **Race conditions:** Add explicit waits
2. **Slow network:** Increase timeouts
3. **Dynamic content:** Wait for specific elements
4. **Animation delays:** Disable animations in test mode

**Solutions:**

```typescript
// Retry failed tests
test.describe.configure({ retries: 2 });

// Wait for specific state
await page.waitForSelector('[data-testid="data-loaded"]');

// Disable animations (in CSS)
* { animation-duration: 0s !important; }
```

### Authentication Issues

```bash
# Verify test user exists in Cognito
aws cognito-idp list-users \
  --user-pool-id [POOL_ID] \
  --filter "email = \"test@example.com\""

# Reset test user password
aws cognito-idp admin-set-user-password \
  --user-pool-id [POOL_ID] \
  --username test@example.com \
  --password TestPassword123! \
  --permanent
```

### CI/CD Failures

```yaml
# GitHub Actions - install dependencies
- name: Install Playwright
  run: npx playwright install --with-deps chromium

# Run with retries
- name: Run E2E tests
  run: npm run test:e2e
  env:
    BASE_URL: ${{ env.CLOUDFRONT_URL }}
    TEST_USER_EMAIL: ${{ secrets.TEST_USER_EMAIL }}
    TEST_USER_PASSWORD: ${{ secrets.TEST_USER_PASSWORD }}
```

## Test Data

### Using Test Fixtures

```typescript
// Create test data
test.beforeEach(async ({ page }) => {
  // Create test vessel via API
  const response = await page.request.post('/api/v1/hydrostatics/vessels', {
    data: {
      name: 'Test Vessel',
      lengthOverall: 100,
      breadth: 20
    }
  });
  const vessel = await response.json();
  test.info().annotations.push({ type: 'vessel_id', description: vessel.id });
});

// Cleanup test data
test.afterEach(async ({ page }) => {
  // Delete test vessel
  // ...
});
```

## CI/CD Integration

### Pull Request Checks

E2E tests are **not** run on every PR (too slow).

### Manual Comprehensive Testing

E2E tests run when triggered manually via GitHub Actions:

```yaml
- name: Run E2E tests
  run: npx playwright test --reporter=html,junit
  env:
    BASE_URL: ${{ env.APP_URL }}
    TEST_USER_EMAIL: ${{ secrets.TEST_USER_EMAIL }}
    TEST_USER_PASSWORD: ${{ secrets.TEST_USER_PASSWORD }}
```

### Viewing Results

After workflow completes:

1. **HTML Report:** Download artifact or view in Actions
2. **Videos:** Download `e2e-videos-shard-*` artifacts (on failure)
3. **Screenshots:** Included in HTML report

## Performance

### Test Execution Time

- **Target:** < 30 minutes for full suite
- **Strategy:** Shard tests across 3 workers

```bash
# Shard 1/3
npx playwright test --shard=1/3

# Shard 2/3
npx playwright test --shard=2/3

# Shard 3/3
npx playwright test --shard=3/3
```

### Optimization Tips

- Use `page.goto()` sparingly (slow)
- Reuse authenticated sessions
- Run tests in parallel (`fullyParallel: true`)
- Skip unnecessary waits
- Use API for data setup (faster than UI)

## Resources

- [Playwright Documentation](https://playwright.dev/)
- [Best Practices](https://playwright.dev/docs/best-practices)
- [Debugging Guide](https://playwright.dev/docs/debug)
- [CI/CD Guide](https://playwright.dev/docs/ci)
- [Test Execution Guide](../temp/TEST_EXECUTION_GUIDE.md)

---

**Maintained by:** Engineering Team  
**Last Updated:** November 8, 2025



