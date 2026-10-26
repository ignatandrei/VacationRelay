# Vacation Relay Plugins GUI - Browser Test Plan

## Scope and inspection

- Target: https://gui-efcore-startplugins-host.dev.localhost:7249/
- Project: `src\VacationRelay\VR.TestUIPlugins`
- Seed: `tests\seed.spec.ts` (currently empty; each generated test needs its own navigation).
- Inspected: 2026-10-26, Chromium, desktop and 390x844 mobile viewport.
- The requested playwright-test-planner agent produced a provisional plan after its browser bootstrap failed. Direct Playwright CLI inspection subsequently succeeded; this document replaces that provisional plan with live observations.
- This is a plan, not implemented tests or a full execution report. Cases marked **Observed** have supporting exploratory evidence; **Proposed** cases still need execution.

Despite its hostname, the inspected UI manages vacation relay records, not plugin installation. No edit, plugin-management, pagination, or authentication controls were observed. Do not generate tests for those features without new evidence.

### Observed functionality

The root page has title and H1 `Vacation relay data`. Desktop navigation contains the `VR_GUI` brand and `Vacation Relay Data` link, both targeting the root. `About` links to Microsoft ASP.NET Core documentation.

Three named regions are initially visible:

1. `Find records`: `Person in vacation` and `Replacement person` text fields, `Search`, and `Clear`.
2. `Records`: either `No vacation relay records found.` or a table with `Person in vacation`, `Replacement person`, `From`, `To`, and `Actions` headers. Each record has `Details` and `Delete` buttons.
3. `Add a vacation relay`: required person text fields, `From date` defaulted to the browser's current date during inspection, optional `To date (optional)`, and `Create record`.

`Details` adds an inline `Record details` region with ID, both people, and From/To values. An omitted end date displays `Ongoing`. Creation clears the new-record person fields. Deletion happened immediately, without a confirmation dialog. Success messages use role `status`; API failures use role `alert`.

A uniquely named inspection record was created, inspected, filtered, and deleted. Its ID was `783f636c-6b3b-41ab-bb32-2bc300eeeab1`; cleanup was confirmed by the deletion status and empty results. A reversed-date submission returned 400 and did not create a row. Only the inspection record was deleted.

### Observed API routes

The browser used a separate API origin, `https://localhost:7250`, not the GUI origin:

| Operation | Method and path | Observed result |
|---|---|---|
| List | `GET /api/vrdata/` | 200 |
| Vacation-person search | `GET /api/vrdata/?idPersonInVacation=<value>` | 200, no-match empty state |
| Create | `POST /api/vrdata/` | 201; reversed dates returned 400 |
| Details | `GET /api/vrdata/<id>` | 200 |

Replacement-person filtering and deletion succeeded through the UI, but their exact request contracts were not captured. Inspect those requests before writing route/body assertions; do not assume parameter names, deletion method, or JSON casing.

## Environment and isolation

- The app and API must already be running. Do not add service-start/restart operations to these tests or reuse the unrelated SqliteWeb screencast setup.
- Local TLS initially caused `ERR_CERT_VERIFIER_CHANGED` failures on Blazor WASM assets and the unhandled-error overlay. A temporary browser context with `ignoreHTTPSErrors: true` allowed inspection. If required for automation, limit that setting to this local test context and both local origins; do not change OS trust or application security.
- Run with a fresh browser context for every test. Use unique person identifiers including run, project, worker, and retry identifiers. Never assume the shared database is empty.
- Mutation tests require an approved test environment. Create only case-owned fixtures and delete only their captured IDs in teardown, even after assertion failures. Do not delete all rows or choose a row by index.
- Create each case's prerequisites independently. Two-record search tests should use distinct unique identifiers with a shared run-specific prefix.
- Use browser-local interception for error cases. Match API origin, path, and HTTP method; continue unrelated requests. Remove routes in teardown. Do not stop backends to create failures.
- Use fixed dates, locale, and timezone in tests that assert date rendering. Inspection showed a row date of `10/26/2026` but details of `10/26/2026 2:00 AM`; do not assume date-only and timestamp presentations are identical.
- For empty-database coverage, use an isolated dataset or a schema-correct intercepted empty list. A no-match search can safely cover the empty-result UI on shared data.

## Locator inventory

Prefer roles and labels. Duplicate person labels require region scoping:

```ts
const find = page.getByRole('region', { name: 'Find records', exact: true });
const add = page.getByRole('region', { name: 'Add a vacation relay', exact: true });
const records = page.getByRole('region', { name: 'Records', exact: true });
const details = page.getByRole('region', { name: 'Record details', exact: true });

find.getByLabel('Person in vacation', { exact: true });
find.getByLabel('Replacement person', { exact: true });
find.getByRole('button', { name: 'Search', exact: true });
find.getByRole('button', { name: 'Clear', exact: true });
add.getByLabel('Person in vacation', { exact: true });
add.getByLabel('Replacement person', { exact: true });
add.getByLabel('From date', { exact: true });
add.getByLabel('To date (optional)', { exact: true });
add.getByRole('button', { name: 'Create record', exact: true });

const row = records.getByRole('row').filter({
  has: page.getByRole('cell', { name: uniqueVacationPerson, exact: true }),
});
row.getByRole('button', { name: 'Details', exact: true });
row.getByRole('button', { name: 'Delete', exact: true });
page.getByRole('status');
page.getByRole('alert');
page.getByRole('button', { name: 'Navigation menu', exact: true });
```

Observed input IDs, if needed for native validity inspection: `filter-vacation`, `filter-replacement`, `new-vacation`, `new-replacement`, `new-from`, and `new-to`. Date controls are native `type=date`; both new-person fields have `required`, while neither date input had a native required/min/max attribute. No maximum length was exposed for the text fields.

## Test scenarios

Priorities: P0 = critical record workflows; P1 = validation, resilience, and usability; P2 = contract-dependent boundaries. All cases start from a fresh context at the target URL and create their own fixtures unless explicitly using intercepted data. Expected outcomes below are acceptance assertions to implement; only **Observed** items were explored.

### 1. Entry and navigation

**Seed:** `tests\seed.spec.ts`  
**Proposed file:** `tests\plugins\entry.spec.ts`

#### GUI-001 - P0 - Render the application shell [Observed]
1. Navigate to the root and wait for the H1 rather than a fixed delay.
2. Inspect the title, three initial named regions, and primary controls.
3. Expect title/H1 `Vacation relay data`, usable fields/buttons, and no persistent loading or unhandled-error overlay.

#### GUI-002 - P1 - Root navigation and reload [Proposed]
1. Load the root, follow `Vacation Relay Data`, then the `VR_GUI` brand.
2. Reload the page.
3. Expect the same usable root view and a completed records load. Do not expect unsaved field values to survive reload unless separately specified.

#### GUI-003 - P1 - Empty records display [Observed]
1. Arrange an isolated empty list, or search for a unique nonexistent vacation person.
2. Wait for the list request and UI update.
3. Expect `No vacation relay records found.` in `Records`, no data rows, and the creation form still available.

### 2. Record creation and validation

**Seed:** `tests\seed.spec.ts`  
**Proposed file:** `tests\plugins\creation.spec.ts`

#### GUI-004 - P0 - Create an ongoing vacation relay [Observed]
1. Fill both new-person fields with unique identifiers and a fixed From date; leave To empty.
2. Submit once.
3. Expect `Vacation relay record created.` in a status region, one matching row, matching people/From date, and `Ongoing` in To.
4. Expect new-person fields cleared. Capture the created ID for cleanup.

#### GUI-005 - P0 - Create with an explicit end date [Proposed]
1. Fill unique people and valid fixed From/To dates, with To later than From.
2. Submit, then open the matching row's details.
3. Expect a successful record with the supplied dates and no `Ongoing` value for that row/details. Delete the case-owned record.

#### GUI-006 - P1 - Required vacation person [Observed for both-empty submission]
1. Leave Person in vacation empty; fill Replacement person and valid dates.
2. Submit.
3. Expect native invalidity on the required field, focus on it, no create request, no success status, and no new record. Avoid asserting localized browser validation text.

#### GUI-007 - P1 - Required replacement person [Proposed]
1. Fill Person in vacation; leave Replacement person empty.
2. Submit.
3. Expect required-field invalidity and no create request or new record; preserve the valid vacation-person value.

#### GUI-008 - P1 - Reject an end date before the start [Observed]
1. Fill unique people, From `2026-10-26`, To `2026-10-25`.
2. Submit.
3. Expect 400 and alert `Unable to create the record (400 Bad Request).`; no matching created row or success status. Expect entered values retained for correction.

#### GUI-009 - P1 - Correct a rejected submission [Proposed]
1. Reproduce GUI-008 within this case.
2. Change To to a valid later date and submit.
3. Expect one successful record, error cleared, and success status. Clean up that record.

#### GUI-010 - P2 - Same-day and missing-start boundaries [Proposed; contract confirmation required]
1. In independent subcases, set To equal to From; then clear From with both people present.
2. Submit each case in an isolated test dataset.
3. Confirm intended acceptance/rejection with the API contract first. Assert either exact persisted dates or an explicit actionable error with no record created. Neither boundary was explored; do not invent a required attribute or same-day restriction.

#### GUI-011 - P2 - Person-value boundaries [Proposed; contract confirmation required]
1. Independently submit whitespace-only, padded, Unicode, and long unique person values.
2. Establish intended normalization and length limits before asserting.
3. Expect accepted values to round-trip correctly, or rejected values to produce an explicit error without writes. Verify rendered values are text; do not introduce arbitrary truncation expectations.

### 3. Searching and clearing

**Seed:** `tests\seed.spec.ts`  
**Proposed file:** `tests\plugins\search.spec.ts`

#### GUI-012 - P0 - Search by vacation person [Proposed; no-match variant observed]
1. Create two independent records with distinct unique vacation people.
2. Enter one exact vacation person in `Find records`; click Search.
3. Expect its row present and the other fixture absent. Confirm request uses `idPersonInVacation`.
4. Do not assume partial-match or case-folding semantics.

#### GUI-013 - P0 - Search by replacement person [Observed]
1. Create records with distinct replacements.
2. Search using one exact replacement identifier and empty vacation filter.
3. Expect the corresponding fixture and no nonmatching fixture. Capture the actual replacement query parameter during implementation.

#### GUI-014 - P1 - Combine both filters [Proposed]
1. Create fixtures A/V1/R1 and B/V2/R2.
2. Search V1 plus R1, then V1 plus R2.
3. Confirm intended filter-combination semantics. If the contract specifies AND, expect A for the first search and no matches for the second; do not encode AND solely from control names.

#### GUI-015 - P1 - No-match search then Clear [Observed]
1. Create an owned fixture; search a unique nonexistent person.
2. Expect the empty-result message and no details panel from a previously selected record.
3. Click Clear.
4. Expect both search fields empty, unfiltered load, and the owned fixture visible again.

#### GUI-016 - P1 - Search and create fields remain independent [Proposed]
1. Enter unsaved creation values.
2. Search and Clear using the separate filter fields.
3. Expect creation values unchanged, filters reset by Clear, and no create request. Inspection showed creation values survived search/clear, but no comprehensive automated assertions were run.

### 4. Details and deletion

**Seed:** `tests\seed.spec.ts`  
**Proposed file:** `tests\plugins\records.spec.ts`

#### GUI-017 - P0 - Open matching record details [Observed]
1. Create a unique ongoing record and click its scoped Details button.
2. Expect inline `Record details`, a nonempty ID matching the created record, both identifiers, From, and `Ongoing` for To.
3. Expect the root URL unchanged; no dialog is expected.

#### GUI-018 - P1 - Switch details between records [Proposed]
1. Create two distinct fixtures and open Details for each in turn.
2. Expect the inline panel to match the most recently selected ID/people/dates, without mixing values from the first.

#### GUI-019 - P0 - Delete only the selected record [Observed for a single fixture]
1. Create two owned records and open Details for the first.
2. Click Delete scoped to that first row; do not register a confirmation-dialog expectation.
3. Expect `Vacation relay record deleted.`, the first row absent, and the second still present.
4. Proposed invariant: details for the deleted record are cleared. Reload/search to verify deletion persists, then clean up the remaining fixture.

#### GUI-020 - P1 - Last filtered result deletion [Observed]
1. Create and filter to a unique fixture.
2. Delete its scoped row.
3. Expect deletion status plus `No vacation relay records found.` and no orphaned details. Do not infer that the shared database is globally empty.

#### GUI-021 - P1 - Persistence and date rendering after reload [Proposed]
1. Create a unique record with fixed dates.
2. Reload, search for it, and open Details.
3. Expect persisted identifiers and date values. Pin locale/timezone and separately assert list date and details timestamp; investigate unintended date shifts rather than copying the observed 2 AM representation as a universal requirement.

### 5. Error handling and asynchronous states

**Seed:** `tests\seed.spec.ts`  
**Proposed file:** `tests\plugins\errors.spec.ts`

#### GUI-022 - P1 - List failure and recovery [Observed]
1. Intercept only the collection GET in this browser context with status 500; click Search.
2. Expect role alert beginning `Unable to load vacation relay records:` and no success status or fatal overlay.
3. Remove interception; click Clear.
4. Expect a completed normal load and alert removed. Current UI also shows the empty-result message during this failure; record that separately from a confirmed empty response.

#### GUI-023 - P1 - Create failure preserves input [Proposed]
1. Fill a valid unique form; intercept only its POST with 500 or abort, without forwarding the write.
2. Submit.
3. Expect explicit error, no creation success/new row, and values retained.
4. Remove interception and retry once; expect exactly one persisted record and clean it up.

#### GUI-024 - P1 - Details failure [Proposed]
1. Create an owned fixture and intercept only its details GET with 404, then 500 in independent subcases.
2. Click Details.
3. Expect explicit error, no fabricated values or another record's stale details.
4. Remove interception and retry; expect correct details. Exact error copy remains to be discovered.

#### GUI-025 - P1 - Delete failure does not report success [Proposed]
1. Create an owned fixture and discover the real delete request contract.
2. Intercept only that record's delete request with 500, without forwarding it; click its Delete.
3. Expect an explicit failure and no deletion success. Verify the fixture still exists after a normal reload.
4. Remove interception and delete only that fixture successfully.

#### GUI-026 - P1 - Delayed requests and repeat actions [Proposed]
1. Delay the relevant GET/POST response in independent subcases; inspect loading and disabled controls.
2. Attempt repeated Search and Create actions only using owned data/intercepted writes.
3. Proposed reliability invariants: loading eventually ends, the form remains recoverable, and one user submission is not persisted multiple times. Confirm desired duplicate-submission policy before making it a strict product requirement.
4. Release delayed requests; remove routes and clean up every case-owned persisted ID.

### 6. Responsive layout and accessibility

**Seed:** `tests\seed.spec.ts`  
**Proposed file:** `tests\plugins\usability.spec.ts`

#### GUI-027 - P1 - Mobile navigation [Observed]
1. Set viewport to 390x844 before navigation.
2. Expect `Navigation menu` and initially collapsed navigation.
3. Activate the menu; expect expanded state and `Vacation Relay Data` link.
4. Follow the link and verify the usable root page. Check closing/toggling behavior during implementation; only opening was explored.

#### GUI-028 - P1 - Responsive forms and populated table [Proposed; empty mobile layout observed]
1. Independently run at 1440x900, 768x1024, and 390x844 with owned rows including long identifiers.
2. Use filters, creation fields, Details, and Delete through visible controls.
3. Expect no clipping or overlap that blocks actions; wide tables may use an intentional usable scroll container. At 390px with empty results, document width was exactly 390px during inspection; populated-table overflow remains unverified.

#### GUI-029 - P1 - Keyboard, labels, and announcements [Proposed; roles/labels observed]
1. Tab/Shift+Tab through navigation, search, row actions, and creation fields.
2. Activate safe buttons by keyboard; inspect visible focus and logical order.
3. Verify duplicate person labels resolve within their named regions and table headers identify cells.
4. Trigger required-field invalidity and a browser-local API failure, then successful creation/deletion of owned data.
5. Expect keyboard operability without traps, meaningful field names, and accessible alert/status announcements. Native accessibility-tree roles alone do not prove screen-reader announcements.

#### GUI-030 - P2 - Zoom and cross-browser parity [Proposed]
1. Execute the P0 journeys in configured Chromium, Firefox, and WebKit.
2. Check native date entry and required validation without browser-specific message assertions.
3. Inspect the critical workflow at 200% browser zoom, separately from viewport resizing.
4. Expect essential controls/content reachable and equivalent record behavior. Record unsupported environments as blocked rather than passed.

## Findings and unresolved expectations

| Observation | Classification / implication |
|---|---|
| Initial WASM loads failed with `ERR_CERT_VERIFIER_CHANGED`; temporary context TLS exception allowed startup. | Local browser/certificate environment issue, not proof of an application defect. |
| Development browser-refresh WebSocket handshakes returned 403 on `ws://localhost:64424/` and `wss://localhost:64426/`. | Observed development-tool noise; CRUD remained usable. Do not assert a completely clean console without a narrowly justified allowlist. |
| Reversed date range returns generic 400 text instead of field-specific guidance. | Confirm desired UX; current rejection is verified, clearer guidance is an improvement candidate. |
| A list 500 shows an alert and `No vacation relay records found.` simultaneously. | Potentially misleading empty-state UX; do not equate load failure with a confirmed empty dataset. |
| Delete acts immediately, without confirmation. | Observed behavior, not automatically a defect. Tests must never target shared rows. |
| Details displayed 2 AM for the inspected date while the table displayed only the date. | Date/time presentation needs explicit locale/timezone expectations; no data-corruption claim established. |
| Partial/case-insensitive search, combined-filter semantics, whitespace normalization, same-day dates, missing From, and duplicate policy were not explored. | Confirm contracts before generating acceptance assertions. |

## Automation and execution notes

- No tests were implemented by this planning task. Generate the proposed files under `tests\plugins` using `tests\seed.spec.ts` as the seed.
- Root `playwright.config.ts` has `fullyParallel`, three browser projects, HTML reporting, and retry traces. Explicitly select that config; another config exists under `tests`.
- No baseURL or webServer is enabled. Navigate with the supplied absolute GUI URL or introduce a test-scoped configurable URL when implementing. Discover API origin from application configuration/traffic rather than freezing port 7250 as a permanent invariant.
- Existing `tests\support` helpers target Aspire/DB-admin videos and environment variables, not this page. Do not add their fixed sleeps, unlimited timeout, or database-admin navigation to these functional tests.
- Use auto-retrying locators and wait for the relevant response/state transition. Avoid asserting immediately after request dispatch or using fixed sleeps.
- Select new GUI cases independently of the existing database demo:

```powershell
Set-Location 'D:\eu\GitHub\VacationRelay\src\VacationRelay\VR.TestUIPlugins'
npx playwright test --config=playwright.config.ts tests\plugins --project=chromium
```

- After targeted P0 coverage works, run the generated folder across all configured browser projects. Mutation fixtures and cleanup must remain independent under parallel execution and retries.
- Exit criteria: all P0 cases pass, owned data is removed even on failure, P1 outcomes are recorded, and contract-dependent P2 scenarios remain explicitly blocked until expectations are resolved. Exploratory observations in this plan do not count as an automated passing suite.
