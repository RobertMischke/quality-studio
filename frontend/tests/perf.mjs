import { chromium } from 'playwright-core';

const executablePath = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const browser = await chromium.launch({ executablePath, headless: true });
const page = await browser.newPage({ viewport: { width: 1600, height: 1000 } });
const events = [];
page.on('console', message => {
  try {
    const event = JSON.parse(message.text());
    if (event.event?.startsWith('qs.')) events.push(event);
  } catch { /* Browser diagnostics that are not structured app events. */ }
});

// Exercise the worst supported payload while keeping transport out of the scripting measurement.
const payload = Array.from({ length: 6000 }, (_, i) => `${i + 1}: public static string ReviewLine${i} => "quality";`).join('\n');
await page.route('**/api/file**', route => route.fulfill({
  contentType: 'application/json',
  body: JSON.stringify({ path: 'src/QualityStudio.Api/ApiContracts.cs', content: payload, metaDocuments: [] }),
}));
await page.goto(process.env.QS_URL ?? 'http://127.0.0.1:4200/?theme=dark');
await page.getByRole('button', { name: /Quality Studio/ }).click();
await page.getByRole('button', { name: /Quality Studio/ }).click();
await page.getByRole('button', { name: /ApiContracts.cs/ }).click();
await page.waitForFunction(() => performance.getEntriesByName('qs.file.first-content').length >= 1);

const measures = await page.evaluate(() => performance.getEntriesByType('measure').map(entry => ({
  name: entry.name,
  durationMs: Number(entry.duration.toFixed(2)),
})));
const result = { measuredAt: new Date().toISOString(), browser: await browser.version(), payloadBytes: Buffer.byteLength(payload), measures, events };
console.log(JSON.stringify(result, null, 2));
await browser.close();

if (measures.some(item => item.name === 'qs.tree.toggle' && item.durationMs >= 50) ||
    measures.some(item => item.name === 'qs.file.first-content' && item.durationMs >= 150)) process.exitCode = 1;
