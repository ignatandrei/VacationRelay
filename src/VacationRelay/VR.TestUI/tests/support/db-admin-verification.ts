/**
 * Shared verification flow for "DB admin UI shows vr_data table" style tests.
 *
 * Each database-specific spec (sqlite, mongodb, postgres, sqlserver) has the same
 * shape: show the AppHost source, jump into Aspire, open the admin UI for that
 * database, log in if needed, and assert that the expected tables/collections are
 * listed and can be opened without error. This function captures that shared shape
 * so each spec only needs to supply the DB-specific bits (URL, resource name, login
 * steps, table names, source files to showcase).
 */
import type { Page, Screencast, TestInfo } from '@playwright/test';
import { expect } from '@playwright/test';
import { navigateToAspire, navigateToSourceCode } from './aspire-helpers';
import { expectAndFlash, flash, sleep } from './playwright-helpers';

export interface SourceFileToShow {
  /** File name as registered with CreateFileDisplay / shown in the FileDisplay app. */
  name: string;
  /** Chapter title shown before navigating to the source file. */
  title: string;
  /** Chapter description shown before navigating to the source file. */
  description: string;
}

export interface DbAdminVerificationOptions {
  /** Human readable label for the database, e.g. 'SQLite', 'PostgreSQL'. */
  dbLabel: string;
  /** The Aspire resource name shown on the dashboard, e.g. 'sqlite-sqliteweb'. */
  resourceName: string;
  /** Function returning the admin UI URL (throws if the required env var is missing). */
  getUrl: () => string;
  /** Table/collection names expected to be visible in the admin UI. */
  tableNames: string[];
  /** Source files to showcase before navigating to Aspire (e.g. AppHost.cs excerpt). */
  introSourceFiles?: SourceFileToShow[];
  /** Source files to showcase after verification (e.g. create-table scripts, the spec itself). */
  outroSourceFiles?: SourceFileToShow[];
  /**
   * Optional login/connect step run right after navigating to the admin UI
   * (e.g. Adminer or PgWeb connection forms). No-op for UIs that auto-connect.
   */
  login?: (page: Page) => Promise<void>;
  /**
   * Opens the given table/collection name and asserts its content loads without error.
   * Defaults to: click the first match of the text, then assert a table/content panel
   * is visible and no error is shown.
   */
  openTableAndVerify?: (page: Page, tableName: string) => Promise<void>;
}

async function defaultOpenTableAndVerify(page: Page, tableName: string): Promise<void> {
  await page.getByText(tableName).first().click();
  await expect(page.locator('table, .table-container, #content, .results, .table-content').first()).toBeVisible();
  await expect(page.locator('.error, .alert-danger')).not.toBeVisible();
}

/**
 * Runs the full "database is accessible and expected tables exist" verification flow
 * shared by SQLite, MongoDB, PostgreSQL and SQL Server admin UI tests.
 */
export async function verifyDbAdminUiAccessible(
  page: Page,
  options: DbAdminVerificationOptions
): Promise<void> {
  const cast: Screencast = await page.screencast;
  const { dbLabel, resourceName, getUrl, tableNames, introSourceFiles = [], outroSourceFiles = [], login } = options;
  const openTableAndVerify = options.openTableAndVerify ?? defaultOpenTableAndVerify;

  await cast.showOverlay('<h1>https://github.com/ignatandrei/VacationRelay </h1>');

  for (const file of introSourceFiles) {
    await navigateToSourceCode(page, cast, file.name);
    await cast.showChapter(file.title, { description: file.description, duration: 5000 });
  }

  await navigateToAspire(page);
  await sleep(2);

  await cast.showChapter('Aspire Page', {
    description: `${resourceName} is accessible through Aspire`,
    duration: 5000,
  });
  await sleep(2);

  await flash(page.getByText(resourceName, { exact: true }));

  // 1. Navigate to the admin UI URL
  await page.goto(getUrl(), { waitUntil: 'networkidle' });
  await sleep(2);

  // 2. Log in / connect if this admin UI requires it
  if (login) {
    await login(page);
  }

  await cast.showChapter(`Here it shows the ${dbLabel} database`, {
    description: `Has tables/collections: ${tableNames.join(', ')}`,
    duration: 5000,
  });
  await sleep(2);

  // 3. Wait for the sidebar / table list to appear
  await expectAndFlash(page.locator('#sidebar, .sidebar, nav, .table-list,#breadcrumb').first());

  // 4. Assert every expected table/collection is listed
  for (const tableName of tableNames) {
    var element = page.getByText(tableName);
    var nr = await element.count();
    console.log(`element count for ${tableName}: ${nr}`);
    if(nr == 0){
      throw new Error(`Expected table/collection ${tableName} not found in ${dbLabel} admin UI`); 
    }
    if(nr >1){
      let isFlashable = false;
      var all=await element.all();
      for(var i=0;i<all.length;i++){
        var el = all[i];
        try{
          await el.scrollIntoViewIfNeeded({timeout: 1000});
          isFlashable = true;
          element = el;
        } catch {
          console.error(`Error flashing element ${i}:`);
        }
        if(isFlashable){
          break;
        }
      }
      if(!isFlashable)
        throw new Error(`Expected table/collection ${tableName} found multiple times in ${dbLabel} admin UI`);
    }

    await expectAndFlash(element);
  }

  await sleep(2);
  await cast.showChapter(`Now we can see the ${tableNames[0]} definition`, {
    description: `Shows the structure and columns of the ${tableNames[0]} table`,
    duration: 5000,
  });
  await sleep(2);

  // 5. Open the first table/collection and assert its content loads with no error
  await openTableAndVerify(page, tableNames[0]);
  await sleep(2);

  for (const file of outroSourceFiles) {
    await navigateToSourceCode(page, cast, file.name);
  }

  await cast.showChapter(`End of demo ${dbLabel} database`, {
    description: 'If you want to see the source code for this video, wait a bit',
    duration: 5000,
  });
  await sleep(2);
}
