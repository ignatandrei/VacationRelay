import { test, expect } from '@playwright/test';
import { getSqliteWebUrl, navigateToAspire, navigateToSourceCode } from './support/aspire-helpers';
import { expectAndFlash, flash, sleep, videoFileName } from './support/playwright-helpers';

test.describe('SQLite – SqliteWeb ', () => {
  test('SQLite database is accessible and vr_data table exists in SqliteWeb', async ({ page },testInfo) => {
    const cast =await page.screencast;
    var indicator = await cast.showOverlay(
  '<h1>https://github.com/ignatandrei/VacationRelay </h1>'
);

  await navigateToSourceCode(page, cast, 'AppHost.cs');
  
  await cast.showChapter('Now go to ASPIRE', {
  description: 'It will show the SqliteWeb database and the vr_data table',
  duration: 5000,
});

    await navigateToAspire(page);

    await sleep(2);
    
    await cast.showChapter('Aspire Page', {
      description: 'SqliteWeb is accessible through Aspire',
      duration: 5000,
    });
    
    await sleep(2);
    
    await flash(page.getByText('sqlite-sqliteweb' ,{ exact: true }))

    // 1. Navigate to the SqliteWeb URL
    await page.goto(getSqliteWebUrl(),{waitUntil: 'networkidle' });

    await sleep(2);
    await cast.showChapter('Here it shows the SqliteWeb database', {

      description: 'Has 2 tables: vr_data and vr_data_history',
      duration: 5000,
    });
    await sleep(2);
    
    // 2. Wait for the sidebar / table list to appear
    await expectAndFlash(page.locator('#sidebar, .sidebar, nav, .table-list').first());

    // 3. Assert vr_data table is listed
    await expectAndFlash(page.getByText('vr_data').first());
    
    // 4. Assert vr_data_history table is listed
    await expectAndFlash(page.getByText('vr_data_history').first());

    await sleep(2);

    
    await cast.showChapter('Now we can see the vr_data definition', {
      description: 'Shows the structure and columns of the vr_data table',
      duration: 5000,
    });
    await sleep(2);
    
    // 5. Click vr_data to open the table
    await page.getByText('vr_data').first().click();

    // 6. Assert the table content panel loads with no error
    await expect(page.locator('table, .table-container, #content').first()).toBeVisible();
    await expect(page.locator('.error, .alert-danger')).not.toBeVisible();
    
    await sleep(2);
    
    await navigateToSourceCode(page, cast, 'SQLite01.createTables.gen.txt');
    
    await cast.showChapter('End of demo sqliteweb database', {
      description: 'If you want to see  the source code for this video , wait a bit',
      duration: 5000,
    });
    await sleep(2);
    
    await navigateToSourceCode(page, cast, 'sqlite-sqliteweb.spec.ts');
    
  });

  test('SqliteWeb shows correct schema for vr_data table', async ({ page }) => {
    // 1. Navigate to SqliteWeb
    await page.goto(getSqliteWebUrl());

    // 2. Open the vr_data table
    await page.getByText('vr_data').first().click();

    // 3. Open the structure / schema view if available
    const structureLink = page.getByRole('link', { name: /structure|schema/i });
    const hasStructure = await structureLink.isVisible({ timeout: 2000 }).catch(() => false);
    if (hasStructure) {
      await structureLink.click();
    }

    // 4. Assert the 'id' column is present in the schema
    await expect(page.getByText('id').first()).toBeVisible();
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
