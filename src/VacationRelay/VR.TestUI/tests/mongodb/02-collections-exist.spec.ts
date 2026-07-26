// spec: specs/mongodb-test-plan.md § 1.2
// seed: src/VacationRelay/VR.TestUI/tests/seed.spec.ts

import { test, expect } from '@playwright/test';

const DASHBOARD_URL =
  process.env.ASPIRE_DASHBOARD_URL ?? 'https://vacationrelay.dev.localhost:17071';
const DASHBOARD_TOKEN =
  process.env.ASPIRE_DASHBOARD_TOKEN ?? 'e1dfab69a928830bb11c9a4af2a17219';

// Helper: discover the MongoExpress endpoint URL from the Aspire dashboard resource table
async function getMongoExpressUrl(page: import('@playwright/test').Page): Promise<string> {
  if (process.env.MONGOEXPRESS_URL) return process.env.MONGOEXPRESS_URL;

  await page.goto(`${DASHBOARD_URL}/login?t=${DASHBOARD_TOKEN}`);
  await page.waitForURL(`${DASHBOARD_URL}/**`, { timeout: 15_000 });
  await page.waitForLoadState('networkidle');

  // The MongoExpress container is registered with name 'mongo-mongoexpress' or similar;
  // look for a hyperlink whose href contains 'mongoexpress' or whose visible text contains it.
  const mongoExpressLink = page
    .locator('a[href]')
    .filter({ hasText: /mongo.?express/i })
    .or(
      page
        .locator('tr, fluent-data-grid-row')
        .filter({ hasText: /mongo.?express/i })
        .locator('a[href]')
        .first(),
    )
    .first();

  const href = await mongoExpressLink.getAttribute('href');
  if (!href) throw new Error('Could not find MongoExpress endpoint URL in Aspire dashboard');
  return href;
}

test.describe('Aspire Resource Startup – MongoDB', () => {
  test('vr_data and vr_data_history collections are created at startup', async ({ page }) => {
    // Step 1: Navigate to the Aspire dashboard and verify mongodb resource Running
    await page.goto(`${DASHBOARD_URL}/login?t=${DASHBOARD_TOKEN}`);
    await page.waitForURL(`${DASHBOARD_URL}/**`, { timeout: 15_000 });
    await page.waitForLoadState('networkidle');

    // Verify mongodb shows Running before we check MongoExpress
    const mongodbRunning = page
      .locator('tr, fluent-data-grid-row')
      .filter({ has: page.locator(':text-is("mongodb")') })
      .getByText('Running', { exact: false });
    await expect(mongodbRunning).toBeVisible({ timeout: 60_000 });

    // Step 2: Locate and open MongoExpress endpoint link from the dashboard
    const meUrl = await getMongoExpressUrl(page);
    await expect(meUrl).toBeTruthy();

    // Step 3: Navigate to MongoExpress
    await page.goto(meUrl);
    await page.waitForLoadState('domcontentloaded');
    // MongoExpress landing page title contains "mongo-express"
    await expect(page).toHaveTitle(/mongo.?express/i, { timeout: 15_000 });

    // Step 4: Locate the 'vacationrelay' database in the sidebar / database table
    await expect(page.getByText('vacationrelay')).toBeVisible({ timeout: 10_000 });

    // Step 5: Click on 'vacationrelay' database to view its collections
    await page.getByText('vacationrelay').first().click();
    await page.waitForLoadState('networkidle');

    // Both collections must be listed
    await expect(page.getByText('vr_data').first()).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText('vr_data_history').first()).toBeVisible({ timeout: 10_000 });
  });
});
