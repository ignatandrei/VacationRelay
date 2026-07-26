// spec: specs/mongodb-test-plan.md § 1.1
// seed: src/VacationRelay/VR.TestUI/tests/seed.spec.ts

import { test, expect } from '@playwright/test';

const DASHBOARD_URL =
  process.env.ASPIRE_DASHBOARD_URL ?? 'https://vacationrelay.dev.localhost:17071';
const DASHBOARD_TOKEN =
  process.env.ASPIRE_DASHBOARD_TOKEN ?? 'e1dfab69a928830bb11c9a4af2a17219';

test.describe('Aspire Resource Startup – MongoDB', () => {
  test.beforeEach(async ({ page }) => {
    // Log in to the Aspire dashboard using the token-based auth URL
    await page.goto(`${DASHBOARD_URL}/login?t=${DASHBOARD_TOKEN}`);
    // Wait for redirect to the main resources page
    await page.waitForURL(`${DASHBOARD_URL}/**`, { timeout: 15_000 });
    await page.waitForLoadState('networkidle');
  });

  test('MongoDB container reaches Ready state', async ({ page }) => {
    // Step 1: The Aspire dashboard prints the resources table; ensure it loaded
    await expect(page).not.toHaveURL(/login/);

    // Step 2: Locate the 'mongo' container resource row
    // The dashboard shows a resource grid; each resource name appears as a text node
    const mongoEntry = page.getByRole('gridcell', { name: /^mongo$/ }).or(
      page.locator('td, fluent-data-grid-cell').filter({ hasText: /^mongo$/ }),
    );
    await expect(mongoEntry).toBeVisible({ timeout: 15_000 });

    // Verify 'mongo' row shows Running state within 60 s
    const mongoRunningState = page
      .locator('tr, fluent-data-grid-row')
      .filter({ has: page.locator(':text-is("mongo")') })
      .getByText('Running', { exact: false });
    await expect(mongoRunningState).toBeVisible({ timeout: 60_000 });

    // Step 3: Locate the 'mongodb' database resource and verify Running state
    const mongodbRunningState = page
      .locator('tr, fluent-data-grid-row')
      .filter({ has: page.locator(':text-is("mongodb")') })
      .getByText('Running', { exact: false });
    await expect(mongodbRunningState).toBeVisible({ timeout: 30_000 });
  });
});
