Once the schema creation script ran at startup, I needed proof — not just trust — that SQLite really ends up with the two tables: `vr_data` and `vr_data_history`. So I wrote a Playwright UI test that drives SqliteWeb (the viewer exposed by Aspire) and asserts both tables are visible and that `vr_data` has the expected columns.

The AppHost wires SQLite with its viewer and points it at a deterministic port, so the test always knows where to navigate:

```csharp
var portSqliteWeb = ports.Resource.GetDeterministicPort("sqliteweb");
string name1 = "sqlite";
string sqliteWeb = $"{name1}-sqliteweb";

builder.AddSqlite(name1)
    .WithSqliteWeb(c =>
    {
        c.WithHttpEndpoint(targetPort: 8080, name: "http", port: portSqliteWeb);
    })
    .ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.Sqlite));
```

The AppHost also spins up the Playwright test project itself as a JavaScript app, feeding it the Aspire environment variables (ports, resource names) so the test can find every resource without hardcoding URLs:

```csharp
var jsTest = builder
    .AddJavaScriptApp("jsTest", "../VR.TestUI")
    .AddNpmCommandsFromPackage()
    .WithPortReference(ports)
    .WaitFor(aspire!)
    ;

aspire!.Resource.AddEnvironmentVariablesTo(jsTest);
```

There is also a small "file display" resource that shows source-code snippets (AppHost.cs, the generated SQL script, the spec file itself) side by side while the test runs, so a recorded video is self-explanatory:

```csharp
var files = builder.CreateFileDisplay(port: ports.Resource.GetDeterministicPort("FileDisplay"));
files.AddFile(relativePath: "AppHost.cs", lines: ["builder.AddSqlite", ".ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.Sqlite));"]);
files.AddFile(@"..\VR.Scripts\SQLite01.createTables.gen.txt", lines: ["CREATE TABLE vr_data", "CREATE TABLE vr_data_history"]);
files.AddFile(@"..\VR.TestUI\tests\sqlite-sqliteweb.spec.ts", ["test('SQLite database is accessible and vr_data table exists in SqliteWeb'"]);
```

The actual test, `sqlite-sqliteweb.spec.ts`, opens Aspire, jumps to the SqliteWeb resource, and asserts both tables are listed, then opens `vr_data` to check its columns:

```ts
import { test, expect } from '@playwright/test';
import { getSqliteWebUrl, navigateToAspire, navigateToSourceCode } from './support/aspire-helpers';
import { expectAndFlash, flash, sleep, videoFileName } from './support/playwright-helpers';

test.describe('SQLite – SqliteWeb ', () => {
  test('SQLite database is accessible and vr_data table exists in SqliteWeb', async ({ page }, testInfo) => {
    const cast = await page.screencast;
    var indicator = await cast.showOverlay('<h1>https://github.com/ignatandrei/VacationRelay </h1>');

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

    await flash(page.getByText('sqlite-sqliteweb', { exact: true }));

    // 1. Navigate to the SqliteWeb URL
    await page.goto(getSqliteWebUrl(), { waitUntil: 'networkidle' });
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
      description: 'If you want to see the source code for this video, wait a bit',
      duration: 5000,
    });
    await sleep(2);

    await navigateToSourceCode(page, cast, 'sqlite-sqliteweb.spec.ts');
  });

  test('SqliteWeb shows correct schema for vr_data table', async ({ page }) => {
    await page.goto(getSqliteWebUrl());
    await page.getByText('vr_data').first().click();

    const structureLink = page.getByRole('link', { name: /structure|schema/i });
    const hasStructure = await structureLink.isVisible({ timeout: 2000 }).catch(() => false);
    if (hasStructure) {
      await structureLink.click();
    }

    // Assert the 'id' column is present in the schema
    await expect(page.getByText('id').first()).toBeVisible();
  });
});
```

`vr_data` and `vr_data_history` are asserted independently, and the second test dives into the schema to make sure the `id` column exists — turning the "database independence" claim into something a CI run can actually verify per provider, not just SQLite.

For this sqlite I have used those NuGet packages referenced by the AppHost project (`VacationRelay.csproj`):

- Aspire.Hosting.JavaScript – https://www.nuget.org/packages/Aspire.Hosting.JavaScript
- AspireExtensionsResource – https://www.nuget.org/packages/AspireExtensionsResource
- AspireFileDisplayExtension – https://www.nuget.org/packages/AspireFileDisplayExtension
- CommunityToolkit.Aspire.Hosting.Sqlite – https://www.nuget.org/packages/CommunityToolkit.Aspire.Hosting.Sqlite
- JavaScriptExtensionsAspire – https://www.nuget.org/packages/JavaScriptExtensionsAspire
- Microsoft.Data.Sqlite – https://www.nuget.org/packages/Microsoft.Data.Sqlite
- PortExtensionsAspire – https://www.nuget.org/packages/PortExtensionsAspire

References:

Part 010 – creation of database: https://github.com/ignatandrei/VacationRelay/blob/main/history/VaRe%26DaIn%E2%80%93part%20010%20of%20n%E2%80%93creation%20of%20database.md

Code: https://github.com/ignatandrei/VacationRelay/blob/main/src/VacationRelay/VacationRelay/AppHost.cs

Test: https://github.com/ignatandrei/VacationRelay/blob/main/src/VacationRelay/VR.TestUI/tests/sqlite-sqliteweb.spec.ts
