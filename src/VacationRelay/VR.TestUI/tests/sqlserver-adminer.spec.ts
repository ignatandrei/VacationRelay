// spec: tests/db-admin-verification.plan.md
// seed: tests/seed-adminer.spec.ts

import { test, expect } from '@playwright/test';
import { getAdminerUrl, DB_CREDENTIALS } from './support/aspire-helpers';

test.describe('SQL Server – Adminer', () => {
  test('SQL Server vacationrelay database is visible and tables exist in Adminer', async ({ page }) => {
    // 1. Navigate to the Adminer URL
    await page.goto(getAdminerUrl());

    // 2. Set System dropdown to MS SQL
    await page.selectOption('select[name="auth[driver]"]', 'mssql');

    // 3. Fill Server field with the SQL Server host
    await page.fill('input[name="auth[server]"]', DB_CREDENTIALS.server);

    // 4. Fill Username with 'sa'
    await page.fill('input[name="auth[username]"]', DB_CREDENTIALS.username);

    // 5. Fill Password
    await page.fill('input[name="auth[password]"]', DB_CREDENTIALS.password);

    // 6. Leave Database empty to list all databases
    await page.fill('input[name="auth[db]"]', '');

    // 7. Click Login button
    await page.click('input[type="submit"]');

    // 8. Assert vacationrelay database link is visible in the database list
    await expect(page.getByRole('link', { name: 'vacationrelay' })).toBeVisible();

    // 9. Click the vacationrelay database link to navigate into it
    await page.getByRole('link', { name: 'vacationrelay' }).click();

    // 10. Assert vr_data table is listed
    await expect(page.getByRole('link', { name: 'vr_data' })).toBeVisible();

    // 11. Assert vr_data_history table is listed
    await expect(page.getByRole('link', { name: 'vr_data_history' })).toBeVisible();
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
