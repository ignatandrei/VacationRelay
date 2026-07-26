import { test } from 'playwright/test';

test('seed', async ({ page }) => {
  await page.goto('https://vacationrelay.dev.localhost:17071/login?t=e1dfab69a928830bb11c9a4af2a17219');
});
