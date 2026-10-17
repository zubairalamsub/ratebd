# ratebd

Remittance rate comparison for sending money to Bangladesh (GBP→BDT, AED→BDT).
Runs entirely on free tiers: a daily GitHub Actions job fetches quotes and commits JSON to `/data`,
and a static site on Cloudflare Pages is rebuilt from that data.

- `/engine` — .NET 10 Native AOT console app that fetches quotes.
- `/data` — latest and historical quotes as JSON (written by the engine, committed by CI).
- `/site` — Astro static site (coming in part 2).
