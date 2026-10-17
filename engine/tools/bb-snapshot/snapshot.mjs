// Usage: node snapshot.mjs <output.html>
// Loads the Bangladesh Bank "Exchange Rate of Taka" page in headless Chromium, waits until the
// JavaScript challenge has cleared and the USD/BDT table is rendered, then writes the page HTML.
// Exits non-zero if the table never appears, so CI can log the failure and the engine marks
// MidMarket as failed (carrying forward the last good value).
import { chromium } from "playwright";
import { writeFile } from "node:fs/promises";

const url = "https://www.bb.org.bd/en/index.php/econdata/exchangerate";
const output = process.argv[2];
if (!output) {
  console.error("usage: node snapshot.mjs <output.html>");
  process.exit(2);
}

const browser = await chromium.launch({ headless: true });
try {
  const page = await browser.newPage({
    userAgent:
      "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36",
    locale: "en-GB",
  });
  await page.goto(url, { waitUntil: "domcontentloaded", timeout: 60_000 });
  // The challenge page reloads itself once solved; wait for the real rate table.
  await page.waitForSelector("text=USD/BDT Exchange Rate", { timeout: 60_000 });
  await writeFile(output, await page.content(), "utf8");
  console.log(`saved ${url} -> ${output}`);
} catch (err) {
  console.error(`bb-snapshot failed: ${err.message}`);
  process.exitCode = 1;
} finally {
  await browser.close();
}
