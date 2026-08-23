# VacationRelay – Admin UI Database Creation Verification

## Application Overview

Verifies that each database (SQL Server, PostgreSQL, MongoDB, SQLite) is created and initialized by Aspire AppHost startup scripts, by navigating to one chosen admin UI per database type and asserting the `vacationrelay` database (and expected tables/collections) are visible. Aspire must be running before tests execute (`dotnet run` in VacationRelay/). Admin UI proxy URLs are passed via environment variables: ADMINER_URL, PGWEB_URL, MONGO_EXPRESS_URL, SQLITEWEB_URL (discoverable from the Aspire resource service at ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL=https://localhost:22202).

## Test Scenarios

### 1. SQL Server – Adminer

**Seed:** `tests/seed-adminer.spec.ts`

#### 1.1. SQL Server vacationrelay database is visible and tables exist in Adminer

**File:** `tests/sqlserver-adminer.spec.ts`

**Steps:**
  1. Navigate to the Adminer URL (read from env var ADMINER_URL, fallback discovery from Aspire resource service)
    - expect: Adminer login page loads with fields: Server, Username, Password, Database, System dropdown
  2. Set System dropdown to 'MS SQL (beta)' or 'MSSQL'
    - expect: System field shows SQL Server option selected
  3. Fill Server field with the SQL Server host (sqlserver or the Aspire container hostname)
    - expect: Server field is filled
  4. Fill Username with 'sa'
    - expect: Username field shows 'sa'
  5. Fill Password with 'myPa!ssW0rd'
    - expect: Password field is filled
  6. Leave Database field empty (to see all databases) and click the Login button
    - expect: Adminer navigates to the database list page showing available databases
  7. Locate 'vacationrelay' in the database list
    - expect: A link or entry for 'vacationrelay' is visible on the page
  8. Click the 'vacationrelay' database link
    - expect: Adminer navigates into the vacationrelay database and shows the table list
  9. Verify that table 'vr_data' is present in the table list
    - expect: The text 'vr_data' appears in the tables section
  10. Verify that table 'vr_data_history' is present in the table list
    - expect: The text 'vr_data_history' appears in the tables section

#### 1.2. Adminer login fails gracefully with wrong credentials

**File:** `tests/sqlserver-adminer.spec.ts`

**Steps:**
  1. Navigate to the Adminer URL
    - expect: Adminer login page loads
  2. Fill credentials with wrong password 'wrongpassword' and click Login
    - expect: Adminer displays an error message indicating invalid login – the page does NOT proceed to the database list

### 2. PostgreSQL – PgWeb

**Seed:** `tests/seed-pgweb.spec.ts`

#### 2.1. PostgreSQL vacationrelay database is listed and tables exist in PgWeb

**File:** `tests/postgres-pgweb.spec.ts`

**Steps:**
  1. Navigate to the PgWeb URL (read from env var PGWEB_URL)
    - expect: PgWeb loads, either showing a connection form or auto-connected to the postgres instance
  2. If a connection form is shown, fill Host (postgres Aspire hostname), User (sa), Password (myPa!ssW0rd), Database (vacationrelay) and click Connect
    - expect: PgWeb connects and shows the database explorer sidebar
  3. Verify 'vacationrelay' appears in the database selector or sidebar header
    - expect: The database name 'vacationrelay' is visible in the navigation area
  4. Expand the tables list in the left sidebar
    - expect: The sidebar shows tables belonging to the vacationrelay database
  5. Verify that 'vr_data' appears as a table
    - expect: The text 'vr_data' is visible in the sidebar table list
  6. Verify that 'vr_data_history' appears as a table
    - expect: The text 'vr_data_history' is visible in the sidebar table list
  7. Click on table 'vr_data' to open it
    - expect: PgWeb shows the table content panel with a grid or empty result – no error is shown

#### 2.2. PgWeb shows error when connecting to a non-existent database

**File:** `tests/postgres-pgweb.spec.ts`

**Steps:**
  1. Navigate to PgWeb URL and attempt to connect to database name 'doesnotexist'
    - expect: PgWeb shows a connection error or failure message – it does NOT show a table list

### 3. MongoDB – Mongo Express

**Seed:** `tests/seed-mongo-express.spec.ts`

#### 3.1. MongoDB vacationrelay database is visible in Mongo Express and vr_data collection exists

**File:** `tests/mongodb-mongo-express.spec.ts`

**Steps:**
  1. Navigate to the Mongo Express URL (read from env var MONGO_EXPRESS_URL)
    - expect: Mongo Express home page loads showing a list of databases in the main panel
  2. Wait until the database list is fully rendered (not loading spinner)
    - expect: Database rows are visible in the list; a 'View' button or database name link is available per row
  3. Locate a row for the 'vacationrelay' database
    - expect: A row with the text 'vacationrelay' is present in the database table
  4. Click the 'View' button or the 'vacationrelay' link for that database
    - expect: Mongo Express navigates to the vacationrelay database page showing its collections
  5. Verify that collection 'vr_data' is listed on the collections page
    - expect: The text 'vr_data' appears as a collection name in the list
  6. Click 'View' or the 'vr_data' link for the collection
    - expect: Mongo Express opens the collection view page – no error is displayed; document count or empty collection message is shown

#### 3.2. Mongo Express does not show non-existent database

**File:** `tests/mongodb-mongo-express.spec.ts`

**Steps:**
  1. Navigate to Mongo Express home page
    - expect: Database list is visible
  2. Verify the text 'nonexistentdb' is NOT present in the database list
    - expect: No row for 'nonexistentdb' is found – the assertion confirms only real databases are shown

### 4. SQLite – SqliteWeb

**Seed:** `tests/seed-sqliteweb.spec.ts`

#### 4.1. SQLite database is accessible and vr_data table exists in SqliteWeb

**File:** `tests/sqlite-sqliteweb.spec.ts`

**Steps:**
  1. Navigate to the SqliteWeb URL (read from env var SQLITEWEB_URL)
    - expect: SqliteWeb loads showing either a table browser or database info page for the SQLite file
  2. Wait for the page to fully load and dismiss any initial loading state
    - expect: The main content area is visible with no error or 'unable to connect' message
  3. Look for the table list in the left sidebar or main panel
    - expect: A sidebar or panel listing the tables in the SQLite database is visible
  4. Verify that 'vr_data' appears in the table list
    - expect: The text 'vr_data' is present in the table listing
  5. Verify that 'vr_data_history' appears in the table list
    - expect: The text 'vr_data_history' is present in the table listing
  6. Click on 'vr_data' table to open it
    - expect: SqliteWeb shows the table content panel with column headers visible (no error) – table schema matches expected structure
  7. Execute a simple query via the SQL console (if available): SELECT count(*) FROM vr_data
    - expect: Query executes without error; result row is shown (count may be 0 for a fresh database)

#### 4.2. SqliteWeb shows correct schema for vr_data table

**File:** `tests/sqlite-sqliteweb.spec.ts`

**Steps:**
  1. Navigate to SqliteWeb and open the 'vr_data' table structure view
    - expect: Table structure page or schema tab is visible
  2. Verify that an 'id' column is present in the schema
    - expect: Column 'id' (randomblob-based primary key) is listed in the schema
  3. Verify the table has no unexpected missing columns that would indicate an incomplete migration script
    - expect: All expected columns as defined in SQLite01.createTables.gen.txt are present
