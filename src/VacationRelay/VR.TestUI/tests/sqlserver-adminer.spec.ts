// spec: tests/db-admin-verification.plan.md
// seed: tests/seed-adminer.spec.ts

import { test, expect } from '@playwright/test';
import { getAdminerUrl, DB_CREDENTIALS } from './support/aspire-helpers';
import { verifyDbAdminUiAccessible } from './support/db-admin-verification';
import { sleep, videoFileName } from './support/playwright-helpers';

test.describe('SQL Server – Adminer', () => {
  test('SQL Server vacationrelay database is visible and tables exist in Adminer', async ({ page }) => {
    await verifyDbAdminUiAccessible(page, {
      dbLabel: 'SQL Server',
      resourceName: 'sqlserver-adminer',
      getUrl: getAdminerUrl,
      tableNames: ['vr_data', 'vr_data_history'],
      login: async (page) => {
        await page.selectOption('select[name="auth[driver]"]', 'mssql');
        await page.selectOption('select[name="auth[custom_server]"]', 'sqlserver');
        await page.click('input[type="submit"]');

        // Click into the vacationrelay database to see its tables
        await page.getByRole('link', { name: 'vacationrelay' }).click();
      },
      openTableAndVerify: async (page, tableName) => {
        var table = page.locator("#tables").getByRole('link', { name: tableName }).first();
        await table.scrollIntoViewIfNeeded();
        await expect(table).toBeVisible();
      },
      introSourceFiles: [
        {
          name: 'sqlserver',
          title: 'Now go to ASPIRE',
          description: 'It will show the SQL Server database and the vr_data table',
        },
      ],
      outroSourceFiles: [
        { name: 'SqlServer01.createTables.gen.txt', title: '', description: '' },
        { name: 'sqlserver-adminer.spec.ts', title: '', description: '' },
      ],
    });
  });

  test('Adminer login fails gracefully with wrong credentials', async ({ page }) => {
    // 1. Navigate to the Adminer URL
    await page.goto(getAdminerUrl());

    // 2. Set System to MS SQL
    await page.selectOption('select[name="auth[driver]"]', 'mssql');

    // 3. Fill credentials with wrong password
    await page.fill('input[name="auth[server]"]', DB_CREDENTIALS.server);
    await page.fill('input[name="auth[username]"]', DB_CREDENTIALS.username);
    await page.fill('input[name="auth[password]"]', 'wrongpassword');

    // 4. Click Login
    await page.click('input[type="submit"]');

    // 5. Assert error message is shown – login page stays visible
    await expect(page.locator('.error, #error')).toBeVisible();

    // 6. Assert the vacationrelay database link is NOT visible
    await expect(page.getByRole('link', { name: 'vacationrelay' })).not.toBeVisible();
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
