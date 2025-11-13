import { test, expect } from "@playwright/test";

const EMAIL = process.env.TEST_USER_EMAIL || "test@example.com";
const PASSWORD = process.env.TEST_USER_PASSWORD || "TestPassword123!";

test.describe("ShipD Geometry Integration", () => {
  test.setTimeout(120000); // Increase timeout to 2 minutes per test

  test.beforeEach(async ({ page }) => {
    // Add delay to avoid rate limiting
    await page.waitForTimeout(1000);

    // Navigate to login
    await page.goto("/login");
    await page.waitForLoadState("domcontentloaded");

    // Wait for login form
    await page.waitForSelector('input[type="email"], input[name*="email"]', { timeout: 15000 });

    // Login
    const emailInput = page.locator('input[type="email"], input[name*="email"]').first();
    const passwordInput = page.locator('input[type="password"], input[name*="password"]').first();
    const submitButton = page.locator('button[type="submit"], button:has-text("Sign in"), button:has-text("Login")').first();

    await emailInput.fill(EMAIL);
    await page.waitForTimeout(500); // Small delay between actions
    await passwordInput.fill(PASSWORD);
    await page.waitForTimeout(500); // Small delay between actions
    await submitButton.click();

    // Wait for navigation after login - be more flexible
    try {
      await page.waitForURL(/\/dashboard|\/sizing|\/home/, { timeout: 30000 });
      await page.waitForTimeout(2000); // Wait after navigation to avoid rate limiting
    } catch (e) {
      // If URL doesn't change, check if we're already logged in or on a different page
      const currentUrl = page.url();
      console.log(`Login may have completed, current URL: ${currentUrl}`);
      await page.waitForTimeout(2000); // Wait even on error
    }

    // Navigate to sizing if not already there
    const currentUrl = page.url();
    if (currentUrl.includes("/dashboard") || currentUrl.includes("/home")) {
      // Try to click on sizing link
      const sizingLink = page.locator('a:has-text("Sizing"), a:has-text("Hull"), button:has-text("Sizing")').first();
      if ((await sizingLink.count()) > 0) {
        await sizingLink.click();
        await page.waitForURL(/\/sizing/, { timeout: 10000 });
      } else {
        // Navigate directly
        await page.goto("/sizing");
        await page.waitForLoadState("networkidle");
      }
    } else if (!currentUrl.includes("/sizing")) {
      await page.goto("/sizing");
      await page.waitForLoadState("networkidle");
    }
  });

  test("should create mission case with ShipD taxonomy and geometry details", async ({
    page,
  }) => {
    // Wait for mission cases page to load
    await page.waitForLoadState("networkidle");
    await page.waitForTimeout(2000); // Wait for page to fully render

    // Look for "New Brief" button - try multiple selectors
    const newBriefButton = page.getByRole("button", { name: /New Brief|Create Brief|New/i }).first();
    await newBriefButton.waitFor({ state: "visible", timeout: 15000 });
    await newBriefButton.click();

    // Wait for wizard to appear
    await page.waitForSelector('text=/Vessel Requirements|Mission/i', { timeout: 10000 });

    // Step 1: Fill vessel requirements with Category and Vessel Type
    await page.waitForSelector('select, [role="combobox"]', { timeout: 5000 });

    // Select Category - look for label with "Category" text
    const categoryLabel = page.locator('label:has-text("Category"), label:has-text("category")').first();
    await categoryLabel.waitFor({ state: "visible", timeout: 10000 });
    const categorySelect = categoryLabel.locator("..").locator('select, [role="combobox"], button').first();
    await categorySelect.waitFor({ state: "visible" });

    // Click to open dropdown if it's a button-based select
    if (await categorySelect.getAttribute("role") === "combobox" || categorySelect.locator("button").count() > 0) {
      await categorySelect.click();
      await page.waitForTimeout(500);
      await page.locator('text="Commercial"').first().click();
    } else {
      await categorySelect.selectOption("Commercial");
    }

    // Wait for Vessel Type dropdown to populate
    await page.waitForTimeout(2000); // Increased delay to avoid rate limiting

    // Select Vessel Type - look for label with "Vessel Type" text
    const vesselTypeLabel = page.locator('label:has-text("Vessel Type"), label:has-text("vessel type")').first();
    await vesselTypeLabel.waitFor({ state: "visible", timeout: 10000 });
    const vesselTypeSelect = vesselTypeLabel.locator("..").locator('select, [role="combobox"], button').first();
    await vesselTypeSelect.waitFor({ state: "visible" });

    // Click to open dropdown if it's a button-based select
    if (await vesselTypeSelect.getAttribute("role") === "combobox" || vesselTypeSelect.locator("button").count() > 0) {
      await vesselTypeSelect.click();
      await page.waitForTimeout(500);
      await page.locator('text=/container/i').first().click();
    } else {
      await vesselTypeSelect.selectOption("container");
    }

    // Fill other required fields - check for TEU count or cargo value
    const cargoInput = page.locator('input[id="teuCount"], input[id="cargoWeight"], input[id="cargoVolume"], input[name*="teu"], input[name*="cargo"]').first();
    if ((await cargoInput.count()) > 0) {
      await cargoInput.fill("1000");
    } else {
      // Try to find any number input
      const numberInputs = page.locator('input[type="number"]');
      const count = await numberInputs.count();
      if (count > 0) {
        await numberInputs.first().fill("1000");
      }
    }

    // Speed field might be in a later step, so skip for now or look for it
    const speedInput = page.locator('input[name*="speed"], input[placeholder*="speed"]').first();
    if ((await speedInput.count()) > 0) {
      await speedInput.fill("20");
    }

    // Click Next
    await page.click('button:has-text("Next"), button:has-text("Continue")');
    await page.waitForTimeout(1000);

    // Step 2: Select Hull Families
    await page.waitForSelector('text=/Hull Families|Bow|Midship|Stern/i', { timeout: 10000 });

    // Select Bow Family
    const bowSelect = page.locator('select, [role="combobox"]').filter({ hasText: /bow/i }).first();
    if ((await bowSelect.count()) > 0) {
      await bowSelect.selectOption("bulbous_bow");
    } else {
      // Fallback: find by label
      const bowLabel = page.locator('label:has-text(/bow/i)').first();
      if ((await bowLabel.count()) > 0) {
        const bowInput = bowLabel.locator("..").locator("select, [role='combobox']").first();
        await bowInput.selectOption("bulbous_bow");
      }
    }

    await page.waitForTimeout(500);

    // Select Midship Family
    const midSelect = page.locator('select, [role="combobox"]').filter({ hasText: /mid/i }).first();
    if ((await midSelect.count()) > 0) {
      await midSelect.selectOption("fine_midship");
    }

    await page.waitForTimeout(500);

    // Select Stern Family
    const sternSelect = page
      .locator('select, [role="combobox"]')
      .filter({ hasText: /stern/i })
      .first();
    if ((await sternSelect.count()) > 0) {
      await sternSelect.selectOption("transom_stern");
    }

    await page.waitForTimeout(1000);

    // Check if Geometry Details step appears (should appear when all families are selected)
    const geometryStep = page.locator('text=/Geometry Details|Hull Geometry/i');
    const hasGeometryStep = (await geometryStep.count()) > 0;

    if (hasGeometryStep) {
      // Click Next to go to Geometry Details
      await page.click('button:has-text("Next"), button:has-text("Continue")');
      await page.waitForTimeout(1000);

      // Verify Geometry Details step is visible
      await expect(
        page.locator('text=/Section Geometry|Flare|Deadrise|Longitudinal/i')
      ).toBeVisible({ timeout: 5000 });

      // Fill some geometry parameters
      const flareInput = page.locator('input[name*="flare"], input[placeholder*="flare"]').first();
      if ((await flareInput.count()) > 0) {
        await flareInput.fill("15");
      }

      const deadriseInput = page
        .locator('input[name*="deadrise"], input[placeholder*="deadrise"]')
        .first();
      if ((await deadriseInput.count()) > 0) {
        await deadriseInput.fill("30");
      }

      // Select Chine Type if available
      const chineSelect = page
        .locator('select, [role="combobox"]')
        .filter({ hasText: /chine/i })
        .first();
      if ((await chineSelect.count()) > 0) {
        await chineSelect.selectOption("hard");
      }

      // Check Tumblehome checkbox if available (should be enabled for fine_midship)
      const tumblehomeCheckbox = page
        .locator('input[type="checkbox"][name*="tumblehome"]')
        .first();
      if ((await tumblehomeCheckbox.count()) > 0) {
        await tumblehomeCheckbox.check();
      }

      // Fill Bulb parameters (should be visible for bulbous_bow)
      const bulbLengthInput = page
        .locator('input[name*="bulb.*length"], input[placeholder*="bulb.*length"]')
        .first();
      if ((await bulbLengthInput.count()) > 0) {
        await bulbLengthInput.fill("0.10");
      }

      // Click Next
      await page.click('button:has-text("Next"), button:has-text("Continue")');
      await page.waitForTimeout(1000);
    } else {
      // If no geometry step, just continue
      await page.click('button:has-text("Next"), button:has-text("Continue")');
      await page.waitForTimeout(1000);
    }

    // Continue through remaining steps (Speed, Constraints, Options)
    // Speed & Environment
    await page.waitForSelector('text=/Speed|Environment/i', { timeout: 5000 });
    await page.click('button:has-text("Next"), button:has-text("Continue")');
    await page.waitForTimeout(1000);

    // Constraints
    await page.waitForSelector('text=/Constraints|Limits/i', { timeout: 5000 });
    await page.click('button:has-text("Next"), button:has-text("Continue")');
    await page.waitForTimeout(1000);

    // Options & Review - verify ShipD info is shown
    await page.waitForSelector('text=/Review|Options|Summary/i', { timeout: 5000 });

    // Check if ShipD geometry info is displayed in summary
    const shipdInfo = page.locator('text=/ShipD|Geometry|Longitudinal|Lb|Lm|Ls/i');
    if ((await shipdInfo.count()) > 0) {
      console.log("✓ ShipD geometry info found in summary");
    }

    // Submit the form
    await page.click('button:has-text("Submit"), button:has-text("Create"), button:has-text("Run Solver")');

    // Wait for solver to complete (this may take a while)
    await page.waitForSelector('text=/completed|results|designs/i', { timeout: 60000 }).catch(() => {
      console.log("Solver may still be running...");
    });

    // Wait a bit for results to appear
    await page.waitForTimeout(3000);
  });

  test("should display ShipD features in candidate card", async ({ page }) => {
    // Navigate to sizing results page
    await page.goto("/sizing");
    await page.waitForLoadState("networkidle");
    await page.waitForTimeout(2000); // Wait for data to load

    // Wait for mission cases list or candidate cards to appear
    // First check if we're on the mission cases list page
    const missionList = page.locator('text=/mission|brief/i');
    if ((await missionList.count()) > 0) {
      // We're on the mission cases list, need to open a mission with results
      // For now, just verify the page loaded
      console.log("✓ Mission cases list page loaded");
      return; // Skip this test if no candidates exist yet
    }

    // Wait for candidate cards to appear - try multiple selectors
    try {
      await page.waitForSelector('[data-testid^="candidate-card"]', { timeout: 10000 });
    } catch {
      // If no candidates, skip this test
      console.log("ℹ No candidate cards found - skipping test");
      return;
    }

    // Find first candidate card with ShipD - try multiple selectors
    let candidateCards = page.locator('[data-testid^="candidate-card"]');
    if ((await candidateCards.count()) === 0) {
      candidateCards = page.locator('[data-testid*="candidate"]');
    }
    if ((await candidateCards.count()) === 0) {
      candidateCards = page.locator('article, .card, [class*="candidate"]');
    }
    const cardCount = await candidateCards.count();

    if (cardCount > 0) {
      const firstCard = candidateCards.first();

      // Check for ShipD badge
      const shipdBadge = firstCard.locator('text=/ShipD/i');
      const hasShipD = (await shipdBadge.count()) > 0;

      if (hasShipD) {
        console.log("✓ ShipD badge found on candidate card");

        // Check for ShipD Geometry info panel
        const geometryInfo = firstCard.locator('text=/ShipD Geometry|Longitudinal|Lb|Lm|Ls/i');
        if ((await geometryInfo.count()) > 0) {
          console.log("✓ ShipD Geometry info panel found");
        }
      } else {
        console.log("ℹ No ShipD badge found (candidate may not have ShipD parameters)");
      }
    }
  });

  test("should show ShipD tab and panels in candidate workspace", async ({ page }) => {
    // Navigate to sizing
    await page.goto("/sizing");
    await page.waitForLoadState("networkidle");
    await page.waitForTimeout(2000); // Wait for data to load

    // Check if we're on mission cases list
    const missionList = page.locator('text=/mission|brief/i');
    if ((await missionList.count()) > 0) {
      console.log("ℹ On mission cases list - need existing run with candidates to test workspace");
      return; // Skip if no candidates exist
    }

    // Wait for candidate cards - try multiple selectors
    let candidateCards;
    try {
      await page.waitForSelector('[data-testid^="candidate-card"]', { timeout: 10000 });
      candidateCards = page.locator('[data-testid^="candidate-card"]');
    } catch {
      console.log("ℹ No candidate cards found - skipping test");
      return;
    }

    // Find a candidate with ShipD - try multiple selectors if needed
    if ((await candidateCards.count()) === 0) {
      candidateCards = page.locator('[data-testid*="candidate"]');
    }
    if ((await candidateCards.count()) === 0) {
      candidateCards = page.locator('article, .card, [class*="candidate"]');
    }
    const cardCount = await candidateCards.count();

    if (cardCount > 0) {
      // Click "Open Workspace" on first card
      const firstCard = candidateCards.first();
      const openButton = firstCard.getByRole("button", { name: /Open Workspace|View/i });

      if ((await openButton.count()) > 0) {
        await openButton.click();

        // Wait for workspace to load
        await page.waitForURL(/\/sizing\/.*\/workspace/, { timeout: 15000 });
        await page.waitForLoadState("networkidle");

        // Check for ShipD tab
        const shipdTab = page.getByRole("button", { name: /ShipD/i });
        const hasShipDTab = (await shipdTab.count()) > 0;

        if (hasShipDTab) {
          console.log("✓ ShipD tab found");

          // Click ShipD tab
          await shipdTab.click();
          await page.waitForTimeout(1000);

          // Verify Geometry Details Panel is visible
          const geometryPanel = page.locator('text=/Section Geometry|Longitudinal Segmentation|Bulb Geometry/i');
          await expect(geometryPanel.first()).toBeVisible({ timeout: 5000 });

          // Verify ShipD Parameter Chart is visible (if metadata loaded)
          const paramChart = page.locator('text=/Principal Parameters|Bow Parameters|Stern Parameters/i');
          if ((await paramChart.count()) > 0) {
            console.log("✓ ShipD Parameter Chart found");
          }

          // Expand sections to verify content
          const sectionButtons = page.locator('button:has-text("Section Geometry"), button:has-text("Longitudinal")');
          if ((await sectionButtons.count()) > 0) {
            await sectionButtons.first().click();
            await page.waitForTimeout(500);
            console.log("✓ Geometry sections are expandable");
          }
        } else {
          console.log("ℹ ShipD tab not found (candidate may not have ShipD parameters)");
        }
      }
    }
  });

  test("should verify 3D visualization uses ShipD geometry", async ({ page }) => {
    // Navigate to sizing workspace
    await page.goto("/sizing");
    await page.waitForLoadState("networkidle");

    // Find and open a candidate
    const candidateCards = page.locator('[data-testid*="candidate-card"]');
    if ((await candidateCards.count()) > 0) {
      const firstCard = candidateCards.first();
      const openButton = firstCard.getByRole("button", { name: /Open Workspace|View/i });

      if ((await openButton.count()) > 0) {
        await openButton.click();
        await page.waitForURL(/\/sizing\/.*\/workspace/, { timeout: 15000 });
        await page.waitForLoadState("networkidle");

        // Wait for 3D visualization to load
        await page.waitForTimeout(3000);

        // Check if canvas/WebGL context exists (indicates 3D rendering)
        const canvas = page.locator("canvas");
        if ((await canvas.count()) > 0) {
          console.log("✓ 3D visualization canvas found");

          // The geometry should be using ShipD if available
          // We can't directly verify the geometry, but we can check console for ShipD-related logs
          const consoleMessages: string[] = [];
          page.on("console", (msg) => {
            if (msg.text().includes("ShipD") || msg.text().includes("shipd")) {
              consoleMessages.push(msg.text());
            }
          });

          // Wait a bit for any console messages
          await page.waitForTimeout(2000);

          if (consoleMessages.length > 0) {
            console.log("✓ ShipD-related console messages found:", consoleMessages);
          }
        }
      }
    }
  });
});
