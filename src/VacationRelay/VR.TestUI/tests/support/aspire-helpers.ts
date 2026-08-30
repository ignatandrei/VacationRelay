/**
 * Aspire admin UI URL discovery helpers.
 *
 * Start Aspire first:  cd VacationRelay && dotnet run
 * Then set env vars from the Aspire resource service proxy ports:
 *   ADMINER_URL, PGWEB_URL, MONGO_EXPRESS_URL, SQLITEWEB_URL
 *
 * ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL defaults to https://localhost:22202
 */

import type { Page, Screencast } from '@playwright/test';

export async function navigateToAspire(page: Page) {
  var login = process.env.ASPIRE_LOGIN_URL;
  await page.goto(login, { waitUntil: 'networkidle' });
  var aspireDashboard = process.env.ASPIRE_BASE_URL;
  await page.goto(aspireDashboard, { waitUntil: 'networkidle' });

  ;
}
export function getAdminerUrl(): string {
  const url = process.env.PORT_sqlserver;
  if (!url) {
    throw new Error(
      'ADMINER_URL is not set. Start Aspire (dotnet run in VacationRelay/) then ' +
      'set ADMINER_URL to the adminer proxy URL shown in the Aspire dashboard.'
    );
  }
  return `http://localhost:${url}`;
}

export function getPgWebUrl(): string {
  const url = process.env.PORT_postgres;
  if (!url) {
    throw new Error(
      'PGWEB_URL is not set. Start Aspire then set PGWEB_URL to the pgweb proxy URL.'
    );
  }
  return `http://localhost:${url}`;
}

export function getMongoExpressUrl(): string {
  const url = process.env.PORT_mongodb;
  if (!url) {
    throw new Error(
      'PORT_mongodb is not set. Start Aspire before running this test.'
    );
  }
  console.log(`Mongo Express URL: http://localhost:${url}`);
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

import type { Response } from '@playwright/test';
import { sleep } from './playwright-helpers';

export async  function navigateToSourceCode(page: Page,cast: Screencast,  name: string): Promise<Response|null> {
  
  var port = process.env.PORT_FileDisplay;
  if (!port) {
    throw new Error(
      'PORT_FileDisplay is not set. Start Aspire before running this test'
    );
  }
  await sleep(2);
  await cast.showChapter(`The code for ${name}`, {
    description: `Viewing the source code for ${name}`,
    duration: 5000,
  });
  await sleep(2);

  var res=await  page.goto(`http://127.0.0.1:${port}/files/${name}`, { waitUntil: 'networkidle' });
  await sleep(2);

  return res;
}


export const DB_CREDENTIALS = {
  server:   process.env.SQLSERVER_HOST     ?? 'localhost',
  pgHost:   process.env.POSTGRES_HOST      ?? 'localhost',
  username: 'sa',
  password: process.env.DB_PASSWORD        ?? 'myPa!ssW0rd',
  database: 'vacationrelay',
} as const;
