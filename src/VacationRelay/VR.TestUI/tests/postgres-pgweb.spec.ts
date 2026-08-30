// spec: tests/db-admin-verification.plan.md
// seed: tests/seed-pgweb.spec.ts

import { test, expect } from '@playwright/test';
import { getPgWebUrl, DB_CREDENTIALS } from './support/aspire-helpers';
import { verifyDbAdminUiAccessible } from './support/db-admin-verification';
import { sleep, videoFileName } from './support/playwright-helpers';

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
          await page.selectOption('select[ID="connection_bookmarks"]', 'postgresdb');

          await connectButton.click();
        }
      },

       introSourceFiles: [
        {
          name: 'postgres',
          title: 'Now go to ASPIRE',
          description: 'It will show the PostgreSQL database and the vr_data table',
        },
      ],
      outroSourceFiles: [
        { name: 'PostgreSQL01.createTables.gen.txt', title: '', description: '' },
        { name: 'postgres-pgweb.spec.ts', title: '', description: '' },
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

