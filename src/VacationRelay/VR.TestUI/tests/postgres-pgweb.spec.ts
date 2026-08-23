// spec: tests/db-admin-verification.plan.md
// seed: tests/seed-pgweb.spec.ts

import { test, expect } from '@playwright/test';
import { getPgWebUrl, DB_CREDENTIALS } from './support/aspire-helpers';

test.describe('PostgreSQL – PgWeb', () => {
  test('PostgreSQL vacationrelay database is listed and tables exist in PgWeb', async ({ page }) => {
    // 1. Navigate to the PgWeb URL
    await page.goto(getPgWebUrl());

    // 2. PgWeb may auto-connect (URL contains creds) or show a connection form
    const connectButton = page.getByRole('button', { name: /connect/i });
    const isFormVisible = await connectButton.isVisible({ timeout: 3000 }).catch(() => false);

    if (isFormVisible) {
      // Fill the connection form if shown
      await page.fill('input[name="host"]', DB_CREDENTIALS.pgHost);
      await page.fill('input[name="user"]', DB_CREDENTIALS.username);
      await page.fill('input[name="password"]', DB_CREDENTIALS.password);
      await page.fill('input[name="db"]', DB_CREDENTIALS.database);
      await connectButton.click();
    }

    // 3. Wait for the sidebar/table list to appear (indicates successful connection)
    const sidebar = page.locator('#sidebar, .sidebar, .sidebar-tables, nav').first();
    await expect(sidebar).toBeVisible();

    // 4. Assert vr_data table is listed in the sidebar
    await expect(page.getByText('vr_data').first()).toBeVisible();

    // 5. Assert vr_data_history table is listed
    await expect(page.getByText('vr_data_history')).toBeVisible();

    // 6. Click on vr_data table to open it
    await page.getByText('vr_data').first().click();

    // 7. Assert the content panel loads without error
    await expect(page.locator('#content, .results, .table-content').first()).toBeVisible();
    await expect(page.locator('.error, .alert-danger')).not.toBeVisible();
  });

  test('PgWeb shows error when connecting to a non-existent database', async ({ page }) => {
    // 1. Navigate to PgWeb URL
    await page.goto(getPgWebUrl());

    const connectButton = page.getByRole('button', { name: /connect/i });
    const isFormVisible = await connectButton.isVisible({ timeout: 3000 }).catch(() => false);

    if (!isFormVisible) {
      // PgWeb auto-connected – skip this test as there's no UI to enter a bad DB name
      test.skip();
      return;
    }

    // 2. Attempt to connect to a non-existent database
    await page.fill('input[name="host"]', DB_CREDENTIALS.pgHost);
    await page.fill('input[name="user"]', DB_CREDENTIALS.username);
    await page.fill('input[name="password"]', DB_CREDENTIALS.password);
    await page.fill('input[name="db"]', 'doesnotexist');
    await connectButton.click();

    // 3. Assert an error is shown – no table list appears
    await expect(page.locator('.error, .alert-danger, .connection-error')).toBeVisible();
    await expect(page.getByText('vr_data')).not.toBeVisible();
  });
});
