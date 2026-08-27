// spec: tests/db-admin-verification.plan.md
// seed: tests/seed-mongo-express.spec.ts

import { test, expect } from '@playwright/test';
import { getMongoExpressUrl } from './support/aspire-helpers';
import { verifyDbAdminUiAccessible } from './support/db-admin-verification';
import { sleep, videoFileName } from './support/playwright-helpers';

test.describe('MongoDB – Mongo Express', () => {
  test('MongoDB vacationrelay database is visible in Mongo Express and vr_data collection exists', async ({ page },testInfo) => {
    testInfo.setTimeout(0); // 2 minutes
    await verifyDbAdminUiAccessible(page, {
      dbLabel: 'Mongo Express',
      resourceName: 'mongo-mongoexpress',
      getUrl: getMongoExpressUrl,
      tableNames: ['vacationrelay'],
      openTableAndVerify: async (page) => {
        // Click the 'View' link in the vacationrelay row, then in the vr_data row
        const vacationRelayRow = page.locator('tr', { hasText: 'vacationrelay' });
        await vacationRelayRow.getByRole('link', { name: /view/i }).click();
        await expect(page.getByText('vr_data',{exact: true})).toBeVisible();

        const vrDataRow = page.locator('tr', { hasText: 'vr_data' }).first();
        await vrDataRow.getByRole('link', { name: /view/i }).click();
        await expect(page.locator('.alert-danger, .error')).not.toBeVisible();
      },
    });
  });

  test('Mongo Express does not show non-existent database', async ({ page }) => {
    // 1. Navigate to Mongo Express home page
    await page.goto(getMongoExpressUrl());

    // 2. Wait for database list to render
    await expect(page.getByRole('table').first()).toBeVisible();

    // 3. Verify 'nonexistentdb' is not listed
    await expect(page.getByText('nonexistentdb')).not.toBeVisible();
  });


  test.beforeEach(async ({ page }, testInfo) => {
    // Start recording a screencast of the test execution
    testInfo.setTimeout(0); 
    await page.screencast.start({ path: `${videoFileName(testInfo)}` });
    await page.screencast.showActions({ position: 'top' });
    var indix = await page.screencast.showOverlay('<div style="color: red">Andrei Ignat</div>');
    await page.screencast.showChapter(`Starting ${testInfo.title}`, {
      description: `Beginning of the ${testInfo.title}`,
      duration: 5000,
    });
    await sleep(4); // Wait for 2 seconds to ensure the first chapter is recorded
    indix.dispose();
  });
  
  test.afterEach(async ({ page }, testInfo) => {
    // Stop recording the screencast after the test execution
    await page.screencast.stop();
  });

});
