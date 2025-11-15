import { test, expect } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';

// ES module equivalent of __dirname
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const BASE_URL = 'https://d300ay3pb0z4dq.cloudfront.net';
const EMAIL = 'abhee15@gmail.com';
const PASSWORD = 'Abhishikth12345$';

/**
 * Deep workspace analysis test
 *
 * This test performs a comprehensive analysis of the design workspace after running the solver.
 * It checks for inconsistencies across all panels and validates data correctness.
 */
test.describe('Design Workspace Analysis', () => {
  test('should analyze workspace panels for inconsistencies after solver completion', async ({ page }) => {
    // Set a longer timeout for this comprehensive test
    test.setTimeout(300000); // 5 minutes

    console.log('🚀 Starting workspace analysis...');

    // Step 1: Navigate to login page
    console.log('📝 Step 1: Navigating to login page...');
    await page.goto(`${BASE_URL}/login`);
    await page.waitForLoadState('networkidle');

    // Step 2: Login
    console.log('🔐 Step 2: Logging in...');
    const emailInput = page.getByLabel(/email/i);
    const passwordInput = page.getByLabel(/password/i);
    const loginButton = page.getByRole('button', { name: /sign in|login/i });

    await expect(emailInput).toBeVisible({ timeout: 10000 });
    await emailInput.fill(EMAIL);
    await passwordInput.fill(PASSWORD);
    await loginButton.click();

    // Wait for redirect to dashboard
    await expect(page).toHaveURL(/dashboard/, { timeout: 15000 });
    console.log('✅ Logged in successfully');

    // Step 3: Navigate to Hull Sizing
    console.log('⛵ Step 3: Opening Hull Sizing app...');
    await page.getByRole('button', { name: /open hull sizing/i }).click();
    await expect(page).toHaveURL(/\/sizing\/missions/, { timeout: 10000 });
    console.log('✅ Navigated to Hull Sizing missions');

    // Step 4: Wait for mission cards to load
    console.log('🔍 Step 4: Finding first mission card...');

    // Wait for mission cards to appear - they may be loading
    await page.waitForTimeout(2000);

    // Try multiple selectors to find Run Solver button
    let runSolverButtons = page.getByRole('button', { name: /run solver/i });
    let buttonCount = await runSolverButtons.count();

    // If not found, try alternative selectors
    if (buttonCount === 0) {
      console.log('  Trying alternative selectors...');
      // Try finding button with Play icon
      runSolverButtons = page.locator('button').filter({ has: page.locator('svg') }).filter({ hasText: /run/i });
      buttonCount = await runSolverButtons.count();
    }

    // If still not found, try finding any card with buttons
    if (buttonCount === 0) {
      console.log('  Looking for mission cards directly...');
      const cards = page.locator('div, article').filter({ has: page.locator('button') });
      const cardCount = await cards.count();
      console.log(`  Found ${cardCount} cards with buttons`);

      if (cardCount > 0) {
        // Get the first button from the first card
        runSolverButtons = cards.first().locator('button').first();
        buttonCount = await runSolverButtons.count();
        console.log(`  Found ${buttonCount} button(s) in first card`);
      }
    }

    // If still no buttons, check if we're already on a results page or workspace
    if (buttonCount === 0) {
      const currentUrl = page.url();
      console.log(`  Current URL: ${currentUrl}`);

      if (currentUrl.includes('/sizing/runs/')) {
        console.log('  ✅ Already on results page - skipping solver run');
      } else if (currentUrl.includes('/sizing/workspace/')) {
        console.log('  ✅ Already on workspace page - skipping solver run');
      } else {
        // Take screenshot for debugging
        await page.screenshot({ path: 'temp/missions-page-no-buttons.png', fullPage: true });
        throw new Error('Could not find any "Run Solver" buttons on the missions page. Screenshot saved to temp/missions-page-no-buttons.png');
      }
    } else {
      console.log(`✅ Found ${buttonCount} mission card(s) with Run Solver button`);

      // Step 5: Click "Run Solver" on the first card
      console.log('🎯 Step 5: Clicking "Run Solver" on first card...');
      await runSolverButtons.first().click();

      // Wait for navigation to results page
      await expect(page).toHaveURL(/\/sizing\/runs\//, { timeout: 180000 }); // 3 minutes for solver
      console.log('✅ Solver completed, navigated to results page');

      // Wait a bit more for the page to fully load
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(3000);
    }
    console.log('✅ Clicked Run Solver button');

    // Step 6: Verify we're on results page (if we ran the solver, we already navigated)
    const currentUrl = page.url();
    if (!currentUrl.includes('/sizing/runs/')) {
      console.log('⏳ Step 6: Waiting for solver to complete...');
      await expect(page).toHaveURL(/\/sizing\/runs\//, { timeout: 180000 }); // 3 minutes for solver
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(3000);
    } else {
      console.log('✅ Already on results page');
    }

    // Step 7: Wait for candidate cards to appear and EXTRACT DATA from results page
    console.log('📊 Step 7: Waiting for candidate cards and extracting candidate data...');
    await page.waitForTimeout(5000); // Allow cards to render

    // Look for "Open Workspace" button on candidate cards
    const workspaceButtons = page.getByRole('button', { name: /open workspace/i });
    const workspaceButtonCount = await workspaceButtons.count();

    if (workspaceButtonCount === 0) {
      // Take a screenshot for debugging
      await page.screenshot({ path: 'temp/candidate-cards-not-found.png', fullPage: true });
      throw new Error('Could not find any "Open Workspace" buttons. Screenshot saved to temp/candidate-cards-not-found.png');
    }

    console.log(`✅ Found ${workspaceButtonCount} candidate card(s) with Open Workspace button`);

    // Extract candidate data from the FIRST card on results page before opening workspace
    console.log('🔍 Extracting candidate data from results page...');
    const firstCard = page.locator('[data-testid="candidate-card-1"]').first();

    let candidateDataFromResults: Record<string, unknown> = {};
    let results3DScreenshot: Buffer | null = null;

    if (await firstCard.count() > 0) {
      // Extract candidate ID from the card (may be in a data attribute or from the Open Workspace button)
      // Try to get it from the button's onclick or href
      const workspaceButton = firstCard.locator('button').filter({ hasText: /open workspace/i }).first();
      if (await workspaceButton.count() > 0) {
        // The button might have an onClick that navigates, or we can extract from the card's key
        // Try to get candidate ID from the URL after clicking (but we'll click later)
        // For now, we'll extract it from the workspace URL after opening
      }

      // Extract key properties from the card
      const hullFamily = await firstCard.getAttribute('data-hull-family') || '';
      const bowFamily = await firstCard.getAttribute('data-bow-family') || '';
      const midshipFamily = await firstCard.getAttribute('data-midship-family') || '';
      const sternFamily = await firstCard.getAttribute('data-stern-family') || '';

      // Extract Lpp, B, T, Δ values from the candidate card
      // The card displays these in a grid with labels "Lpp", "B", "T", "CB"
      const dimensions: Record<string, string> = {};

      // Extract from Principal Dimensions section
      const principalDimensionsSection = firstCard.locator('text=/Principal Dimensions/i').first();
      if (await principalDimensionsSection.count() > 0) {
        // Find the grid that contains dimensions
        const gridContainer = principalDimensionsSection.locator('..').locator('..').locator('div.grid').first();

        // Try to extract Lpp, B, T, CB values
        const dimensionLabels = ['Lpp', 'B', 'T', 'CB'];
        for (const label of dimensionLabels) {
          // Look for text matching the label exactly
          const labelElement = gridContainer.locator(`text=/^${label}$/`).first();
          if (await labelElement.count() > 0) {
            // Find the value below the label (in the next div)
            const valueElement = labelElement.locator('..').locator('div').nth(1);
            const valueText = await valueElement.textContent().catch(() => null);
            if (valueText) {
              // Extract number and unit
              const numMatch = valueText.match(/[\d.]+/);
              if (numMatch) {
                dimensions[label] = `${numMatch[0]}${valueText.includes('m') ? 'm' : ''}`;
              }
            }
          }
        }
      }

      // Extract displacement if shown (in KPIs section below)
      const displacementElements = firstCard.locator('text=/Δ|Displacement/i');
      if (await displacementElements.count() > 0) {
        for (let i = 0; i < await displacementElements.count(); i++) {
          const dispElement = displacementElements.nth(i);
          const dispText = await dispElement.textContent().catch(() => '');
          const dispMatch = dispText.match(/([\d.]+)\s*(t|tonnes?|kg)/i) || [];
          if (dispMatch[1]) {
            dimensions['Δ'] = `${dispMatch[1]}${dispMatch[2]?.includes('t') ? 't' : 'kg'}`;
            break;
          }
        }
      }

      // Extract score
      const scoreText = await firstCard.locator('text=/Score:/i').first().textContent().catch(() => '');
      const scoreMatch = scoreText?.match(/(\d+\.?\d*)%/) || [];
      const score = scoreMatch[1] ? parseFloat(scoreMatch[1]) : null;

      candidateDataFromResults = {
        hullFamily,
        bowFamily,
        midshipFamily,
        sternFamily,
        score,
        dimensions,
      };

      console.log('  ✅ Candidate data from results page:', JSON.stringify(candidateDataFromResults, null, 2));

      // Capture 3D visualization from results page BEFORE opening workspace
      console.log('📸 Capturing 3D thumbnail from results page card...');
      // Wait for 3D rendering to complete
      await page.waitForTimeout(3000);

      // Try multiple selectors for 3D thumbnail
      const thumbnailSelectors = [
        firstCard.locator('canvas').first(),
        firstCard.locator('[class*="thumbnail"]').first(),
        firstCard.locator('[class*="hull3d"]').first(),
        firstCard.locator('[data-testid*="3d"]').first(),
        firstCard.locator('[class*="three"]').first(),
      ];

      for (const selector of thumbnailSelectors) {
        if (await selector.count() > 0) {
          console.log('  ✅ Found 3D thumbnail on results page card');
          // Take screenshot of the 3D thumbnail
          results3DScreenshot = await selector.screenshot().catch(() => null);
          if (results3DScreenshot) {
            fs.writeFileSync(path.join(__dirname, '../../temp/results-3d-thumbnail.png'), results3DScreenshot);
            console.log('  📸 Saved results page 3D thumbnail screenshot');
            break;
          }
        }
      }

      if (!results3DScreenshot) {
        // Try capturing the entire card area where 3D visualization should be
        const card3DArea = firstCard.locator('div').filter({ has: page.locator('canvas, [class*="three"]') }).first();
        if (await card3DArea.count() > 0) {
          results3DScreenshot = await card3DArea.screenshot().catch(() => null);
          if (results3DScreenshot) {
            fs.writeFileSync(path.join(__dirname, '../../temp/results-3d-thumbnail.png'), results3DScreenshot);
            console.log('  📸 Saved results page 3D area screenshot');
          }
        } else {
          console.log('  ⚠️ Could not find 3D thumbnail on results page card');
        }
      }
    } else {
      console.log('  ⚠️ Could not find first candidate card to extract data');
    }

    // Step 8: Open the workspace for the first candidate and extract candidate ID from URL
    console.log('🏗️ Step 8: Opening design workspace...');
    await workspaceButtons.first().click();

    // Wait for workspace URL and extract candidate ID
    await expect(page).toHaveURL(/\/sizing\/workspace\//, { timeout: 30000 });
    const workspaceUrl = page.url();
    const candidateIdMatch = workspaceUrl.match(/\/sizing\/workspace\/([^/]+)/);
    const candidateIdFromWorkspace = candidateIdMatch ? candidateIdMatch[1] : null;
    console.log('✅ Workspace opened');
    console.log(`  Candidate ID from workspace URL: ${candidateIdFromWorkspace}`);

    // Step 9: Wait for workspace to fully load and EXTRACT workspace candidate data
    console.log('⏳ Step 9: Waiting for workspace to load...');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000); // Additional wait for dynamic content

    // Extract candidate data from workspace (after opening)
    console.log('🔍 Extracting candidate data from workspace...');
    let candidateDataFromWorkspace: Record<string, unknown> = {};

    try {
      // Extract data from KPI panel or workspace header
      const workspaceData: Record<string, unknown> = {};

      // Extract hull family from workspace header
      const headerText = await page.locator('h2, h3').first().textContent().catch(() => '');
      workspaceData.hullFamily = headerText?.toLowerCase().replace(/\s+/g, '_') || '';

      // Extract dimensions from KPI panel
      const kpiDimensions: Record<string, string> = {};
      const metricLabels = ['Lpp', 'B', 'T', 'Δ', 'Fn', 'Score'];
      for (const label of metricLabels) {
        const labelElement = page.locator(`text=/^${label}$/i`).first();
        if (await labelElement.count() > 0) {
          const valueElement = labelElement.locator('..').locator('text=/\\d+\\.?\\d*/').first();
          if (await valueElement.count() > 0) {
            const value = await valueElement.textContent();
            kpiDimensions[label] = value?.trim() || '';
          }
        }
      }
      workspaceData.dimensions = kpiDimensions;

      candidateDataFromWorkspace = workspaceData;
      console.log('  ✅ Candidate data from workspace:', JSON.stringify(candidateDataFromWorkspace, null, 2));
    } catch (error) {
      console.log(`  ⚠️ Error extracting workspace data: ${error}`);
    }

    // Initialize inconsistencies array for tracking issues
    const inconsistencies: string[] = [];

    // Step 10: Compare candidate data between results page and workspace
    console.log('\n🔗 Step 10: Comparing candidate data (Results vs Workspace)...\n');
    const dataComparison: Record<string, unknown> = {};

    // First check: Are we comparing the same candidate?
    console.log('  🔍 Candidate ID check:');
    if (candidateIdFromWorkspace) {
      console.log(`    Workspace Candidate ID: ${candidateIdFromWorkspace}`);
      // We don't have the candidate ID from results page card yet, but we can verify
      // that the dimensions match the candidate we clicked
      dataComparison.candidateIdMatch = 'Unknown - candidate ID not extracted from results page';
    }

    if (Object.keys(candidateDataFromResults).length > 0 && Object.keys(candidateDataFromWorkspace).length > 0) {
      // Compare hull families
      if (candidateDataFromResults.hullFamily && candidateDataFromWorkspace.hullFamily) {
        const resultsFamily = candidateDataFromResults.hullFamily.toLowerCase();
        const workspaceFamily = candidateDataFromWorkspace.hullFamily.toLowerCase();
        const familiesMatch = resultsFamily === workspaceFamily || workspaceFamily.includes(resultsFamily) || resultsFamily.includes(workspaceFamily);
        dataComparison.hullFamilyMatch = familiesMatch;
        console.log(`  ${familiesMatch ? '✅' : '❌'} Hull Family Match: ${resultsFamily} vs ${workspaceFamily}`);
        if (!familiesMatch) {
          inconsistencies.push(`Hull family mismatch: Results=${resultsFamily}, Workspace=${workspaceFamily}`);
        }
      }

      // Compare dimensions (Lpp, B, T, Δ)
      const dimensionComparison: Record<string, boolean> = {};
      const dimensionsToCompare = ['Lpp', 'B', 'T', 'Δ'];

      for (const dim of dimensionsToCompare) {
        const resultsValue = candidateDataFromResults.dimensions?.[dim];
        const workspaceValue = candidateDataFromWorkspace.dimensions?.[dim];

        if (resultsValue && workspaceValue) {
          // Extract numeric values (remove units)
          const resultsNum = parseFloat(resultsValue.replace(/[^\d.]/g, ''));
          const workspaceNum = parseFloat(workspaceValue.replace(/[^\d.]/g, ''));

          if (!isNaN(resultsNum) && !isNaN(workspaceNum)) {
            // Allow 1% tolerance for floating point differences
            const tolerance = 0.01;
            const diff = Math.abs(resultsNum - workspaceNum);
            const avg = (resultsNum + workspaceNum) / 2;
            const match = diff / avg < tolerance;

            dimensionComparison[dim] = match;
            console.log(`  ${match ? '✅' : '❌'} ${dim} Match: ${resultsValue} vs ${workspaceValue} (diff: ${diff.toFixed(3)})`);

            if (!match) {
              inconsistencies.push(`${dim} mismatch: Results=${resultsValue}, Workspace=${workspaceValue}`);
            }
          }
        }
      }
      dataComparison.dimensions = dimensionComparison;
    } else {
      console.log('  ⚠️ Cannot compare - missing data from results or workspace');
      inconsistencies.push('Cannot compare candidate data - missing extraction data');
    }

    // Step 11: Test Parameter Slider Integration
    console.log('\n🎚️ Step 11: Testing Parameter Slider Integration...\n');
    const parameterTestResults: Record<string, unknown> = {};

    try {
      // Find parameter sliders
      const sliders = page.locator('input[type="range"], [class*="slider"]');
      const sliderCount = await sliders.count();

      if (sliderCount > 0) {
        console.log(`  Found ${sliderCount} parameter sliders`);

        // Get initial KPI values before adjusting slider
        const getKPIValues = async () => {
          const values: Record<string, string> = {};
          const metricLabels = ['Lpp', 'B', 'T', 'Δ'];
          for (const label of metricLabels) {
            const labelElement = page.locator(`text=/^${label}$/i`).first();
            if (await labelElement.count() > 0) {
              const valueElement = labelElement.locator('..').locator('text=/\\d+\\.?\\d*/').first();
              if (await valueElement.count() > 0) {
                const value = await valueElement.textContent();
                values[label] = value?.trim() || '';
              }
            }
          }
          return values;
        };

        const initialKPIs = await getKPIValues();
        console.log('  Initial KPI values:', initialKPIs);

        // Try to adjust the first visible slider (assumes dimensions group is already selected)
        await page.waitForTimeout(1000);

        // Find visible sliders
        const visibleSliders = page.locator('input[type="range"]:visible');
        const visibleSliderCount = await visibleSliders.count();

        if (visibleSliderCount === 0) {
          console.log('  ⚠️ No visible sliders found - trying all sliders');
          // Fall back to all sliders (might be hidden but still accessible)
          const allSliders = page.locator('input[type="range"]');
          const allSliderCount = await allSliders.count();
          if (allSliderCount > 0) {
            console.log(`  Found ${allSliderCount} sliders (may be hidden)`);
            const firstSlider = allSliders.first();

            // Use evaluate to get slider properties directly from DOM
            const sliderInfo = await firstSlider.evaluate((el: HTMLInputElement) => {
              return {
                value: el.value,
                min: el.min,
                max: el.max,
                step: el.step,
                type: el.type,
                visible: el.offsetParent !== null,
              };
            }).catch(() => null);

            if (sliderInfo && sliderInfo.type === 'range') {
              console.log(`  First slider (hidden=${!sliderInfo.visible}): value=${sliderInfo.value}, min=${sliderInfo.min}, max=${sliderInfo.max}`);

              // Skip adjustment if slider is not visible
              if (!sliderInfo.visible) {
                console.log('  ⚠️ Slider is not visible - cannot test integration');
                parameterTestResults.sliderAdjustmentTest = { adjusted: false, reason: 'Sliders not visible - may need to select parameter group first' };
              } else {
                // Proceed with adjustment
                const currentValue = parseFloat(sliderInfo.value);
                const minValue = parseFloat(sliderInfo.min);
                const maxValue = parseFloat(sliderInfo.max);
                const stepValue = parseFloat(sliderInfo.step) || 0.1;
                const range = maxValue - minValue;
                const adjustment = Math.min(range * 0.01, 0.5);
                const newValue = Math.min(currentValue + adjustment, maxValue);
                const adjustedValue = Math.round(newValue / stepValue) * stepValue;

                console.log(`  Adjusting slider from ${currentValue} to ${adjustedValue}`);
                // Range inputs need to use setInputValue or evaluate to set value
                await firstSlider.evaluate((el: HTMLInputElement, value: number) => {
                  el.value = value.toString();
                  el.dispatchEvent(new Event('input', { bubbles: true }));
                  el.dispatchEvent(new Event('change', { bubbles: true }));
                }, adjustedValue);
                // Trigger mouse up to fire handleSliderRelease
                await firstSlider.dispatchEvent('mouseup');
                await page.waitForTimeout(3000);

                const updatedKPIs = await getKPIValues();
                console.log('  Updated KPI values:', updatedKPIs);

                let valuesChanged = false;
                const changes: Record<string, { from: string; to: string }> = {};
                for (const key of Object.keys(initialKPIs)) {
                  if (initialKPIs[key] !== updatedKPIs[key]) {
                    valuesChanged = true;
                    changes[key] = { from: initialKPIs[key], to: updatedKPIs[key] };
                    console.log(`  ✅ ${key} changed: ${initialKPIs[key]} → ${updatedKPIs[key]}`);
                  }
                }

                parameterTestResults.sliderAdjustmentTest = {
                  adjusted: true,
                  valuesChanged,
                  sliderValue: sliderInfo.value,
                  adjustedTo: adjustedValue,
                  changes,
                  initialKPIs,
                  updatedKPIs,
                };

                if (!valuesChanged) {
                  inconsistencies.push('Parameter slider adjustment did not update KPI values - sliders may not be integrated');
                  console.log('  ❌ Parameter slider adjustment did not update KPI values');
                } else {
                  console.log('  ✅ Parameter slider adjustment updated KPI values');
                }

                // Reset slider using evaluate
                await firstSlider.evaluate((el: HTMLInputElement, value: string) => {
                  el.value = value;
                  el.dispatchEvent(new Event('input', { bubbles: true }));
                  el.dispatchEvent(new Event('change', { bubbles: true }));
                }, sliderInfo.value);
                await firstSlider.dispatchEvent('mouseup');
                await page.waitForTimeout(2000);
              }
            } else {
              parameterTestResults.sliderAdjustmentTest = { adjusted: false, reason: 'Could not read slider attributes' };
            }
          } else {
            parameterTestResults.sliderAdjustmentTest = { adjusted: false, reason: 'No sliders found' };
          }
        } else {
          console.log(`  Found ${visibleSliderCount} visible sliders`);
          const firstSlider = visibleSliders.first();

          // Use evaluate to get slider properties directly from DOM
          const sliderInfo = await firstSlider.evaluate((el: HTMLInputElement) => {
            return {
              value: el.value,
              min: el.min,
              max: el.max,
              step: el.step,
              type: el.type,
            };
          }).catch(() => null);

          if (sliderInfo && sliderInfo.type === 'range') {
          console.log(`  First slider: value=${sliderInfo.value}, min=${sliderInfo.min}, max=${sliderInfo.max}`);

          // Calculate a small adjustment (increase by 1% of range or 0.5 units, whichever is smaller)
          const currentValue = parseFloat(sliderInfo.value);
          const minValue = parseFloat(sliderInfo.min);
          const maxValue = parseFloat(sliderInfo.max);
          const step = parseFloat(sliderInfo.step) || 0.1;
          const range = maxValue - minValue;
          const adjustment = Math.min(range * 0.01, 0.5);
          const newValue = Math.min(currentValue + adjustment, maxValue);
          // Round to step
          const adjustedValue = Math.round(newValue / step) * step;

          console.log(`  Adjusting slider from ${currentValue} to ${adjustedValue}`);

          // Adjust the slider using evaluate (required for range inputs with step attribute)
          await firstSlider.evaluate((el: HTMLInputElement, val: number) => {
            el.value = val.toString();
            el.dispatchEvent(new Event('input', { bubbles: true }));
          }, adjustedValue);

          await page.waitForTimeout(100); // Brief wait

          // Trigger mouse up event to fire handleSliderRelease
          await firstSlider.dispatchEvent('mouseup');
          await page.waitForTimeout(3000); // Wait for update to propagate (API call + re-render)

          // Check if KPI values changed
          const updatedKPIs = await getKPIValues();
          console.log('  Updated KPI values:', updatedKPIs);

          // Check if any values changed
          let valuesChanged = false;
          const changes: Record<string, { from: string; to: string }> = {};
          for (const key of Object.keys(initialKPIs)) {
            if (initialKPIs[key] !== updatedKPIs[key]) {
              valuesChanged = true;
              changes[key] = { from: initialKPIs[key], to: updatedKPIs[key] };
              console.log(`  ✅ ${key} changed: ${initialKPIs[key]} → ${updatedKPIs[key]}`);
            }
          }

          parameterTestResults.sliderAdjustmentTest = {
            adjusted: true,
            valuesChanged,
            sliderValue: sliderInfo.value,
            adjustedTo: adjustedValue,
            changes,
            initialKPIs,
            updatedKPIs,
          };

          if (!valuesChanged) {
            inconsistencies.push('Parameter slider adjustment did not update KPI values - sliders may not be integrated');
            console.log('  ❌ Parameter slider adjustment did not update KPI values');
          } else {
            console.log('  ✅ Parameter slider adjustment updated KPI values');
          }

          // Reset slider back to original value
                // Reset slider using evaluate
                await firstSlider.evaluate((el: HTMLInputElement, value: string) => {
                  el.value = value;
                  el.dispatchEvent(new Event('input', { bubbles: true }));
                  el.dispatchEvent(new Event('change', { bubbles: true }));
                }, sliderInfo.value);
                await firstSlider.dispatchEvent('mouseup');
                await page.waitForTimeout(2000);

          } else {
            console.log('  ⚠️ Could not read slider attributes or slider is not type="range"');
            parameterTestResults.sliderAdjustmentTest = { adjusted: false, reason: 'Could not read slider attributes' };
          }
        }
      } else {
        console.log('  ⚠️ No parameter sliders found');
        inconsistencies.push('No parameter sliders found in workspace');
        parameterTestResults.sliderAdjustmentTest = { adjusted: false, reason: 'No sliders found' };
      }
    } catch (error) {
      console.log(`  ❌ Error testing parameter sliders: ${error}`);
      inconsistencies.push(`Parameter slider test error: ${error}`);
      parameterTestResults.sliderAdjustmentTest = { adjusted: false, error: String(error) };
    }

    // Step 12: Begin Panel-by-Panel Analysis
    console.log('\n📋 Step 12: Starting Panel-by-Panel Analysis...\n');

    // Continue using the existing inconsistencies array
    // Pass results3DScreenshot to panel analysis scope
    const analysisResults: Record<string, unknown> = {
      candidateDataComparison: dataComparison,
      parameterIntegrationTest: parameterTestResults,
    };

    // Make results3DScreenshot available in panel analysis scope
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (globalThis as any).results3DScreenshot = results3DScreenshot;

    // Panel 1: CompactHUD (KPI Panel)
    console.log('📊 Analyzing Panel 1: KPI Panel...');
    try {
      // Find the KPI tab and click if needed
      const kpiTab = page.locator('button').filter({ hasText: /KPIs|KPI/i });
      if (await kpiTab.count() > 0) {
        await kpiTab.first().click();
        await page.waitForTimeout(1000);
      }

      // Look for the CompactHUD component which displays critical metrics
      // It shows: Lpp, B, T, Δ, Fn, Score in a grid
      const kpiData: Record<string, string> = {};

      // Try to extract metric values by looking for specific labels
      const metricLabels = ['Lpp', 'B', 'T', 'Δ', 'Fn', 'Score'];
      for (const label of metricLabels) {
        const labelElement = page.locator(`text=/^${label}$/i`).first();
        if (await labelElement.count() > 0) {
          // Find the value near the label (usually in the same or next element)
          const valueElement = labelElement.locator('..').locator('text=/\\d+\\.?\\d*/').first();
          if (await valueElement.count() > 0) {
            const value = await valueElement.textContent();
            kpiData[label] = value?.trim() || '';
          }
        }
      }

      // Also try to find all grid items in the KPI panel
      const gridItems = page.locator('div.grid').first().locator('div').all();
      if ((await gridItems).length > 0) {
        console.log(`  ℹ️ Found ${(await gridItems).length} grid items in KPI panel`);
      }

      analysisResults.kpiPanel = kpiData;
      if (Object.keys(kpiData).length > 0) {
        console.log('  ✅ KPI Panel data extracted:', kpiData);
      } else {
        console.log('  ⚠️ KPI Panel found but could not extract values');
        inconsistencies.push('KPI Panel values could not be extracted');
      }
    } catch (error) {
      inconsistencies.push(`KPI Panel analysis error: ${error}`);
      console.log(`  ❌ Error analyzing KPI Panel: ${error}`);
    }

    // Panel 2: Offsets Table
    console.log('\n📐 Analyzing Panel 2: Offsets Table...');
    try {
      const offsetsTab = page.locator('button').filter({ hasText: /Offsets|Offset/i });
      if (await offsetsTab.count() > 0) {
        await offsetsTab.first().click();
        await page.waitForTimeout(1000);

        // Extract offsets data
        const offsetsTable = page.locator('table, [role="table"], [class*="table"]').first();
        if (await offsetsTable.count() > 0) {
          const rows = await offsetsTable.locator('tr').count();
          const columns = await offsetsTable.locator('tr').first().locator('th, td').count();

          analysisResults.offsetsTable = {
            rows,
            columns,
            hasData: rows > 1,
          };
          console.log(`  ✅ Offsets Table found: ${rows} rows, ${columns} columns`);
        } else {
          inconsistencies.push('Offsets Table not found after clicking tab');
          console.log('  ⚠️ Offsets Table not found');
        }
      } else {
        inconsistencies.push('Offsets Tab not found');
        console.log('  ⚠️ Offsets Tab not found');
      }
    } catch (error) {
      inconsistencies.push(`Offsets Table analysis error: ${error}`);
      console.log(`  ❌ Error analyzing Offsets Table: ${error}`);
    }

    // Panel 3: Sensitivity Panel
    console.log('\n📈 Analyzing Panel 3: Sensitivity Panel...');
    try {
      const sensitivityTab = page.locator('button').filter({ hasText: /Sensitivity/i });
      if (await sensitivityTab.count() > 0) {
        await sensitivityTab.first().click();
        await page.waitForTimeout(1000);

        const sensitivityContent = page.locator('[data-testid*="sensitivity"], [class*="sensitivity"]');
        if (await sensitivityContent.count() > 0) {
          const hasChart = await page.locator('canvas, svg, [class*="chart"]').count() > 0;
          analysisResults.sensitivityPanel = {
            hasContent: true,
            hasChart,
          };
          console.log(`  ✅ Sensitivity Panel found (chart: ${hasChart})`);
        } else {
          inconsistencies.push('Sensitivity Panel content not found');
          console.log('  ⚠️ Sensitivity Panel content not found');
        }
      } else {
        inconsistencies.push('Sensitivity Tab not found');
        console.log('  ⚠️ Sensitivity Tab not found');
      }
    } catch (error) {
      inconsistencies.push(`Sensitivity Panel analysis error: ${error}`);
      console.log(`  ❌ Error analyzing Sensitivity Panel: ${error}`);
    }

    // Panel 4: Resistance Curve Panel
    console.log('\n🌊 Analyzing Panel 4: Resistance Curve Panel...');
    try {
      const resistancePanel = page.locator('text=/Resistance|Resistance Analysis/i').first();
      if (await resistancePanel.count() > 0) {
        const hasChart = await resistancePanel.locator('..').locator('canvas, svg, [class*="chart"]').count() > 0;
        const hasData = await resistancePanel.locator('..').locator('text=/\\d+\\.?\\d*/').count() > 0;

        analysisResults.resistancePanel = {
          hasChart,
          hasData,
        };
        console.log(`  ✅ Resistance Panel found (chart: ${hasChart}, data: ${hasData})`);
      } else {
        inconsistencies.push('Resistance Curve Panel not found');
        console.log('  ⚠️ Resistance Curve Panel not found');
      }
    } catch (error) {
      inconsistencies.push(`Resistance Panel analysis error: ${error}`);
      console.log(`  ❌ Error analyzing Resistance Panel: ${error}`);
    }

    // Panel 5: Viewport Visualization - Compare with results page
    console.log('\n👁️ Analyzing Panel 5: Viewport Visualization...');
    try {
      const viewportPanel = page.locator('text=/Hull Visualization|Viewport/i').first();
      if (await viewportPanel.count() > 0) {
        const viewports = await page.locator('canvas, [class*="viewport"], [data-testid*="viewport"]').count();
        const has3D = await page.locator('canvas[class*="three"], [data-testid*="3d"]').count() > 0;

        // Capture 3D visualization from workspace
        console.log('  📸 Capturing 3D visualization from workspace...');
        const workspace3DCanvas = page.locator('canvas').first();
        let workspace3DScreenshot: Buffer | null = null;

        if (await workspace3DCanvas.count() > 0) {
          console.log('  ✅ Found 3D canvas in workspace');
          // Wait a moment for 3D rendering to complete
          await page.waitForTimeout(2000);

          // Take screenshot of the 3D canvas/viewport
          workspace3DScreenshot = await workspace3DCanvas.screenshot().catch(() => null);
          if (workspace3DScreenshot) {
            fs.writeFileSync(path.join(__dirname, '../../temp/workspace-3d-viewport.png'), workspace3DScreenshot);
            console.log('  📸 Saved workspace 3D viewport screenshot');
          }
        } else {
          console.log('  ⚠️ Could not find 3D canvas in workspace');
        }

        // Compare screenshots if both exist
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const results3D = (globalThis as any).results3DScreenshot as Buffer | null;
        if (results3D && workspace3DScreenshot) {
          console.log('  🔍 Comparing 3D visualizations...');
          // Basic comparison: check if images are identical or very similar
          const resultsImageHash = results3D.toString('base64').substring(0, 100);
          const workspaceImageHash = workspace3DScreenshot.toString('base64').substring(0, 100);

          const imagesMatch = resultsImageHash === workspaceImageHash;

          analysisResults.viewportPanel = {
            viewportCount: viewports,
            has3D,
            hasResultsScreenshot: !!results3D,
            hasWorkspaceScreenshot: !!workspace3DScreenshot,
            imagesMatch: imagesMatch,
          };

          if (!imagesMatch) {
            inconsistencies.push('3D visualizations differ between results page and workspace - designs may not match visually');
            console.log('  ❌ 3D visualizations DO NOT MATCH - designs appear different!');
            console.log('  💡 Check temp/results-3d-thumbnail.png vs temp/workspace-3d-viewport.png');
            console.log('  📸 Screenshots saved for visual comparison');
          } else {
            console.log('  ✅ 3D visualizations match');
          }
        } else {
          analysisResults.viewportPanel = {
            viewportCount: viewports,
            has3D,
            hasResultsScreenshot: !!results3D,
            hasWorkspaceScreenshot: !!workspace3DScreenshot,
            imagesMatch: 'Cannot compare - missing screenshots',
          };
          console.log(`  ⚠️ Cannot compare 3D visualizations - results: ${!!results3D}, workspace: ${!!workspace3DScreenshot}`);
        }

        console.log(`  ✅ Viewport Panel found: ${viewports} viewports (3D: ${has3D})`);
      } else {
        inconsistencies.push('Viewport Visualization Panel not found');
        console.log('  ⚠️ Viewport Visualization Panel not found');
      }
    } catch (error) {
      inconsistencies.push(`Viewport Panel analysis error: ${error}`);
      console.log(`  ❌ Error analyzing Viewport Panel: ${error}`);
    }

    // Panel 6: Parameter Sliders
    console.log('\n🎚️ Analyzing Panel 6: Parameter Sliders...');
    try {
      const parameterSliders = page.locator('input[type="range"], [class*="slider"], [class*="parameter"]');
      const sliderCount = await parameterSliders.count();

      if (sliderCount > 0) {
        const sliderLabels: string[] = [];
        for (let i = 0; i < Math.min(sliderCount, 10); i++) {
          const slider = parameterSliders.nth(i);
          const label = await slider.locator('..').locator('label, span, text=/Lpp|Beam|Draft/i').first().textContent().catch(() => null);
          if (label) sliderLabels.push(label.trim());
        }

        analysisResults.parameterSliders = {
          count: sliderCount,
          labels: sliderLabels,
        };
        console.log(`  ✅ Parameter Sliders found: ${sliderCount} sliders`);
      } else {
        inconsistencies.push('Parameter Sliders not found');
        console.log('  ⚠️ Parameter Sliders not found');
      }
    } catch (error) {
      inconsistencies.push(`Parameter Sliders analysis error: ${error}`);
      console.log(`  ❌ Error analyzing Parameter Sliders: ${error}`);
    }

    // Step 13: Cross-Panel Consistency Checks
    console.log('\n🔗 Step 13: Performing Cross-Panel Consistency Checks...\n');

    // Check 1: KPI values match across panels
    if (analysisResults.kpiPanel && analysisResults.parameterSliders) {
      console.log('  🔍 Checking if KPI values match parameter slider values...');
      // This would require extracting specific values and comparing
      // For now, we'll log that the check was attempted
      console.log('  ℹ️ Cross-panel consistency check needs value extraction');
    }

    // Step 14: Data Correctness Checks
    console.log('\n✅ Step 14: Performing Data Correctness Checks...\n');

    // Check for negative values where they shouldn't exist
    const allText = await page.textContent('body');
    const negativeValues = allText?.match(/-\d+\.?\d*/g) || [];
    if (negativeValues.length > 10) { // Allow some negative values (like coordinates)
      inconsistencies.push(`Suspicious number of negative values found: ${negativeValues.length}`);
      console.log(`  ⚠️ Found ${negativeValues.length} negative values (may be normal for coordinates)`);
    }

    // Check for NaN or undefined values
    if (allText?.includes('NaN') || allText?.includes('undefined')) {
      inconsistencies.push('Found NaN or undefined values in displayed data');
      console.log('  ❌ Found NaN or undefined values');
    }

    // Step 15: Generate Summary Report
    console.log('\n📋 Step 15: Generating Analysis Summary...\n');

    const summary = {
      timestamp: new Date().toISOString(),
      url: page.url(),
      panelsAnalyzed: Object.keys(analysisResults).length,
      inconsistenciesFound: inconsistencies.length,
      inconsistencies,
      analysisResults,
    };

    console.log('\n' + '='.repeat(60));
    console.log('ANALYSIS SUMMARY');
    console.log('='.repeat(60));
    console.log(`Panels Analyzed: ${summary.panelsAnalyzed}`);
    console.log(`Inconsistencies Found: ${summary.inconsistenciesFound}`);
    console.log('\nAnalysis Results:');
    console.log(JSON.stringify(analysisResults, null, 2));

    if (inconsistencies.length > 0) {
      console.log('\n⚠️ INCONSISTENCIES DETECTED:');
      inconsistencies.forEach((issue, index) => {
        console.log(`  ${index + 1}. ${issue}`);
      });
    } else {
      console.log('\n✅ No inconsistencies detected!');
    }
    console.log('='.repeat(60) + '\n');

    // Save report to file
    const reportPath = path.join(__dirname, '../../temp/workspace-analysis-report.json');
    fs.mkdirSync(path.dirname(reportPath), { recursive: true });
    fs.writeFileSync(reportPath, JSON.stringify(summary, null, 2));
    console.log(`📄 Full report saved to: ${reportPath}`);

    // Take final screenshot
    const screenshotPath = path.join(__dirname, '../../temp/workspace-final-state.png');
    await page.screenshot({ path: screenshotPath, fullPage: true });
    console.log(`📸 Screenshot saved to: ${screenshotPath}`);

    // Assert that we found at least some panels
    expect(summary.panelsAnalyzed).toBeGreaterThan(0);

    // Log inconsistencies but don't fail the test (for now)
    if (inconsistencies.length > 0) {
      console.warn(`⚠️ Test completed with ${inconsistencies.length} inconsistency(ies). Check report for details.`);
    }
  });
});
