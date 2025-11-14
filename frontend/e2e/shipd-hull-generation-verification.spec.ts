import { test, expect } from "@playwright/test";

const EMAIL = process.env.TEST_USER_EMAIL || "test@example.com";
const PASSWORD = process.env.TEST_USER_PASSWORD || "TestPassword123!";

/**
 * Comprehensive test suite for ShipD hull generation verification
 * Tests bulbous bow, different vessel types, and hull family variations
 * Run in UI mode to visually inspect generated designs
 */
test.describe("ShipD Hull Generation Verification", () => {
  test.beforeEach(async ({ page }) => {
    // Login
    await page.goto("/login");
    await page.getByLabel(/email/i).fill(EMAIL);
    await page.getByLabel(/password/i).fill(PASSWORD);
    await page.getByRole("button", { name: /sign in|login/i }).click();
    await expect(page).toHaveURL(/dashboard/, { timeout: 15_000 });

    // Open Hull Sizing app
    await page.getByRole("button", { name: /open hull sizing/i }).click();
    await expect(page).toHaveURL(/\/sizing\/missions/, { timeout: 10_000 });
  });

  test("Test 1: Bulbous Bow Generation - Container Ship", async ({ page }) => {
    test.setTimeout(180_000); // 3 minutes for this test

    // Start new brief
    await page.getByRole("button", { name: /new brief/i }).click();
    await expect(page).toHaveURL(/\/sizing\/wizard/, { timeout: 10_000 });

    // Wait for options to load and select Commercial -> Container Ship
    // Use the hidden select element which is more reliable
    await page.waitForSelector("select#missionCategory", { state: "visible" });
    await page.evaluate(() => {
      const select = document.querySelector("select#missionCategory") as HTMLSelectElement;
      if (select) select.value = "Commercial";
      select?.dispatchEvent(new Event("change", { bubbles: true }));
    });
    await page.waitForTimeout(500); // Wait for options to update

    await page.waitForSelector("select#missionType", { state: "visible" });
    await page.evaluate(() => {
      const select = document.querySelector("select#missionType") as HTMLSelectElement;
      if (select) select.value = "container";
      select?.dispatchEvent(new Event("change", { bubbles: true }));
    });
    await page.waitForTimeout(500);

    // Step 1: Mission parameters
    const missionName = `Bulbous Bow Test - Container ${Date.now()}`;
    await page.fill("input#name", missionName);
    await page.waitForSelector("select#cargoBasis", { state: "visible" });
    await page.evaluate(() => {
      const select = document.querySelector("select#cargoBasis") as HTMLSelectElement;
      if (select) select.value = "weight";
      select?.dispatchEvent(new Event("change", { bubbles: true }));
    });
    await page.fill("input#cargoWeight", "50000"); // 50,000 tons
    await page.getByRole("button", { name: /next: hull families/i }).click();

    // Step 2: Select bulbous_bow - wait for taxonomy to load
    await expect(page.getByRole("heading", { name: "Hull Families" })).toBeVisible();
    // Wait for loading indicator to disappear
    await page
      .waitForFunction(
        () => {
          const loadingText = Array.from(document.querySelectorAll("*")).find(
            (el) =>
              el.textContent?.includes("Loading taxonomy") ||
              el.textContent?.includes("Loading hull form")
          );
          return !loadingText || loadingText.textContent?.includes("Loading") === false;
        },
        { timeout: 15_000 }
      )
      .catch(() => {}); // Ignore if no loading indicator

    // Wait for select elements and try to select options
    await page.waitForSelector("select#bowFamily", { state: "attached", timeout: 10_000 });
    // Try selecting - if options aren't loaded, this will fail with a clear error
    try {
      await page.selectOption("select#bowFamily", { value: "bulbous_bow", timeout: 5_000 });
    } catch {
      // If selectOption fails, wait a bit more and try again
      await page.waitForTimeout(2000);
      await page.selectOption("select#bowFamily", { value: "bulbous_bow" });
    }

    await page.waitForSelector("select#midshipFamily", { state: "attached", timeout: 10_000 });
    try {
      await page.selectOption("select#midshipFamily", { value: "fine_midship", timeout: 5_000 });
    } catch {
      await page.waitForTimeout(2000);
      await page.selectOption("select#midshipFamily", { value: "fine_midship" });
    }

    await page.waitForSelector("select#sternFamily", { state: "attached", timeout: 10_000 });
    try {
      await page.selectOption("select#sternFamily", { value: "transom_stern", timeout: 5_000 });
    } catch {
      await page.waitForTimeout(2000);
      await page.selectOption("select#sternFamily", { value: "transom_stern" });
    }
    await page.waitForTimeout(2000); // Wait for form state to update

    // Verify form values are set by checking if button is enabled
    const nextButton = page.getByRole("button", { name: /next: speed & environment/i });
    // Wait up to 15 seconds for button to become enabled (taxonomy might be loading)
    let buttonEnabled = false;
    for (let i = 0; i < 15; i++) {
      const isEnabled = await nextButton.isEnabled();
      if (isEnabled) {
        buttonEnabled = true;
        break;
      }
      await page.waitForTimeout(1000);
    }

    if (!buttonEnabled) {
      // If button is still disabled, check what's missing
      const bowValue = await page.evaluate(() => {
        const select = document.querySelector("select#bowFamily") as HTMLSelectElement;
        return select?.value;
      });
      const midshipValue = await page.evaluate(() => {
        const select = document.querySelector("select#midshipFamily") as HTMLSelectElement;
        return select?.value;
      });
      const sternValue = await page.evaluate(() => {
        const select = document.querySelector("select#sternFamily") as HTMLSelectElement;
        return select?.value;
      });
      throw new Error(
        `Form validation failed. Bow: ${bowValue}, Midship: ${midshipValue}, Stern: ${sternValue}`
      );
    }

    await nextButton.click();
    // Check if we're on "Hull Geometry Details" step (step 2.5) - if so, skip it
    const geometryDetailsHeading = page.getByRole("heading", { name: /hull geometry details/i });
    const isGeometryStep = await geometryDetailsHeading.isVisible().catch(() => false);
    if (isGeometryStep) {
      // Skip the geometry details step - click Next
      const geometryNextButton = page.getByRole("button", { name: /next: speed & environment/i });
      await expect(geometryNextButton).toBeEnabled({ timeout: 10_000 });
      await geometryNextButton.click();
      await page.waitForTimeout(1000);
    }
    await expect(page.getByRole("heading", { name: "Speed & Environment" })).toBeVisible({
      timeout: 15_000,
    });
    await page.fill("#serviceSpeedKn", "24");
    await page.fill("#seaMarginPct", "15");
    await page.fill("#envHsM", "3.5");
    await page.fill("#envTzS", "7.5");
    await page.fill("#enduranceNm", "8000");
    await page.getByRole("button", { name: /next: constraints/i }).click();
    await page.getByRole("button", { name: /next: options & review/i }).click();

    // Verify bulbous_bow is in summary
    await expect(page.getByText(/bulbous_bow/i)).toBeVisible();

    // Generate hulls
    await page.getByRole("button", { name: /generate hulls/i }).click();
    await expect(page).toHaveURL(/\/sizing\/runs\//, { timeout: 120_000 });

    // Wait for candidates to load
    await expect(page.getByTestId("candidate-card-1")).toBeVisible({ timeout: 120_000 });

    // Verify bulbous_bow is in generated candidates
    const bowFamilies = await page
      .locator('[data-testid^="candidate-card-"]')
      .evaluateAll((cards) => cards.map((card) => card.getAttribute("data-bow-family")));
    expect(bowFamilies.filter(Boolean)).toContain("bulbous_bow");

    // Click on first candidate to view details
    await page.getByTestId("candidate-card-1").click();
    await page.waitForTimeout(2000); // Wait for 3D visualization to load

    // Verify 3D visualization is present
    const canvas = page.locator("canvas").first();
    await expect(canvas).toBeVisible({ timeout: 10_000 });

    console.log("✅ Bulbous bow test completed - check 3D visualization for bulb");
  });

  test("Test 2: Different Bow Families - Yacht", async ({ page }) => {
    test.setTimeout(180_000);

    // Start new brief
    await page.getByRole("button", { name: /new brief/i }).click();
    await expect(page).toHaveURL(/\/sizing\/wizard/, { timeout: 10_000 });

    // Select Recreational -> Yacht
    await page.waitForSelector("select#missionCategory", { state: "visible" });
    await page.evaluate(() => {
      const select = document.querySelector("select#missionCategory") as HTMLSelectElement;
      if (select) select.value = "Recreational";
      select?.dispatchEvent(new Event("change", { bubbles: true }));
    });
    await page.waitForTimeout(500);
    await page.waitForSelector("select#missionType", { state: "visible" });
    await page.evaluate(() => {
      const select = document.querySelector("select#missionType") as HTMLSelectElement;
      if (select) select.value = "yacht";
      select?.dispatchEvent(new Event("change", { bubbles: true }));
    });
    await page.waitForTimeout(500);

    // Step 1
    const missionName = `Bow Families Test - Yacht ${Date.now()}`;
    await page.fill("input#name", missionName);
    await page.waitForSelector("select#cargoBasis", { state: "visible" });
    await page.evaluate(() => {
      const select = document.querySelector("select#cargoBasis") as HTMLSelectElement;
      if (select) select.value = "weight";
      select?.dispatchEvent(new Event("change", { bubbles: true }));
    });
    await page.fill("input#cargoWeight", "50");
    await page.getByRole("button", { name: /next: hull families/i }).click();

    // Step 2: Test wave_piercing bow
    await expect(page.getByRole("heading", { name: "Hull Families" })).toBeVisible();
    await page.waitForSelector("select#bowFamily", { state: "attached" });
    await page.waitForFunction(
      () => {
        const select = document.querySelector("select#bowFamily") as HTMLSelectElement;
        return select && select.options.length > 1;
      },
      { timeout: 10_000 }
    );
    await page.selectOption("select#bowFamily", { value: "wave_piercing" });
    await page.waitForSelector("select#midshipFamily", { state: "attached" });
    await page.waitForFunction(
      () => {
        const select = document.querySelector("select#midshipFamily") as HTMLSelectElement;
        return select && select.options.length > 1;
      },
      { timeout: 10_000 }
    );
    await page.selectOption("select#midshipFamily", { value: "deep_v_midship" });
    await page.waitForSelector("select#sternFamily", { state: "attached" });
    await page.waitForFunction(
      () => {
        const select = document.querySelector("select#sternFamily") as HTMLSelectElement;
        return select && select.options.length > 1;
      },
      { timeout: 10_000 }
    );
    await page.selectOption("select#sternFamily", { value: "transom_stern" });
    await page.waitForTimeout(500);

    // Continue - handle intermediate "Hull Geometry Details" step if present
    await page.getByRole("button", { name: /next: speed & environment/i }).click();
    const geometryDetailsHeading = page.getByRole("heading", { name: /hull geometry details/i });
    const isGeometryStep = await geometryDetailsHeading.isVisible().catch(() => false);
    if (isGeometryStep) {
      const geometryNextButton = page.getByRole("button", { name: /next: speed & environment/i });
      await expect(geometryNextButton).toBeEnabled({ timeout: 10_000 });
      await geometryNextButton.click();
      await page.waitForTimeout(1000);
    }
    await expect(page.getByRole("heading", { name: "Speed & Environment" })).toBeVisible({
      timeout: 15_000,
    });
    await page.fill("#serviceSpeedKn", "20");
    await page.fill("#seaMarginPct", "10");
    await page.fill("#envHsM", "2.0");
    await page.fill("#envTzS", "6.0");
    await page.fill("#enduranceNm", "2000");
    await page.getByRole("button", { name: /next: constraints/i }).click();
    await page.getByRole("button", { name: /next: options & review/i }).click();

    // Generate
    await page.getByRole("button", { name: /generate hulls/i }).click();
    await expect(page).toHaveURL(/\/sizing\/runs\//, { timeout: 120_000 });
    await expect(page.getByTestId("candidate-card-1")).toBeVisible({ timeout: 120_000 });

    // Verify wave_piercing bow
    const bowFamilies = await page
      .locator('[data-testid^="candidate-card-"]')
      .evaluateAll((cards) => cards.map((card) => card.getAttribute("data-bow-family")));
    expect(bowFamilies.filter(Boolean)).toContain("wave_piercing");

    // View first candidate
    await page.getByTestId("candidate-card-1").click();
    await page.waitForTimeout(2000);

    console.log("✅ Wave piercing bow test completed - check 3D visualization");
  });

  test("Test 3: Different Stern Families - Tanker", async ({ page }) => {
    test.setTimeout(180_000);

    // Start new brief
    await page.getByRole("button", { name: /new brief/i }).click();
    await expect(page).toHaveURL(/\/sizing\/wizard/, { timeout: 10_000 });

    // Select Commercial -> Tanker
    await page.waitForSelector("select#missionCategory", { state: "visible" });
    await page.evaluate(() => {
      const select = document.querySelector("select#missionCategory") as HTMLSelectElement;
      if (select) select.value = "Commercial";
      select?.dispatchEvent(new Event("change", { bubbles: true }));
    });
    await page.waitForTimeout(500);
    await page.waitForSelector("select#missionType", { state: "visible" });
    await page.evaluate(() => {
      const select = document.querySelector("select#missionType") as HTMLSelectElement;
      if (select) select.value = "tanker";
      select?.dispatchEvent(new Event("change", { bubbles: true }));
    });
    await page.waitForTimeout(500);

    // Step 1
    const missionName = `Stern Families Test - Tanker ${Date.now()}`;
    await page.fill("input#name", missionName);
    await page.waitForSelector("select#cargoBasis", { state: "visible" });
    await page.evaluate(() => {
      const select = document.querySelector("select#cargoBasis") as HTMLSelectElement;
      if (select) select.value = "weight";
      select?.dispatchEvent(new Event("change", { bubbles: true }));
    });
    await page.fill("input#cargoWeight", "100000");
    await page.getByRole("button", { name: /next: hull families/i }).click();

    // Step 2: Test transom_stern
    await expect(page.getByRole("heading", { name: "Hull Families" })).toBeVisible();
    await page.waitForSelector("select#bowFamily", { state: "attached" });
    await page.waitForFunction(
      () => {
        const select = document.querySelector("select#bowFamily") as HTMLSelectElement;
        return select && select.options.length > 1;
      },
      { timeout: 10_000 }
    );
    await page.selectOption("select#bowFamily", { value: "bulbous_bow" });
    await page.waitForSelector("select#midshipFamily", { state: "attached" });
    await page.waitForFunction(
      () => {
        const select = document.querySelector("select#midshipFamily") as HTMLSelectElement;
        return select && select.options.length > 1;
      },
      { timeout: 10_000 }
    );
    await page.selectOption("select#midshipFamily", { value: "full_midship" });
    await page.waitForSelector("select#sternFamily", { state: "attached" });
    await page.waitForFunction(
      () => {
        const select = document.querySelector("select#sternFamily") as HTMLSelectElement;
        return select && select.options.length > 1;
      },
      { timeout: 10_000 }
    );
    await page.selectOption("select#sternFamily", { value: "transom_stern" });
    await page.waitForTimeout(500);

    // Continue - handle intermediate "Hull Geometry Details" step if present
    await page.getByRole("button", { name: /next: speed & environment/i }).click();
    const geometryDetailsHeading = page.getByRole("heading", { name: /hull geometry details/i });
    const isGeometryStep = await geometryDetailsHeading.isVisible().catch(() => false);
    if (isGeometryStep) {
      const geometryNextButton = page.getByRole("button", { name: /next: speed & environment/i });
      await expect(geometryNextButton).toBeEnabled({ timeout: 10_000 });
      await geometryNextButton.click();
      await page.waitForTimeout(1000);
    }
    await expect(page.getByRole("heading", { name: "Speed & Environment" })).toBeVisible({
      timeout: 15_000,
    });
    await page.fill("#serviceSpeedKn", "16");
    await page.fill("#seaMarginPct", "20");
    await page.fill("#envHsM", "4.0");
    await page.fill("#envTzS", "8.0");
    await page.fill("#enduranceNm", "10000");
    await page.getByRole("button", { name: /next: constraints/i }).click();
    await page.getByRole("button", { name: /next: options & review/i }).click();

    // Generate
    await page.getByRole("button", { name: /generate hulls/i }).click();
    await expect(page).toHaveURL(/\/sizing\/runs\//, { timeout: 120_000 });
    await expect(page.getByTestId("candidate-card-1")).toBeVisible({ timeout: 120_000 });

    // Verify transom_stern
    const sternFamilies = await page
      .locator('[data-testid^="candidate-card-"]')
      .evaluateAll((cards) => cards.map((card) => card.getAttribute("data-stern-family")));
    expect(sternFamilies.filter(Boolean)).toContain("transom_stern");

    // View first candidate
    await page.getByTestId("candidate-card-1").click();
    await page.waitForTimeout(2000);

    console.log("✅ Transom stern test completed - check 3D visualization");
  });

  test("Test 4: Different Midship Families - Fishing Vessel", async ({ page }) => {
    test.setTimeout(180_000);

    // Start new brief
    await page.getByRole("button", { name: /new brief/i }).click();
    await expect(page).toHaveURL(/\/sizing\/wizard/, { timeout: 10_000 });

    // Select Commercial -> Fishing
    await page.waitForSelector("select#missionCategory", { state: "visible" });
    await page.evaluate(() => {
      const select = document.querySelector("select#missionCategory") as HTMLSelectElement;
      if (select) select.value = "Commercial";
      select?.dispatchEvent(new Event("change", { bubbles: true }));
    });
    await page.waitForTimeout(500);
    await page.waitForSelector("select#missionType", { state: "visible" });
    await page.evaluate(() => {
      const select = document.querySelector("select#missionType") as HTMLSelectElement;
      if (select) select.value = "fishing";
      select?.dispatchEvent(new Event("change", { bubbles: true }));
    });
    await page.waitForTimeout(500);

    // Step 1
    const missionName = `Midship Families Test - Fishing ${Date.now()}`;
    await page.fill("input#name", missionName);
    await page.waitForSelector("select#cargoBasis", { state: "visible" });
    await page.evaluate(() => {
      const select = document.querySelector("select#cargoBasis") as HTMLSelectElement;
      if (select) select.value = "weight";
      select?.dispatchEvent(new Event("change", { bubbles: true }));
    });
    await page.fill("input#cargoWeight", "500");
    await page.getByRole("button", { name: /next: hull families/i }).click();

    // Step 2: Test deep_v_midship
    await expect(page.getByRole("heading", { name: "Hull Families" })).toBeVisible();
    await page.waitForSelector("select#bowFamily", { state: "attached" });
    await page.waitForFunction(
      () => {
        const select = document.querySelector("select#bowFamily") as HTMLSelectElement;
        return select && select.options.length > 1;
      },
      { timeout: 10_000 }
    );
    await page.selectOption("select#bowFamily", { value: "fine_entry" });
    await page.waitForSelector("select#midshipFamily", { state: "attached" });
    await page.waitForFunction(
      () => {
        const select = document.querySelector("select#midshipFamily") as HTMLSelectElement;
        return select && select.options.length > 1;
      },
      { timeout: 10_000 }
    );
    await page.selectOption("select#midshipFamily", { value: "deep_v_midship" });
    await page.waitForSelector("select#sternFamily", { state: "attached" });
    await page.waitForFunction(
      () => {
        const select = document.querySelector("select#sternFamily") as HTMLSelectElement;
        return select && select.options.length > 1;
      },
      { timeout: 10_000 }
    );
    await page.selectOption("select#sternFamily", { value: "canoe_stern" });
    await page.waitForTimeout(500);

    // Continue - handle intermediate "Hull Geometry Details" step if present
    await page.getByRole("button", { name: /next: speed & environment/i }).click();
    const geometryDetailsHeading = page.getByRole("heading", { name: /hull geometry details/i });
    const isGeometryStep = await geometryDetailsHeading.isVisible().catch(() => false);
    if (isGeometryStep) {
      const geometryNextButton = page.getByRole("button", { name: /next: speed & environment/i });
      await expect(geometryNextButton).toBeEnabled({ timeout: 10_000 });
      await geometryNextButton.click();
      await page.waitForTimeout(1000);
    }
    await expect(page.getByRole("heading", { name: "Speed & Environment" })).toBeVisible({
      timeout: 15_000,
    });
    await page.fill("#serviceSpeedKn", "12");
    await page.fill("#seaMarginPct", "15");
    await page.fill("#envHsM", "3.0");
    await page.fill("#envTzS", "7.0");
    await page.fill("#enduranceNm", "3000");
    await page.getByRole("button", { name: /next: constraints/i }).click();
    await page.getByRole("button", { name: /next: options & review/i }).click();

    // Generate
    await page.getByRole("button", { name: /generate hulls/i }).click();
    await expect(page).toHaveURL(/\/sizing\/runs\//, { timeout: 120_000 });
    await expect(page.getByTestId("candidate-card-1")).toBeVisible({ timeout: 120_000 });

    // Verify deep_v_midship
    const midshipFamilies = await page
      .locator('[data-testid^="candidate-card-"]')
      .evaluateAll((cards) => cards.map((card) => card.getAttribute("data-midship-family")));
    expect(midshipFamilies.filter(Boolean)).toContain("deep_v_midship");

    // View first candidate
    await page.getByTestId("candidate-card-1").click();
    await page.waitForTimeout(2000);

    console.log("✅ Deep V midship test completed - check 3D visualization");
  });

  test("Test 5: Complete Vessel Type Variations", async ({ page }) => {
    test.setTimeout(240_000); // 4 minutes for comprehensive test

    const vesselTypes = [
      {
        category: "Commercial",
        type: "container",
        bow: "bulbous_bow",
        midship: "fine_midship",
        stern: "transom_stern",
      },
      {
        category: "Commercial",
        type: "tanker",
        bow: "bulbous_bow",
        midship: "full_midship",
        stern: "transom_stern",
      },
      {
        category: "Recreational",
        type: "yacht",
        bow: "wave_piercing",
        midship: "deep_v_midship",
        stern: "transom_stern",
      },
      {
        category: "Commercial",
        type: "fishing",
        bow: "fine_entry",
        midship: "deep_v_midship",
        stern: "canoe_stern",
      },
    ];

    for (const vessel of vesselTypes) {
      console.log(`\n🧪 Testing ${vessel.category} - ${vessel.type}...`);

      // Start new brief
      await page.getByRole("button", { name: /new brief/i }).click();
      await expect(page).toHaveURL(/\/sizing\/wizard/, { timeout: 10_000 });

      // Select vessel type
      await page.waitForSelector("select#missionCategory", { state: "visible" });
      await page.evaluate((category) => {
        const select = document.querySelector("select#missionCategory") as HTMLSelectElement;
        if (select) select.value = category;
        select?.dispatchEvent(new Event("change", { bubbles: true }));
      }, vessel.category);
      await page.waitForTimeout(500);
      await page.waitForSelector("select#missionType", { state: "visible" });
      await page.evaluate((type) => {
        const select = document.querySelector("select#missionType") as HTMLSelectElement;
        if (select) select.value = type;
        select?.dispatchEvent(new Event("change", { bubbles: true }));
      }, vessel.type);
      await page.waitForTimeout(500);

      // Step 1
      const missionName = `${vessel.type} Test ${Date.now()}`;
      await page.fill("input#name", missionName);
      await page.waitForSelector("select#cargoBasis", { state: "visible" });
      await page.evaluate(() => {
        const select = document.querySelector("select#cargoBasis") as HTMLSelectElement;
        if (select) select.value = "weight";
        select?.dispatchEvent(new Event("change", { bubbles: true }));
      });
      await page.fill(
        "input#cargoWeight",
        vessel.type === "yacht" ? "50" : vessel.type === "fishing" ? "500" : "50000"
      );
      await page.getByRole("button", { name: /next: hull families/i }).click();

      // Step 2: Select families
      await expect(page.getByRole("heading", { name: "Hull Families" })).toBeVisible();
      await page.waitForSelector("select#bowFamily", { state: "attached" });
      await page.waitForFunction(
        () => {
          const select = document.querySelector("select#bowFamily") as HTMLSelectElement;
          return select && select.options.length > 1;
        },
        { timeout: 10_000 }
      );
      await page.selectOption("select#bowFamily", { value: vessel.bow });
      await page.waitForSelector("select#midshipFamily", { state: "attached" });
      await page.waitForFunction(
        () => {
          const select = document.querySelector("select#midshipFamily") as HTMLSelectElement;
          return select && select.options.length > 1;
        },
        { timeout: 10_000 }
      );
      await page.selectOption("select#midshipFamily", { value: vessel.midship });
      await page.waitForSelector("select#sternFamily", { state: "attached" });
      await page.waitForFunction(
        () => {
          const select = document.querySelector("select#sternFamily") as HTMLSelectElement;
          return select && select.options.length > 1;
        },
        { timeout: 10_000 }
      );
      await page.selectOption("select#sternFamily", { value: vessel.stern });
      await page.waitForTimeout(500);

      // Continue
      await page.getByRole("button", { name: /next: speed & environment/i }).click();
      await expect(page.getByRole("heading", { name: "Speed & Environment" })).toBeVisible();
      await page.fill("#serviceSpeedKn", vessel.type === "yacht" ? "20" : "16");
      await page.fill("#seaMarginPct", "15");
      await page.fill("#envHsM", "3.5");
      await page.fill("#envTzS", "7.5");
      await page.fill("#enduranceNm", vessel.type === "yacht" ? "2000" : "8000");
      await page.getByRole("button", { name: /next: constraints/i }).click();
      await page.getByRole("button", { name: /next: options & review/i }).click();

      // Generate
      await page.getByRole("button", { name: /generate hulls/i }).click();
      await expect(page).toHaveURL(/\/sizing\/runs\//, { timeout: 120_000 });
      await expect(page.getByTestId("candidate-card-1")).toBeVisible({ timeout: 120_000 });

      // Verify families
      const bowFamilies = await page
        .locator('[data-testid^="candidate-card-"]')
        .evaluateAll((cards) => cards.map((card) => card.getAttribute("data-bow-family")));
      const midshipFamilies = await page
        .locator('[data-testid^="candidate-card-"]')
        .evaluateAll((cards) => cards.map((card) => card.getAttribute("data-midship-family")));
      const sternFamilies = await page
        .locator('[data-testid^="candidate-card-"]')
        .evaluateAll((cards) => cards.map((card) => card.getAttribute("data-stern-family")));

      expect(bowFamilies.filter(Boolean)).toContain(vessel.bow);
      expect(midshipFamilies.filter(Boolean)).toContain(vessel.midship);
      expect(sternFamilies.filter(Boolean)).toContain(vessel.stern);

      // View first candidate
      await page.getByTestId("candidate-card-1").click();
      await page.waitForTimeout(3000); // Wait longer for visualization

      // Verify 3D visualization
      const canvas = page.locator("canvas").first();
      await expect(canvas).toBeVisible({ timeout: 10_000 });

      console.log(`✅ ${vessel.type} test completed - families verified, 3D visualization visible`);

      // Go back to missions page for next iteration
      await page.goto("/sizing/missions");
      await page.waitForTimeout(1000);
    }

    console.log("\n✅ All vessel type variations tested successfully!");
  });
});
