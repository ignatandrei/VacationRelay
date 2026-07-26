# MongoDB Test Plan – VacationRelay

## Application Overview

VacationRelay is a .NET Aspire 13.4 distributed application that provisions a MongoDB instance (via Docker container with persistent lifetime) containing the `vacationrelay` database. At startup, two collections are created: `vr_data` (active vacation relay records) and `vr_data_history` (historical records). Each document shares the same schema: id (ObjectId), idpersoninvacation (string ≤50), idpersonreplace (string ≤50), fromdate (datetime, required), todate (datetime, nullable). The Aspire AppHost exposes two MongoDB admin UIs – MongoExpress and DbGate – accessible from the Aspire dashboard. A custom Aspire dashboard command `ExecuteDBScripts` re-runs the collection-creation scripts against the live database. Run `aspire start` from `src/VacationRelay/VacationRelay` to start all resources before executing any test. Wait for the `mongodb` resource to show Ready in `aspire describe` before proceeding.

## Test Scenarios

### 1. Aspire Resource Startup – MongoDB

**Seed:** `src/VacationRelay/VR.TestUI/tests/seed.spec.ts`

#### 1.1. MongoDB container reaches Ready state

**File:** `tests/mongodb/01-startup.spec.ts`

**Steps:**
  1. Run `aspire start --non-interactive` from the `src/VacationRelay/VacationRelay` directory in a terminal
    - expect: The Aspire CLI prints the dashboard URL and begins starting resources
  2. Run `aspire wait mongo` and observe the result
    - expect: Command exits with code 0
    - expect: The `mongo` resource transitions to Running/Ready state within 60 seconds
  3. Run `aspire describe --format Json` and inspect the `mongo` and `mongodb` resource entries
    - expect: Both `mongo` (container) and `mongodb` (database) resources show state `Running`
    - expect: Connection string for `mongodb` is non-empty

#### 1.2. vr_data and vr_data_history collections are created at startup

**File:** `tests/mongodb/02-collections-exist.spec.ts`

**Steps:**
  1. After `aspire wait mongodb` succeeds, open the Aspire dashboard URL shown by `aspire start` in a browser
    - expect: Dashboard loads and shows the `mongodb` resource as Running
  2. In the Aspire dashboard, click on the `mongodb` resource row to view its details and locate the MongoExpress endpoint URL
    - expect: An endpoint URL for MongoExpress is visible and clickable
  3. Navigate to the MongoExpress URL and log in if required
    - expect: MongoExpress home page loads showing connected databases
  4. Locate the `vacationrelay` database in the MongoExpress sidebar
    - expect: `vacationrelay` database is listed
  5. Click on the `vacationrelay` database and view its collections
    - expect: Collections `vr_data` and `vr_data_history` are both listed
    - expect: Both collections have 0 documents (fresh state)

#### 1.3. ExecuteDBScripts Aspire command re-creates collections without error

**File:** `tests/mongodb/03-execute-scripts-command.spec.ts`

**Steps:**
  1. On the Aspire dashboard, select the `mongodb` resource and click the custom command `ExecuteDBScripts`
    - expect: Command triggers and the dashboard shows a progress or completion indicator
  2. Wait for the command result to appear in the dashboard
    - expect: The result shows `Success: true` and `Message: All scripts executed successfully.`
  3. Navigate back to MongoExpress and verify the `vacationrelay` database still has both `vr_data` and `vr_data_history` collections
    - expect: Both collections still exist (MongoDB `create` is idempotent when the collection already exists)

### 2. MongoExpress UI – CRUD on vr_data

**Seed:** `src/VacationRelay/VR.TestUI/tests/seed.spec.ts`

#### 2.1. Insert a valid vacation relay document via MongoExpress

**File:** `tests/mongodb/04-mongoexpress-insert.spec.ts`

**Steps:**
  1. Navigate to MongoExpress and open the `vacationrelay` > `vr_data` collection
    - expect: Collection view loads with 0 documents
  2. Click the 'New Document' or 'Insert' button
    - expect: A JSON editor or form for a new document appears
  3. Enter the following JSON and save: `{ "idpersoninvacation": "alice", "idpersonreplace": "bob", "fromdate": { "$date": "2026-08-01T00:00:00Z" }, "todate": { "$date": "2026-08-15T00:00:00Z" } }`
    - expect: The document is saved without error
    - expect: The collection now shows 1 document
  4. Click on the document to view its full detail
    - expect: An auto-generated `_id` field (ObjectId) is present
    - expect: All fields (`idpersoninvacation`, `idpersonreplace`, `fromdate`, `todate`) match the inserted values

#### 2.2. Insert a document with null todate (open-ended vacation)

**File:** `tests/mongodb/05-mongoexpress-null-todate.spec.ts`

**Steps:**
  1. In MongoExpress, open `vr_data` and create a new document: `{ "idpersoninvacation": "charlie", "idpersonreplace": "dave", "fromdate": { "$date": "2026-09-01T00:00:00Z" } }`
    - expect: Document is saved without error
  2. View the saved document
    - expect: `todate` field is absent or null
    - expect: `fromdate` is correctly stored
    - expect: `_id` is auto-generated

#### 2.3. Query documents by idpersoninvacation via MongoExpress filter

**File:** `tests/mongodb/06-mongoexpress-query.spec.ts`

**Steps:**
  1. In MongoExpress `vr_data` collection view, locate the filter/query input field
    - expect: A query input field is present
  2. Enter the filter `{ "idpersoninvacation": "alice" }` and submit
    - expect: Only the document with `idpersoninvacation: alice` is returned
    - expect: The charlie document is NOT shown
  3. Clear the filter and enter `{ "idpersoninvacation": "nonexistent" }`
    - expect: No documents are returned
    - expect: The UI shows an empty result set

#### 2.4. Update an existing document in vr_data via MongoExpress

**File:** `tests/mongodb/07-mongoexpress-update.spec.ts`

**Steps:**
  1. In MongoExpress, locate the `alice` document in `vr_data` and click Edit
    - expect: The document editor opens with current values pre-filled
  2. Change `idpersonreplace` from `bob` to `eve` and save
    - expect: Save completes without error
  3. View the updated document
    - expect: `idpersonreplace` is now `eve`
    - expect: All other fields (`idpersoninvacation`, `fromdate`, `todate`) remain unchanged
    - expect: `_id` is unchanged

#### 2.5. Delete a document from vr_data via MongoExpress

**File:** `tests/mongodb/08-mongoexpress-delete.spec.ts`

**Steps:**
  1. In MongoExpress `vr_data`, locate the `charlie` document and click Delete
    - expect: A confirmation prompt appears
  2. Confirm the deletion
    - expect: The document is removed
    - expect: The collection now shows 1 document (alice/eve)
  3. Refresh the collection view
    - expect: charlie's document is permanently removed and does not reappear

### 3. DbGate UI – CRUD on vr_data

**Seed:** `src/VacationRelay/VR.TestUI/tests/seed.spec.ts`

#### 3.1. Connect to MongoDB via DbGate and verify collections

**File:** `tests/mongodb/09-dbgate-connect.spec.ts`

**Steps:**
  1. In the Aspire dashboard, locate the DbGate endpoint URL for the `mongo` resource and navigate to it
    - expect: DbGate loads in the browser
  2. In the DbGate left panel, expand the MongoDB connection and then the `vacationrelay` database
    - expect: The `vacationrelay` database node is visible
    - expect: Child nodes `vr_data` and `vr_data_history` are listed under it

#### 3.2. Browse vr_data_history collection in DbGate

**File:** `tests/mongodb/10-dbgate-history.spec.ts`

**Steps:**
  1. Double-click `vr_data_history` in the DbGate tree
    - expect: A data grid opens for `vr_data_history`
  2. Verify the collection content (fresh state)
    - expect: The grid shows 0 rows
    - expect: Column headers or field names are not enforced (schema-less)

#### 3.3. Insert a document into vr_data_history via DbGate

**File:** `tests/mongodb/11-dbgate-insert-history.spec.ts`

**Steps:**
  1. In DbGate, right-click `vr_data_history` and select 'Insert Row' or open the insert panel
    - expect: An insert form or JSON editor appears
  2. Insert the document: `{ "idpersoninvacation": "alice", "idpersonreplace": "bob", "fromdate": ISODate("2026-07-01T00:00:00Z"), "todate": ISODate("2026-07-15T00:00:00Z") }` and confirm
    - expect: Document is inserted without error
  3. Refresh the `vr_data_history` grid
    - expect: 1 document is shown with the inserted values
    - expect: `_id` is auto-generated

#### 3.4. Run a MongoDB query from DbGate query editor

**File:** `tests/mongodb/12-dbgate-query.spec.ts`

**Steps:**
  1. In DbGate, open the query editor (SQL or MongoDB query tab) for the `vacationrelay` database
    - expect: A query editor panel opens
  2. Execute the query `db.vr_data.find({})` or equivalent
    - expect: Results are returned in the output panel
    - expect: The alice/eve document from the MongoExpress tests appears (if state is shared)
  3. Execute `db.vr_data.countDocuments({})` and inspect the result
    - expect: Count returns the correct number of documents

### 4. Data Validation and Edge Cases

**Seed:** `src/VacationRelay/VR.TestUI/tests/seed.spec.ts`

#### 4.1. Insert document with idpersoninvacation exactly 50 characters

**File:** `tests/mongodb/13-boundary-50chars.spec.ts`

**Steps:**
  1. In MongoExpress, insert a document into `vr_data` with `idpersoninvacation` set to a 50-character string (e.g. `"ABCDEFGHIJABCDEFGHIJABCDEFGHIJABCDEFGHIJABCDEFGHIJ"`) and a valid `fromdate`
    - expect: Document is accepted and saved (MongoDB has no built-in string length enforcement)
    - expect: `idpersoninvacation` is stored as-is

#### 4.2. Insert document missing required field idpersoninvacation

**File:** `tests/mongodb/14-missing-required-field.spec.ts`

**Steps:**
  1. In MongoExpress, insert a document with only `{ "idpersonreplace": "bob", "fromdate": { "$date": "2026-08-01T00:00:00Z" } }` (omitting `idpersoninvacation`)
    - expect: MongoDB accepts the document (schema validation is not enforced at DB level in this version)
    - expect: The document is stored without `idpersoninvacation`
    - expect: Document is visible in the collection grid
  2. Note this as a finding: application-level validation should enforce required fields before writing to MongoDB
    - expect: Test documents the absence of schema enforcement as a known characteristic

#### 4.3. Insert document where fromdate is after todate

**File:** `tests/mongodb/15-invalid-date-range.spec.ts`

**Steps:**
  1. In MongoExpress, insert: `{ "idpersoninvacation": "frank", "idpersonreplace": "grace", "fromdate": { "$date": "2026-09-30T00:00:00Z" }, "todate": { "$date": "2026-09-01T00:00:00Z" } }`
    - expect: MongoDB stores the document without error (no date-range enforcement at DB level)
  2. Note as a finding: business-logic validation (fromdate < todate) must be enforced at the application layer
    - expect: Test documents the absence of date-range validation as a known characteristic

#### 4.4. Delete all documents from vr_data – verify collection still exists

**File:** `tests/mongodb/16-delete-all.spec.ts`

**Steps:**
  1. In MongoExpress, delete all documents from `vr_data` using the 'Delete All' or bulk delete option
    - expect: All documents are removed
  2. Refresh the collection view
    - expect: Collection `vr_data` still exists with 0 documents
    - expect: The collection itself is NOT dropped

#### 4.5. Re-run ExecuteDBScripts – collections persist with existing data

**File:** `tests/mongodb/17-re-run-scripts.spec.ts`

**Steps:**
  1. Insert one document into `vr_data` via MongoExpress
    - expect: Document is stored successfully
  2. On the Aspire dashboard, trigger the `ExecuteDBScripts` command on the `mongodb` resource again
    - expect: Command completes with `Success: true`
  3. Return to MongoExpress and check `vr_data`
    - expect: The previously inserted document is still present (MongoDB `create` command does not drop existing collections)
    - expect: No data loss occurs

#### 4.6. MongoDB container restart – data persists (persistent lifetime)

**File:** `tests/mongodb/18-persistence.spec.ts`

**Steps:**
  1. Insert a document into `vr_data` via MongoExpress: `{ "idpersoninvacation": "persist-test", "idpersonreplace": "tester", "fromdate": { "$date": "2026-10-01T00:00:00Z" } }`
    - expect: Document is stored
  2. Run `aspire resource mongo stop` (or use the Aspire dashboard Stop action on the `mongo` resource), then run `aspire resource mongo start`
    - expect: Container stops and restarts without errors
  3. After restart, run `aspire wait mongodb`, then navigate to MongoExpress and open `vr_data`
    - expect: The `persist-test` document is still present
    - expect: Both collections `vr_data` and `vr_data_history` still exist
    - expect: Data was persisted because the container uses `WithLifetime(ContainerLifetime.Persistent)`
