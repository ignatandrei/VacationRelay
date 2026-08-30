Once every database backend (SQLite, SQL Server, PostgreSQL, MongoDB) could create its own schema at Aspire startup, I had a new problem: proving it, for all four providers, not just one, without writing four unrelated test suites by hand. My solution was to combine a single AppHost wiring pattern with one shared Playwright verification helper that every provider-specific spec reuses.

**AppHost.cs: one resource graph, four databases**

In `src/VacationRelay/VacationRelay/AppHost.cs` I add all four databases side by side, each paired with its own admin/viewer UI:

```csharp
builder.AddSqlite(name1)
    .WithSqliteWeb(c => { c.WithHttpEndpoint(targetPort: 8080, name: "http", port: portSqliteWeb); })
    .ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.Sqlite));

var sqlserver = builder.AddSqlServer("sqlserver", paramPass, 1433)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDbGate()
    .WithAdminer(c => { c.WithHostPort(ports.Resource.GetDeterministicPort("sqlserver")); });

var dbSqlServer = sqlserver.AddDatabase("vacationrelaySqlServer", "vacationrelay")
    .WithSqlPadViewerForDB(sqlserver)
    .ExecuteSqlServerScriptsAtStartup(ScriptsData.GetScripts(DatabaseType.SqlServer));

var postgres = builder.AddPostgres("postgres", username, password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgWeb(c => { c.WithHostPort(ports.Resource.GetDeterministicPort("postgres")); })
    .WithPgAdmin();

var postgresdb = postgres.AddDatabase("postgresdb", "vacationrelay")
    .ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.Postgres));

var mongo = builder.AddMongoDB("mongo", userName: username, password: password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithMongoExpress(c => { c.WithHostPort(ports.Resource.GetDeterministicPort("mongodb")); });

var mongodb = mongo.AddDatabase("mongodb", "vacationrelay")
    .ExecuteDBScripts(ScriptsData.GetScripts(DatabaseType.MongoDB));
```

**Deterministic ports, so I never have to scrape the dashboard**

I use `builder.AddPort().WithDeterministicPortEnvironment("postgres","sqliteweb","mongodb","FileDisplay").WithDeterministicPortEnvironment("sqlserver",13456).Construct()` to fix the host ports for each admin UI ahead of time. My tests read them back as `PORT_<name>` environment variables (`PORT_sqlserver`, `PORT_postgres`, `PORT_mongodb`, `PORT_sqliteweb`) in `tests/support/aspire-helpers.ts`, so I never hardcode a port or try to discover one by scraping the Aspire dashboard.

**The JavaScript test app runs inside Aspire itself**

```csharp
var jsTest = builder
    .AddJavaScriptApp("jsTest", "../VR.TestUI")
    .AddNpmCommandsFromPackage()
    .WithPortReference(ports)
    .WaitFor(aspire!);

aspire!.Resource.AddEnvironmentVariablesTo(jsTest);
```

I add my Playwright project (`VR.TestUI`) as just another Aspire resource. It waits for the `aspire` resource (a small `AddAspireResource()` helper I wrote that parses the running model), and `AddEnvironmentVariablesTo` forwards every Aspire resource endpoint/env var into the test process automatically — so I never have to guess a URL from inside a test.

**FileDisplay: showing the relevant source per database**

`builder.CreateFileDisplay(port: ...)` starts a small resource I built that serves source snippets over HTTP. For each database I call `files.AddFile(...)` to register the AppHost excerpt, the generated create-table script (e.g. `SQLite01.createTables.gen.txt`, `MongoDb01.createTables.gen.txt`, `SqlServer01.createTables.gen.txt`, `PostgreSQL01.createTables.gen.txt`), and the spec file itself. In my tests I call `navigateToSourceCode(page, cast, name)` to open these files mid-recording, so the Playwright video becomes a narrated walkthrough of "here is the code, here is the schema, here is the assertion" for each provider.

**One shared verification flow for all four providers**

The reuse I'm proudest of lives in `tests/support/db-admin-verification.ts`, where I export `verifyDbAdminUiAccessible(page, options)`. Every provider spec I wrote (`sqlite-sqliteweb.spec.ts`, `sqlserver-adminer.spec.ts`, `postgres-pgweb.spec.ts`, `mongodb-mongo-express.spec.ts`) calls this one function with provider-specific options instead of duplicating the flow:

- `dbLabel` — human readable name (e.g. "PostgreSQL")
- `resourceName` — the Aspire dashboard resource name I flash (e.g. "sqlite-sqliteweb")
- `getUrl` — a helper from `aspire-helpers.ts` (`getSqliteWebUrl`, `getAdminerUrl`, `getPgWebUrl`, `getMongoExpressUrl`) reading the matching `PORT_*` env var
- `tableNames` — expected tables/collections, e.g. `['vr_data','vr_data_history']`
- `login` — an optional per-provider connect step (Adminer needs driver/server selection and a submit click; PgWeb may need to pick a saved connection; SqliteWeb and Mongo Express auto-connect)
- `openTableAndVerify` — an optional override for how I open a table row; by default I click the text and assert a content panel appears with no error banner
- `introSourceFiles` / `outroSourceFiles` — which FileDisplay snippets I show before and after the check

```ts
export async function verifyDbAdminUiAccessible(
  page: Page,
  options: DbAdminVerificationOptions
): Promise<void> {
  // show overlay + intro source files, navigate to Aspire, flash resourceName
  await page.goto(getUrl(), { waitUntil: 'networkidle' });
  if (login) { await login(page); }

  for (const tableName of tableNames) {
    // assert each expected table/collection name is visible, handling duplicates
  }

  await openTableAndVerify(page, tableNames[0]);
  // show outro source files
}
```

**Provider-specific differences I captured as options, not new code**

- **SQLite (SqliteWeb)** — I need no login step; I use the default `openTableAndVerify`.
- **SQL Server (Adminer)** — my `login` selects the "mssql" driver and the "sqlserver" custom server option, submits, then clicks into the `vacationrelay` database; I override `openTableAndVerify` to look inside the `#tables` sidebar specifically.
- **PostgreSQL (PgWeb)** — my `login` only acts if a connect button/form is visible (PgWeb can auto-connect from the URL), otherwise it selects the `postgresdb` bookmark.
- **MongoDB (Mongo Express)** — my `tableNames` is just `['vacationrelay']` (the database itself), and I override `openTableAndVerify` to click "View" on the `vacationrelay` row, then "View" again on the `vr_data` collection row.

**Screencast lifecycle is identical across all my specs**

Every spec file I wrote registers the same `test.beforeEach`/`test.afterEach` pair: I start `page.screencast` to a file named from the test title (`videoFileName` from `tests/support/playwright-helpers.ts`), show an overlay with my name, show a "Starting ..." chapter, and stop the screencast after the test. Small helpers I built (`flash`, `flashAndClick`, `expectAndFlash`, `sleep`) provide the highlight/animation used throughout `db-admin-verification.ts`, so my recorded videos stay readable no matter which database is under test.

**Supporting files**

- `tests/db-admin-verification.plan.md` — the written test plan I made, enumerating scenarios for all four admin UIs (including negative cases like wrong credentials or a non-existent database), which I used as the spec these tests were generated from.
- `tests/playwright.config.ts` — single chromium project, sequential workers, HTML reporter, loads `.env` for local overrides of admin UI URLs.
- `tests/seed.spec.ts` — an empty seed template I keep around for scaffolding a new provider spec.

I think the AppHost proves independence at the orchestration layer (same API, different engine wiring), and my test layer proves it at the verification layer (same assertion shape — "database up, expected tables/collections visible, no error opening them" — reused via one function across four completely different admin UIs). If I add a fifth database backend, I only need to add an AppHost resource block, a `getXxxUrl()` helper, and a small options object — not a new test framework.

References:

Code: https://github.com/ignatandrei/VacationRelay/blob/main/src/VacationRelay/VacationRelay/AppHost.cs

Tests: https://github.com/ignatandrei/VacationRelay/tree/main/src/VacationRelay/VR.TestUI/tests


Video:

https://ignatandrei.github.io/VacationRelay/multiTestUI/SQLServer.webm
https://ignatandrei.github.io/VacationRelay/multiTestUI/MongoDB.webm
https://ignatandrei.github.io/VacationRelay/multiTestUI/PostgreSQL.webm
https://ignatandrei.github.io/VacationRelay/multiTestUI/SQLite.webm
