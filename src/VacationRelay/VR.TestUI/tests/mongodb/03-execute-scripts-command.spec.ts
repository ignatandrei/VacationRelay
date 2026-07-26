// spec: specs/mongodb-test-plan.md § 1.3
// seed: src/VacationRelay/VR.TestUI/tests/seed.spec.ts

import { test, expect } from '@playwright/test';

const DASHBOARD_URL =
  process.env.ASPIRE_DASHBOARD_URL ?? 'https://vacationrelay.dev.localhost:17071';
const DASHBOARD_TOKEN =
  process.env.ASPIRE_DASHBOARD_TOKEN ?? 'e1dfab69a928830bb11c9a4af2a17219';

// Helper: discover the MongoExpress URL so we can verify collections after the command
async function getMongoExpressUrl(page: import('@playwright/test').Page): Promise<string> {
  if (process.env.MONGOEXPRESS_URL) return process.env.MONGOEXPRESS_URL;
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
  test('ExecuteDBScripts Aspire command re-creates collections without error', async ({ page }) => {
    // Step 1: Open Aspire dashboard
    await page.goto(`${DASHBOARD_URL}/login?t=${DASHBOARD_TOKEN}`);
    await page.waitForURL(`${DASHBOARD_URL}/**`, { timeout: 15_000 });
    await page.waitForLoadState('networkidle');

    // Locate the 'mongodb' resource row in the dashboard grid
    const mongodbRow = page
      .locator('tr, fluent-data-grid-row')
      .filter({ has: page.locator(':text-is("mongodb")') })
      .first();
    await expect(mongodbRow).toBeVisible({ timeout: 30_000 });

    // Step 2: Trigger the ExecuteDBScripts command
    // The custom command is exposed as a button (or via a context-menu / actions column).
    // Try direct button first; fall back to ellipsis/actions menu.
    const executeBtn = mongodbRow
      .getByRole('button', { name: /ExecuteDBScripts/i })
      .or(page.getByRole('button', { name: /ExecuteDBScripts/i }))
      .first();

    // If the button is inside a collapsed actions menu, expand it first
    const actionsMenuBtn = mongodbRow
      .getByRole('button', { name: /actions|more|⋮|\.\.\./i })
      .first();
    if (await actionsMenuBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await actionsMenuBtn.click();
    }

    await expect(executeBtn).toBeVisible({ timeout: 10_000 });
    await executeBtn.click();

    // Step 3: Wait for the result notification / toast to show success
    // Aspire dashboard typically shows a toast or inline message after a command completes.
    const successNotification = page
      .getByText(/All scripts executed successfully/i)
      .or(page.getByText(/Success.*true/i))
      .or(page.locator('[role="alert"], .toast, .notification').filter({ hasText: /success/i }));
    await expect(successNotification).toBeVisible({ timeout: 30_000 });

    // Step 4: Navigate back to MongoExpress and verify collections still exist (idempotency)
    const meUrl = await getMongoExpressUrl(page);
    await page.goto(meUrl);
    await page.waitForLoadState('domcontentloaded');
    await page.getByText('vacationrelay').first().click();
    await page.waitForLoadState('networkidle');

    // Both collections must still be present after re-run
    await expect(page.getByText('vr_data').first()).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText('vr_data_history').first()).toBeVisible({ timeout: 10_000 });
  });
});
