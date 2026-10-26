// spec: src/VacationRelay/VR.TestUIPlugins/specs/plugins-ui-test-plan.md
// seed: src/VacationRelay/VR.TestUIPlugins/tests/seed.spec.ts
import { test, expect } from '@playwright/test';
import { sleep, videoFileName } from '../support/playwright-helpers';

test.describe('Entry and navigation', () => {
  // The local development host uses a development TLS certificate.
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

  test('GUI-001 - Render the application shell', async ({ page }) => {
    // 1. Navigate to the root and wait for the H1 rather than a fixed delay.
    await page.goto('https://gui-efcore-startplugins-host.dev.localhost:7249/');
    const heading = page.getByRole('heading', { name: 'Vacation relay data', level: 1, exact: true });
    await expect(heading).toBeVisible({ timeout: 30_000 });

    // 2. Inspect the title, three initial named regions, and primary controls.
    await expect(page).toHaveTitle('Vacation relay data');
    const findRecords = page.getByRole('region', { name: 'Find records', exact: true });
    const records = page.getByRole('region', { name: 'Records', exact: true });
    const addRelay = page.getByRole('region', { name: 'Add a vacation relay', exact: true });
    await expect(findRecords).toBeVisible();
    await expect(records).toBeVisible();
    await expect(addRelay).toBeVisible();

    // 3. Expect title/H1 Vacation relay data, usable fields/buttons, and no persistent loading or unhandled-error overlay.
    await expect(heading).toHaveText('Vacation relay data');
    for (const region of [findRecords, addRelay]) {
      for (const name of ['Person in vacation', 'Replacement person']) {
        const input = region.getByRole('textbox', { name, exact: true });
        await expect(input).toBeVisible();
        await expect(input).toBeEditable();
      }
    }
    for (const name of ['From date', 'To date (optional)']) {
      const input = addRelay.getByLabel(name, { exact: true });
      await expect(input).toBeVisible();
      await expect(input).toBeEditable();
    }
    for (const button of [
      findRecords.getByRole('button', { name: 'Search', exact: true }),
      findRecords.getByRole('button', { name: 'Clear', exact: true }),
      addRelay.getByRole('button', { name: 'Create record', exact: true }),
    ]) {
      await expect(button).toBeVisible();
      await expect(button).toBeEnabled();
    }
    await expect(records.getByText('Loading records...', { exact: true })).toBeHidden();
    await expect(page.locator('.loading-progress')).toBeHidden();
    await expect(page.locator('.loading-progress-text')).toBeHidden();
    await expect(page.locator('#blazor-error-ui')).toBeHidden();
  });

  test('GUI-003 - Empty records display', async ({ page }) => {
    await page.route(
      url => url.pathname === '/api/vrdata/',
      async route => {
        if (route.request().method() === 'GET') {
          await route.fulfill({ json: [] });
        } else {
          await route.continue();
        }
      },
    );
    const recordsResponse = page.waitForResponse(response =>
      new URL(response.url()).pathname === '/api/vrdata/' &&
      response.request().method() === 'GET',
    );

    await page.goto('https://gui-efcore-startplugins-host.dev.localhost:7249/');
    await expect(page.getByRole('heading', {
      name: 'Vacation relay data', level: 1, exact: true,
    })).toBeVisible({ timeout: 30_000 });
    expect((await recordsResponse).status()).toBe(200);

    const records = page.getByRole('region', { name: 'Records', exact: true });
    await expect(records.getByText('No vacation relay records found.', { exact: true })).toBeVisible();
    await expect(records.getByText('Loading records...', { exact: true })).toBeHidden();
    await expect(records.getByRole('table')).toHaveCount(0);
    await expect(records.getByRole('row')).toHaveCount(0);
    await expect(page.getByRole('alert')).toHaveCount(0);

    const addRelay = page.getByRole('region', { name: 'Add a vacation relay', exact: true });
    await expect(addRelay).toBeVisible();
    for (const name of ['Person in vacation', 'Replacement person', 'From date', 'To date (optional)']) {
      await expect(addRelay.getByLabel(name, { exact: true })).toBeEditable();
    }
    await expect(addRelay.getByRole('button', { name: 'Create record', exact: true })).toBeEnabled();
  });
});
