// spec: tests/db-admin-verification.plan.md
// seed: tests/seed-mongo-express.spec.ts

import { test, expect } from '@playwright/test';
import { getMongoExpressUrl } from './support/aspire-helpers';

test.describe('MongoDB – Mongo Express', () => {
  test('MongoDB vacationrelay database is visible in Mongo Express and vr_data collection exists', async ({ page }) => {
    // 1. Navigate to the Mongo Express URL
    await page.goto(getMongoExpressUrl());

    // 2. Wait until the database list table is fully rendered
    await expect(page.getByRole('table').first()).toBeVisible();

    // 3. Assert a row for 'vacationrelay' database is present
    await expect(page.getByText('vacationrelay')).toBeVisible();

    // 4. Click the 'View' link in the vacationrelay row
    const vacationRelayRow = page.locator('tr', { hasText: 'vacationrelay' });
    await vacationRelayRow.getByRole('link', { name: /view/i }).click();

    // 5. Assert the collections page loads and vr_data is listed
    await expect(page.getByText('vr_data')).toBeVisible();

    // 6. Click 'View' for the vr_data collection – no error should appear
    const vrDataRow = page.locator('tr', { hasText: 'vr_data' }).first();
    await vrDataRow.getByRole('link', { name: /view/i }).click();
    await expect(page.locator('.alert-danger, .error')).not.toBeVisible();
  });

  test('Mongo Express does not show non-existent database', async ({ page }) => {
    // 1. Navigate to Mongo Express home page
    await page.goto(getMongoExpressUrl());

    // 2. Wait for database list to render
    await expect(page.getByRole('table').first()).toBeVisible();

    // 3. Verify 'nonexistentdb' is not listed
    await expect(page.getByText('nonexistentdb')).not.toBeVisible();
  });
});
