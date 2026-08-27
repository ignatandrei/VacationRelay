// spec: tests/db-admin-verification.plan.md
// seed: tests/seed-adminer.spec.ts

import { test, expect } from '@playwright/test';
import { getAdminerUrl, DB_CREDENTIALS } from './support/aspire-helpers';
import { verifyDbAdminUiAccessible } from './support/db-admin-verification';

test.describe('SQL Server – Adminer', () => {
  test('SQL Server vacationrelay database is visible and tables exist in Adminer', async ({ page }) => {
    await verifyDbAdminUiAccessible(page, {
      dbLabel: 'SQL Server',
      resourceName: 'sqlserver-adminer',
      getUrl: getAdminerUrl,
      tableNames: ['vr_data', 'vr_data_history'],
      login: async (page) => {
        await page.selectOption('select[name="auth[driver]"]', 'mssql');
        await page.fill('input[name="auth[server]"]', DB_CREDENTIALS.server);
        await page.fill('input[name="auth[username]"]', DB_CREDENTIALS.username);
        await page.fill('input[name="auth[password]"]', DB_CREDENTIALS.password);
        await page.fill('input[name="auth[db]"]', '');
        await page.click('input[type="submit"]');

        // Click into the vacationrelay database to see its tables
        await page.getByRole('link', { name: 'vacationrelay' }).click();
      },
      openTableAndVerify: async (page, tableName) => {
        await expect(page.getByRole('link', { name: tableName })).toBeVisible();
      },
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
});
