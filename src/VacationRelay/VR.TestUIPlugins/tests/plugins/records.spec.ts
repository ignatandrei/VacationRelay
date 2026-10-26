import { randomUUID } from 'node:crypto';
import { test, expect } from '@playwright/test';
import { sleep, videoFileName } from '../support/playwright-helpers';

test.describe('Details and deletion', () => {
  test.use({ ignoreHTTPSErrors: true });
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

  test('GUI-019 - Delete only the selected record', async ({ page }, testInfo) => {
    const identifier = `gui019-${testInfo.project.name}-${testInfo.workerIndex}-${testInfo.retry}-${randomUUID()}`;
    const vacationPeople = [`${identifier}-first`, `${identifier}-second`];
    const createdRecordUrls: string[] = [];

    try {
      const [initialResponse] = await Promise.all([
        page.waitForResponse(response =>
          response.request().method() === 'GET' &&
          new URL(response.url()).pathname === '/api/vrdata/',
        ),
        page.goto('https://gui-efcore-startplugins-host.dev.localhost:7249/'),
      ]);
      expect(initialResponse.status()).toBe(200);
      const collectionUrl = initialResponse.url();

      for (const vacationPerson of vacationPeople) {
        const response = await page.request.post(collectionUrl, {
          data: {
            idpersonInVacation: vacationPerson,
            idpersonReplace: `${identifier}-replacement`,
            fromDate: '2026-10-26T00:00:00Z',
            toDate: null,
          },
        });
        const location = response.headers()['location'];
        if (location) {
          createdRecordUrls.push(new URL(location, collectionUrl).href);
        }
        expect(response.status()).toBe(201);
        expect(location, 'Created response must identify the fixture for cleanup').toBeTruthy();
      }

      await page.reload();
      const records = page.getByRole('region', { name: 'Records', exact: true });
      const firstRow = records.getByRole('row').filter({
        has: page.getByRole('cell', { name: vacationPeople[0], exact: true }),
      });
      const secondRow = records.getByRole('row').filter({
        has: page.getByRole('cell', { name: vacationPeople[1], exact: true }),
      });
      await expect(firstRow).toBeVisible({ timeout: 30_000 });
      await expect(secondRow).toBeVisible();

      await firstRow.getByRole('button', { name: 'Details', exact: true }).click();
      const details = page.getByRole('region', { name: 'Record details', exact: true });
      await expect(details.getByText(vacationPeople[0], { exact: true })).toBeVisible();
      const recordPath = new URL(createdRecordUrls[0]).pathname;
      await expect(details.getByText(recordPath.slice(recordPath.lastIndexOf('/') + 1), {
        exact: true,
      })).toBeVisible();

      const [deletion] = await Promise.all([
        page.waitForResponse(response =>
          response.request().method() === 'DELETE' &&
          response.url() === createdRecordUrls[0],
        ),
        firstRow.getByRole('button', { name: 'Delete', exact: true }).click(),
      ]);
      expect(deletion.status()).toBe(204);
      await expect(page.getByRole('status')).toHaveText('Vacation relay record deleted.');
      await expect(firstRow).toHaveCount(0);
      await expect(secondRow).toBeVisible();
      await expect(details).toHaveCount(0);
      await expect(page.getByRole('alert')).toHaveCount(0);

      await page.reload();
      await expect(page.getByRole('heading', {
        name: 'Vacation relay data', level: 1, exact: true,
      })).toBeVisible({ timeout: 30_000 });
      await expect(records.getByText('Loading records...', { exact: true })).toBeHidden();
      await expect(firstRow).toHaveCount(0);
      await expect(secondRow).toBeVisible();
      await expect(details).toHaveCount(0);
      expect((await page.request.get(createdRecordUrls[0])).status()).toBe(404);
      expect((await page.request.get(createdRecordUrls[1])).status()).toBe(200);
    } finally {
      await Promise.all(createdRecordUrls.map(async url => {
        const deletion = await page.request.delete(url);
        expect.soft([204, 404], `Clean up only the GUI-019 fixture: ${url}`)
          .toContain(deletion.status());
        const lookup = await page.request.get(url);
        expect.soft(lookup.status(), `GUI-019 fixture must not remain: ${url}`).toBe(404);
      }));
    }
  });
});
