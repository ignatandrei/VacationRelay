import { randomUUID } from 'node:crypto';
import { test, expect } from '@playwright/test';
import { sleep, videoFileName } from '../support/playwright-helpers';

test.describe('Searching and clearing', () => {
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

  test('GUI-012 - Search by vacation person', async ({ page }, testInfo) => {
    const identifier = `gui012-${testInfo.project.name}-${testInfo.workerIndex}-${testInfo.retry}-${randomUUID()}`;
    const vacationPeople = [`${identifier}-first`, `${identifier}-second`];
    const replacementPerson = `${identifier}-replacement`;
    const createdRecordUrls: string[] = [];

    try {
      await page.goto('https://gui-efcore-startplugins-host.dev.localhost:7249/');
      const addRelay = page.getByRole('region', { name: 'Add a vacation relay', exact: true });
      await expect(addRelay).toBeVisible({ timeout: 30_000 });
      const records = page.getByRole('region', { name: 'Records', exact: true });

      for (const vacationPerson of vacationPeople) {
        await addRelay.getByLabel('Person in vacation', { exact: true }).fill(vacationPerson);
        await addRelay.getByLabel('Replacement person', { exact: true }).fill(replacementPerson);
        await addRelay.getByLabel('From date', { exact: true }).fill('2026-10-26');

        const [response] = await Promise.all([
          page.waitForResponse(response =>
            response.request().method() === 'POST' &&
            new URL(response.url()).pathname === '/api/vrdata/',
          ),
          addRelay.getByRole('button', { name: 'Create record', exact: true }).click(),
        ]);
        const location = response.headers()['location'];
        if (location) {
          createdRecordUrls.push(new URL(location, response.url()).href);
        }
        expect(response.status()).toBe(201);
        expect(location, 'Created response must identify the record for cleanup').toBeTruthy();
        await expect(records.getByRole('cell', { name: vacationPerson, exact: true })).toBeVisible();
        await expect(addRelay.getByRole('button', { name: 'Create record', exact: true })).toBeEnabled();
      }

      const findRecords = page.getByRole('region', { name: 'Find records', exact: true });
      await findRecords.getByLabel('Person in vacation', { exact: true }).fill(vacationPeople[0]);
      await expect(findRecords.getByLabel('Replacement person', { exact: true })).toBeEmpty();

      const [searchResponse] = await Promise.all([
        page.waitForResponse(response => {
          const url = new URL(response.url());
          return response.request().method() === 'GET' &&
            url.pathname === '/api/vrdata/' &&
            url.searchParams.get('idPersonInVacation') === vacationPeople[0];
        }),
        findRecords.getByRole('button', { name: 'Search', exact: true }).click(),
      ]);
      expect(searchResponse.status()).toBe(200);

      const matchingRow = records.getByRole('row').filter({
        has: page.getByRole('cell', { name: vacationPeople[0], exact: true }),
      });
      await expect(matchingRow).toHaveCount(1);
      await expect(matchingRow.getByRole('cell', { name: replacementPerson, exact: true })).toBeVisible();
      await expect(records.getByRole('cell', { name: vacationPeople[1], exact: true })).toHaveCount(0);
      await expect(page.getByRole('alert')).toHaveCount(0);
    } finally {
      await Promise.all(createdRecordUrls.map(async url => {
        const deletion = await page.request.delete(url);
        expect.soft(deletion.status(), `Delete only the GUI-012 fixture: ${url}`).toBe(204);
        const lookup = await page.request.get(url);
        expect.soft(lookup.status(), `GUI-012 fixture must not remain: ${url}`).toBe(404);
      }));
    }
  });
});
