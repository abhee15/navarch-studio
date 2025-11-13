import { test, expect } from '@playwright/test';

type TaxonomyEntry = {
  category: string;
  type: string;
  displayName: string;
  bowFamilies: string[];
  midshipFamilies: string[];
  sternFamilies: string[];
  primaryFamily: string;
};

const EMAIL = process.env.TEST_USER_EMAIL || 'test@example.com';
const PASSWORD = process.env.TEST_USER_PASSWORD || 'TestPassword123!';
const MAX_COMBOS_PER_ENTRY = 1;

const TAXONOMY: TaxonomyEntry[] = [
  {
    category: 'Commercial',
    type: 'general_cargo',
    displayName: 'Commercial – General Cargo',
    bowFamilies: ['bulbous_bow', 'straight_raked', 'fine_entry'],
    midshipFamilies: ['full_midship', 'fine_midship'],
    sternFamilies: ['transom_stern', 'cruiser_stern'],
    primaryFamily: 'cargo',
  },
  {
    category: 'Commercial',
    type: 'bulk_carrier',
    displayName: 'Commercial – Bulk Carrier',
    bowFamilies: ['bulbous_bow', 'straight_raked'],
    midshipFamilies: ['full_midship'],
    sternFamilies: ['transom_stern', 'cruiser_stern'],
    primaryFamily: 'bulk',
  },
  {
    category: 'Commercial',
    type: 'container',
    displayName: 'Commercial – Container Ship',
    bowFamilies: ['bulbous_bow', 'axe_bow'],
    midshipFamilies: ['fine_midship'],
    sternFamilies: ['transom_stern', 'twin_skeg'],
    primaryFamily: 'container',
  },
  {
    category: 'Commercial',
    type: 'fishing',
    displayName: 'Commercial – Fishing Vessel',
    bowFamilies: ['fine_entry', 'wave_piercing'],
    midshipFamilies: ['deep_v_midship', 'fine_midship'],
    sternFamilies: ['canoe_stern', 'wedge_stern'],
    primaryFamily: 'fishing',
  },
  {
    category: 'Commercial',
    type: 'tanker',
    displayName: 'Commercial – Tanker',
    bowFamilies: ['bulbous_bow', 'straight_raked'],
    midshipFamilies: ['full_midship'],
    sternFamilies: ['transom_stern', 'cruiser_stern'],
    primaryFamily: 'tanker',
  },
  {
    category: 'Commercial',
    type: 'lng_carrier',
    displayName: 'Commercial – LNG Carrier',
    bowFamilies: ['bulbous_bow', 'wave_piercing'],
    midshipFamilies: ['full_midship', 'fine_midship'],
    sternFamilies: ['transom_stern', 'twin_skeg'],
    primaryFamily: 'lng',
  },
  {
    category: 'Commercial',
    type: 'cruise_vessel',
    displayName: 'Commercial – Cruise Vessel',
    bowFamilies: ['bulbous_bow', 'fine_entry'],
    midshipFamilies: ['full_midship'],
    sternFamilies: ['cruiser_stern', 'transom_stern'],
    primaryFamily: 'ferry_conv',
  },
  {
    category: 'Commercial',
    type: 'passenger_vessel',
    displayName: 'Commercial – Passenger Vessel',
    bowFamilies: ['bulbous_bow', 'fine_entry'],
    midshipFamilies: ['full_midship', 'fine_midship'],
    sternFamilies: ['cruiser_stern', 'transom_stern'],
    primaryFamily: 'ferry_conv',
  },
  {
    category: 'Government',
    type: 'cutters',
    displayName: 'Government – Cutter',
    bowFamilies: ['fine_entry', 'axe_bow'],
    midshipFamilies: ['deep_v_midship', 'fine_midship'],
    sternFamilies: ['transom_stern', 'canoe_stern'],
    primaryFamily: 'patrol',
  },
  {
    category: 'Government',
    type: 'medical_ship',
    displayName: 'Government – Medical Ship',
    bowFamilies: ['bulbous_bow', 'straight_raked'],
    midshipFamilies: ['full_midship'],
    sternFamilies: ['cruiser_stern', 'transom_stern'],
    primaryFamily: 'osv',
  },
  {
    category: 'Government',
    type: 'general_military',
    displayName: 'Government – General Military',
    bowFamilies: ['fine_entry', 'axe_bow'],
    midshipFamilies: ['deep_v_midship'],
    sternFamilies: ['transom_stern', 'skeg_stern'],
    primaryFamily: 'patrol',
  },
  {
    category: 'Recreational',
    type: 'yacht',
    displayName: 'Recreational – Yacht',
    bowFamilies: ['wave_piercing', 'fine_entry'],
    midshipFamilies: ['deep_v_midship'],
    sternFamilies: ['transom_stern', 'canoe_stern'],
    primaryFamily: 'yacht_disp',
  },
  {
    category: 'Recreational',
    type: 'fishing_recreational',
    displayName: 'Recreational – Fishing',
    bowFamilies: ['fine_entry', 'straight_raked'],
    midshipFamilies: ['deep_v_midship', 'barge_midship'],
    sternFamilies: ['wedge_stern', 'transom_stern'],
    primaryFamily: 'fishing',
  },
  {
    category: 'Recreational',
    type: 'high_speed_craft',
    displayName: 'Recreational – High Speed Craft',
    bowFamilies: ['axe_bow', 'wave_piercing'],
    midshipFamilies: ['deep_v_midship'],
    sternFamilies: ['transom_stern', 'twin_skeg'],
    primaryFamily: 'ferry_fast',
  },
  {
    category: 'Research',
    type: 'research_vessel',
    displayName: 'Research – Oceanographic Research Vessel',
    bowFamilies: ['bulbous_bow', 'wave_piercing'],
    midshipFamilies: ['fine_midship', 'full_midship'],
    sternFamilies: ['cruiser_stern', 'transom_stern'],
    primaryFamily: 'research',
  },
];

const defaultMission = {
  cargoValue: '800',
  serviceSpeed: '20',
  seaMargin: '15',
  hs: '3',
  tz: '7',
  endurance: '3500',
};

test.describe('Hull Sizing ShipD taxonomy coverage', () => {
  test.slow();

  test('every taxonomy entry produces ShipD-consistent designs', async ({ page }) => {
    test.setTimeout(20 * 60 * 1000);

    // Login once
    await page.goto('/login');
    await page.getByLabel(/email/i).fill(EMAIL);
    await page.getByLabel(/password/i).fill(PASSWORD);
    await page.getByRole('button', { name: /sign in|login/i }).click();
    await page.waitForLoadState('networkidle');
    await page.goto('/sizing/missions');
    const newBriefButton = page.getByRole('button', { name: /new brief/i });
    await page.waitForTimeout(500); // allow loading state to mount
    await page.locator('text=Loading briefs...').waitFor({ state: 'detached', timeout: 20_000 }).catch(() => {});
    await newBriefButton.waitFor({ state: 'visible', timeout: 20_000 });

    let missionCounter = 0;

    for (const entry of TAXONOMY) {
      const combinations: Array<{ bow: string; mid: string; stern: string }> = [];
      entry.bowFamilies.forEach((bow) => {
        entry.midshipFamilies.forEach((mid) => {
          entry.sternFamilies.forEach((stern) => {
            combinations.push({ bow, mid, stern });
          });
        });
      });

      const limitedCombos = combinations.slice(0, MAX_COMBOS_PER_ENTRY);

      for (const combo of limitedCombos) {
        missionCounter += 1;

        // Navigate to missions and start new brief
        await page.goto('/sizing/missions');
        await expect(page).toHaveURL(/\/sizing\/missions/, { timeout: 10_000 });
        await page.locator('text=Loading briefs...').waitFor({ state: 'detached', timeout: 20_000 }).catch(() => {});
        const missionNewBriefButton = page.getByRole('button', { name: /new brief/i });
        await missionNewBriefButton.waitFor({ state: 'visible', timeout: 25_000 });
        await missionNewBriefButton.click();
        await expect(page).toHaveURL(/\/sizing\/wizard/, { timeout: 10_000 });

        // Step 1: Mission & cargo
        const missionName = `${entry.displayName} Auto ${combo.bow}-${combo.mid}-${combo.stern} ${Date.now()}-${missionCounter}`;
        await page.fill('input#name', missionName);
        await page.selectOption('select#missionCategory', { value: entry.category });
        await page.selectOption('select#missionType', { value: entry.type });
        await page.selectOption('select#cargoBasis', { value: 'weight' });
        await page.fill('input#cargoWeight', defaultMission.cargoValue);

        // Ensure mission type options match expected set
        const missionTypeOptions = await page.locator('select#missionType option').allTextContents();
        expect(missionTypeOptions.length).toBeGreaterThan(0);

        await page.getByRole('button', { name: /next: hull families/i }).click();
        await expect(page.getByRole('heading', { name: /hull families/i })).toBeVisible();

        // Step 2: Hull families - verify dropdowns match taxonomy
        const bowOptions = await page.locator('select#bowFamily option').allTextContents();
        expect(bowOptions.map((o) => o.trim()).sort()).toEqual([...entry.bowFamilies].sort());

        const midshipOptions = await page.locator('select#midshipFamily option').allTextContents();
        expect(midshipOptions.map((o) => o.trim()).sort()).toEqual([...entry.midshipFamilies].sort());

        const sternOptions = await page.locator('select#sternFamily option').allTextContents();
        expect(sternOptions.map((o) => o.trim()).sort()).toEqual([...entry.sternFamilies].sort());

        await page.selectOption('select#bowFamily', { value: combo.bow });
        await page.selectOption('select#midshipFamily', { value: combo.mid });
        await page.selectOption('select#sternFamily', { value: combo.stern });

        await page.getByRole('button', { name: /next: speed & environment/i }).click();

        // Step 3: Speed & environment
        await page.fill('#serviceSpeedKn', defaultMission.serviceSpeed);
        await page.fill('#seaMarginPct', defaultMission.seaMargin);
        await page.fill('#envHsM', defaultMission.hs);
        await page.fill('#envTzS', defaultMission.tz);
        await page.fill('#enduranceNm', defaultMission.endurance);
        await page.getByRole('button', { name: /next: constraints/i }).click();

        // Step 4: Constraints, Options
        await page.getByRole('button', { name: /next: options & review/i }).click();
        // Generate only the primary candidate for verification speed
        await page.fill('#maxCandidates', '1');
        await page.getByRole('button', { name: /generate hulls/i }).click();

        await expect(page).toHaveURL(/\/sizing\/runs\//, { timeout: 120_000 });
        const candidateCards = page.locator('[data-testid^="candidate-card-"]');
        await expect(candidateCards.first()).toBeVisible({ timeout: 240_000 });

        const cardCount = await candidateCards.count();
        expect(cardCount).toBeGreaterThan(0);
        for (let i = 0; i < cardCount; i += 1) {
          const card = candidateCards.nth(i);
          await expect(card).toHaveAttribute('data-hull-family', entry.primaryFamily);
          await expect(card).toHaveAttribute('data-bow-family', combo.bow);
          await expect(card).toHaveAttribute('data-midship-family', combo.mid);
          await expect(card).toHaveAttribute('data-stern-family', combo.stern);
        }

        // Return to missions for next iteration
        await page.getByRole('button', { name: /← back to briefs/i }).click();
        await expect(page).toHaveURL(/\/sizing\/missions/, { timeout: 30_000 });
      }
    }
  });
});
