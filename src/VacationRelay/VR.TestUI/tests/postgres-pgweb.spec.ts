// spec: tests/db-admin-verification.plan.md
// seed: tests/seed-pgweb.spec.ts

import { test, expect } from '@playwright/test';
import { getPgWebUrl, DB_CREDENTIALS } from './support/aspire-helpers';
import { verifyDbAdminUiAccessible } from './support/db-admin-verification';

test.describe('PostgreSQL – PgWeb', () => {
  test('PostgreSQL vacationrelay database is listed and tables exist in PgWeb', async ({ page }) => {
    await verifyDbAdminUiAccessible(page, {
      dbLabel: 'PostgreSQL',
      resourceName: 'pgweb',
      getUrl: getPgWebUrl,
      tableNames: ['vr_data', 'vr_data_history'],
      login: async (page) => {
        // PgWeb may auto-connect (URL contains creds) or show a connection form
        const connectButton = page.getByRole('button', { name: /connect/i });
        const isFormVisible = await connectButton.isVisible({ timeout: 3000 }).catch(() => false);

        if (isFormVisible) {
            await page.fill('input[id="pg_host"]', DB_CREDENTIALS.pgHost);
            await page.fill('input[id="pg_user"]', DB_CREDENTIALS.username);
            await page.fill('input[id="pg_password"]', DB_CREDENTIALS.password);
            await page.fill('input[id="pg_db"]', DB_CREDENTIALS.database);
          await connectButton.click();
        }
      },
    });
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
    await page.fill('input[id="pg_host"]', DB_CREDENTIALS.pgHost);
    await page.fill('input[id="pg_user"]', DB_CREDENTIALS.username);
    await page.fill('input[id="pg_password"]', DB_CREDENTIALS.password);
    await page.fill('input[id="pg_db"]', 'doesnotexist');
    await connectButton.click();

    // 3. Assert an error is shown – no table list appears
    await expect(page.locator('.error, .alert-danger, .connection-error')).toBeVisible();
    await expect(page.getByText('vr_data')).not.toBeVisible();
  });
});
