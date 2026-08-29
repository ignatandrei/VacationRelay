import { test, expect } from '@playwright/test';
import { getSqliteWebUrl } from './support/aspire-helpers';
import { sleep, videoFileName } from './support/playwright-helpers';
import { verifyDbAdminUiAccessible } from './support/db-admin-verification';

test.describe('SQLite – SqliteWeb ', () => {
  test('SQLite database is accessible and vr_data table exists in SqliteWeb', async ({ page }) => {
    await verifyDbAdminUiAccessible(page, {
      dbLabel: 'SqliteWeb',
      resourceName: 'sqlite-sqliteweb',
      getUrl: getSqliteWebUrl,
      tableNames: ['vr_data', 'vr_data_history'],
      introSourceFiles: [
        {
          name: 'sqlite',
          title: 'Now go to ASPIRE',
          description: 'It will show the SqliteWeb database and the vr_data table',
        },
      ],
      outroSourceFiles: [
        { name: 'SQLite01.createTables.gen.txt', title: '', description: '' },
        { name: 'sqlite-sqliteweb.spec.ts', title: '', description: '' },
      ],
    });
  });

  
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
