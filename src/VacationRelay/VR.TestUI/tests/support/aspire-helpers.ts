/**
 * Aspire admin UI URL discovery helpers.
 *
 * Start Aspire first:  cd VacationRelay && dotnet run
 * Then set env vars from the Aspire resource service proxy ports:
 *   ADMINER_URL, PGWEB_URL, MONGO_EXPRESS_URL, SQLITEWEB_URL
 *
 * ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL defaults to https://localhost:22202
 */

import type { Page } from '@playwright/test';

export async function navigateToAspire(page: Page) {
  var login = process.env.ASPIRE_LOGIN_URL;
  await page.goto(login, { waitUntil: 'networkidle' });
  var aspireDashboard = process.env.ASPIRE_BASE_URL;
  await page.goto(aspireDashboard, { waitUntil: 'networkidle' });

  ;
}
export function getAdminerUrl(): string {
  const url = process.env.ADMINER_URL;
  if (!url) {
    throw new Error(
      'ADMINER_URL is not set. Start Aspire (dotnet run in VacationRelay/) then ' +
      'set ADMINER_URL to the adminer proxy URL shown in the Aspire dashboard.'
    );
  }
  return url;
}

export function getPgWebUrl(): string {
  const url = process.env.PGWEB_URL;
  if (!url) {
    throw new Error(
      'PGWEB_URL is not set. Start Aspire then set PGWEB_URL to the pgweb proxy URL.'
    );
  }
  return url;
}

export function getMongoExpressUrl(): string {
  const url = process.env.PORT_mongodb;
  if (!url) {
    throw new Error(
      'PORT_mongodb is not set. Start Aspire before running this test.'
    );
  }
  return `http://localhost:${url}`;
}

export function getSqliteWebUrl(): string {
  const url = process.env.PORT_sqliteweb;
  if (!url) {
    throw new Error(
      'PORT_sqliteweb is not set. Start Aspire before running this test'
    );
  }
  return `http://localhost:${url}`;
}

export const DB_CREDENTIALS = {
  server:   process.env.SQLSERVER_HOST     ?? 'localhost',
  pgHost:   process.env.POSTGRES_HOST      ?? 'localhost',
  username: 'sa',
  password: process.env.DB_PASSWORD        ?? 'myPa!ssW0rd',
  database: 'vacationrelay',
} as const;
