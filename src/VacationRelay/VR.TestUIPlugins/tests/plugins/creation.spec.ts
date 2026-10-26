import { randomUUID } from 'node:crypto';
import { test, expect } from '@playwright/test';
import { sleep, videoFileName } from '../support/playwright-helpers';

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
  await sleep(4); // Wait  seconds to ensure the first chapter is recorded
  indix.dispose();
});

test.afterEach(async ({ page }, testInfo) => {
  // Stop recording the screencast after the test execution
  await page.screencast.stop();
});
test.describe('Record creation and validation', () => {
  test.use({ ignoreHTTPSErrors: true, locale: 'en-US', timezoneId: 'UTC' });

  test('GUI-005 - Create with an explicit end date', async ({ page }, testInfo) => {
    const identifier = `gui005-${testInfo.project.name}-${testInfo.workerIndex}-${testInfo.retry}-${randomUUID()}`;
    const vacationPerson = `${identifier}-vacation`;
    const replacementPerson = `${identifier}-replacement`;
    let createdRecordUrl: string | undefined;

    try {
      await page.goto('https://gui-efcore-startplugins-host.dev.localhost:7249/');
      const addRelay = page.getByRole('region', { name: 'Add a vacation relay', exact: true });
      await expect(addRelay).toBeVisible({ timeout: 30_000 });
      await addRelay.getByLabel('Person in vacation', { exact: true }).fill(vacationPerson);
      await addRelay.getByLabel('Replacement person', { exact: true }).fill(replacementPerson);
      await addRelay.getByLabel('From date', { exact: true }).fill('2026-10-26');
      await addRelay.getByLabel('To date (optional)', { exact: true }).fill('2026-10-30');

      const [response] = await Promise.all([
        page.waitForResponse(response =>
          response.request().method() === 'POST' &&
          new URL(response.url()).pathname === '/api/vrdata/',
        ),
        addRelay.getByRole('button', { name: 'Create record', exact: true }).click(),
      ]);
      const location = response.headers()['location'];
      if (location) {
        createdRecordUrl = new URL(location, response.url()).href;
      }
      expect(response.status()).toBe(201);
      expect(createdRecordUrl, 'Created response must identify the record for cleanup').toBeDefined();
      await expect(page.getByRole('status')).toHaveText('Vacation relay record created.');

      const records = page.getByRole('region', { name: 'Records', exact: true });
      const row = records.getByRole('row').filter({
        has: page.getByRole('cell', { name: vacationPerson, exact: true }),
      });
      await expect(row).toHaveCount(1);
      await expect(row.getByRole('cell', { name: replacementPerson, exact: true })).toBeVisible();
      await expect(row.getByRole('cell', { name: '10/26/2026', exact: true })).toBeVisible();
      await expect(row.getByRole('cell', { name: '10/30/2026', exact: true })).toBeVisible();
      await expect(row.getByText('Ongoing', { exact: true })).toHaveCount(0);

      await row.getByRole('button', { name: 'Details', exact: true }).click();
      const details = page.getByRole('region', { name: 'Record details', exact: true });
      await expect(details).toBeVisible();
      await expect(details.getByText(vacationPerson, { exact: true })).toBeVisible();
      await expect(details.getByText(replacementPerson, { exact: true })).toBeVisible();
      await expect(details.locator('dt').filter({ hasText: /^From$/ }).locator('+ dd'))
        .toHaveText(/^10\/26\/2026\s/);
      await expect(details.locator('dt').filter({ hasText: /^To$/ }).locator('+ dd'))
        .toHaveText(/^10\/30\/2026\s/);
      await expect(details.getByText('Ongoing', { exact: true })).toHaveCount(0);
      await expect(page.getByRole('alert')).toHaveCount(0);
    } finally {
      if (createdRecordUrl) {
        const deletion = await page.request.delete(createdRecordUrl);
        expect(deletion.status(), 'Delete only the record created by GUI-005').toBe(204);
        const lookup = await page.request.get(createdRecordUrl);
        expect(lookup.status(), 'Created record must not remain after cleanup').toBe(404);
      }
    }
  });
});
