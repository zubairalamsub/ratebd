# ratebd

Remittance rate comparison for sending money to Bangladesh (GBP→BDT and AED→BDT).
Everything runs on free tiers: no servers, no database, no VMs.

```
GitHub Actions (cron 03:00 UTC) ──► engine (Native AOT) ──► /data/*.json ──► git push to main
                                                                                  │
                                              Cloudflare Pages builds /site ◄─────┘
```

| Folder | What it is |
|---|---|
| [`engine/`](engine) | .NET 10 console app (C# 14, Native AOT) that fetches quotes from each provider |
| [`data/`](data) | Latest quotes per corridor plus one history file per day, written by the engine and committed by CI |
| [`site/`](site) | Astro 5 + Tailwind 4 static site built from `/data`, deployed on Cloudflare Pages |
| [`.github/workflows/`](.github/workflows) | `ci.yml` (engine build/test/AOT publish), `site.yml` (site type-check/build), `fetch.yml` (daily fetch + commit) |

## Engine

```bash
cd engine
dotnet test --solution RateBd.slnx
```

```bash
dotnet run -c Release --project engine/src/RateBd.Engine
```

The run writes to `data/` at the root of the checkout. Each provider is a separate class implementing
`IRateProvider`. A provider that throws is logged and marked `failed` without stopping the run; its last good
quotes are carried forward with `stale: true`.

### Providers and sources

All sources are public, unauthenticated endpoints that the providers' own websites call. No API keys are needed.
Each parser is tested against real responses saved in `engine/tests/RateBd.Engine.Tests/Fixtures`.

| Provider | Source | GBP→BDT | AED→BDT |
|---|---|---|---|
| Wise | `POST api.wise.com/v3/quotes/`, bank-transfer pay-in | ✅ | Not supported: Wise only offers a disabled balance pay-in |
| Remitly | `GET api.remitly.com/v3/calculator/estimate` with the page-issued device ID | ✅ | ✅ |
| Taptap Send | `GET api.taptapsend.com/api/fxRates` | ✅ | ✅ |
| WorldRemit | `POST api.worldremit.com/graphql` | ✅ | Not supported: the UAE isn't a WorldRemit send country |
| Xe | `POST launchpad-api.xe.com/v2/quotes` | ✅ | Not supported: Xe Money Transfer isn't offered to UAE residents |
| MidMarket (benchmark) | Bangladesh Bank USD/BDT WAR, crossed with [fawazahmed0/currency-api](https://github.com/fawazahmed0/exchange-api) | ✅ | ✅ |

Source notes:
- **Remitly** returns a first-transfer promo price. ratebd shows the standard price instead: `base_rate` with the undiscounted fee.
- **Remitly** answers anonymous calls with HTTP 429 `NOT_ALLOWED` far more often when they lack the `Remitly-DeviceEnvironmentID` header its own site sends. The provider reads that server-issued ID from a public converter page once per run, sends the same headers, paces requests, and retries once with a fresh ID before carrying the last quote forward.
- **Taptap Send** publishes one rate per corridor. Its BD corridors carry no fee schedule, so the fee is 0. It gives no delivery method or speed, and those show as "Not specified".
- **Xe:** every response says *"Automatic extraction of rates is prohibited under the Terms of Use."* Xe is included at the project owner's request. To turn it off, set `Providers:Xe:Enabled=false` (env `RATEBD_Providers__Xe__Enabled=false`).
- **Bangladesh Bank** serves a JavaScript bot challenge to plain HTTP clients. CI saves the page with headless Chromium (`engine/tools/bb-snapshot`, Playwright), and the engine parses that file with AngleSharp. GBP and AED mid rates are `USD/BDT ÷ units-per-USD`.

### How quotes are normalised

- `sendAmount` is the total the sender pays, fee included.
- `receiveAmount = (sendAmount − fee) × rate`. This is recalculated for providers that add the fee on top (Remitly, WorldRemit, Xe, Taptap). Wise already deducts its fee, so its own `targetAmount` is used.
- One quote per provider per amount:
  - Wise uses bank transfer.
  - WorldRemit uses whichever payout method delivers most.
  - Xe uses its default option.
- Missing data is never invented. A field the source doesn't give shows as "Not specified".

### Data files

`data/{corridor}.json` holds the latest snapshot. `data/history/{corridor}/{yyyy-MM-dd}.json` holds one snapshot per
UTC day, and a rerun on the same day overwrites it.

```jsonc
{
  "corridor": "gbp-bdt", "sendCurrency": "GBP", "receiveCurrency": "BDT",
  "generatedAt": "2026-10-17T08:14:07Z",
  "midMarket": { "rate": 164.2248, "usdBdt": 123.45, "usdBdtDate": "2026-10-17", "sendPerUsd": 0.7517, "stale": false, ... },
  "providers": [ { "name": "Wise", "status": "ok|failed|stale|not_supported", "message": null }, ... ],
  "quotes": [ { "provider": "Wise", "sendAmount": 1000, "rate": 163.916, "fee": 8.86, "receiveAmount": 162463.70,
                "deliveryMethod": "Bank deposit", "speed": "Within 2 days", "fetchedAt": "...", "stale": false }, ... ]
}
```

### Configuration

Configuration comes from `engine/src/RateBd.Engine/appsettings.json`. Environment variables override it, either
with the `RATEBD_` prefix or unprefixed, using `__` for nesting:

| Setting | Default | Purpose |
|---|---|---|
| `Engine:DataDirectory` | `data/` at the git root | Output folder |
| `Engine:SendAmounts` | `[500, 1000]` | Amounts quoted per corridor |
| `Engine:Corridors` | GBP→BDT, AED→BDT | Corridor list |
| `Engine:ProviderTimeoutSeconds` | `90` | Cap per provider per corridor, retries included |
| `Engine:FailWhenNothingFetched` | `true` | Exit code 2 if no provider returned a fresh quote |
| `Providers:{Name}:Enabled` | `true` | Turn a provider off |
| `Providers:{Name}:ApiKey` | empty | Reserved; no v1 provider needs one |
| `Providers:MidMarket:BangladeshBankHtmlPath` | empty | HTML saved by `tools/bb-snapshot` |

HTTP calls go through `Microsoft.Extensions.Http.Resilience` (Polly v8). Each call gets 3 retries with jittered
exponential backoff on 5xx, 408 and 429 (honouring `Retry-After`), a 15 s timeout per attempt, and a 60 s total.

## Site

```bash
cd site
npm install
npm run dev
```

`npm run build` writes `site/dist`. The build reads `../data`, so run the engine once first.

The site has these pages. Every page also exists in Bengali under `/bn/...`.
- `/`
- `/send-money-gbp-to-bangladesh` and `/send-money-aed-to-bangladesh`
- `/providers/{wise|remitly|taptap-send|worldremit|xe}`
- `/how-rates-work`
- `/government-incentive`

What the site does:
- **Corridor table:** sorted by amount received. Columns are rate, fee, you receive, margin vs mid-market, delivery, speed and an inline-SVG 7-day sparkline. Rows show a stale badge when a quote was carried forward.
- **Amount switch:** CSS only (`:has()`). The site ships no client-side JavaScript.
- **SEO:** sitemap (`@astrojs/sitemap`, with hreflang), canonical and OpenGraph tags, and JSON-LD `Table` and `FAQPage`.
- **Lighthouse (mobile, local preview):** 100 for performance, accessibility, best practices and SEO on the home, corridor, provider and methodology pages, in both languages.
- **Affiliate redirects:** [`site/src/config/providers.json`](site/src/config/providers.json) lists each provider's official `homepage` and an `affiliateUrl` placeholder (`null`). At build time `/go/{slug}` becomes a 302 in `dist/_redirects` to the affiliate URL when set, otherwise to the homepage. Put real affiliate links there only once you have them from the provider's programme.
- **Bengali text:** the Bengali UI strings in `site/src/i18n/ui.ts` and the methodology page were written without native review. Have a native speaker check them before launch.

### Deploying on Cloudflare Pages (free plan)

1. In Cloudflare: **Workers & Pages → Create → Pages → Connect to Git**, then pick this repository.
2. Build settings:

   | Setting | Value |
   |---|---|
   | Framework preset | Astro |
   | Root directory | `site` |
   | Build command | `npm run build` |
   | Build output | `dist` |

   [`site/wrangler.toml`](site/wrangler.toml) also sets `pages_build_output_dir`. Node comes from `site/.nvmrc` (22).
3. Optional: set `SITE_URL` to your custom domain. Without it, canonical URLs use `https://ratebd.pages.dev`.
4. Optional: under **Settings → Builds → Build watch paths**, include `site/*` and `data/*`. Engine-only commits then don't use a build.

Cloudflare builds every push to `main`. That includes the daily `data: rates …` commit from `fetch.yml`, so the site refreshes once a day without any deploy token in GitHub.

## Free-tier guardrails

### GitHub Actions

| Workflow | When | Measured duration | Minutes per month |
|---|---|---|---|
| `fetch.yml` | daily 03:00 UTC + manual | 1 min 12 s on a cold cache, including the 29 s AOT publish (run #2, 17 Oct 2026). About 40 s when the cached binary is reused. Bangladesh Bank snapshot ~19 s, fetching all providers ~8 s | ~30–60 (GitHub rounds each job up to a whole minute) |
| `ci.yml` | pushes touching `engine/**` | ~1 min | a few |
| `site.yml` | pushes touching `site/**` | ~1 min | a few |

- **This repository is public,** so standard GitHub-hosted runners cost nothing and have no minute cap.
- **If you make it private,** the Free plan includes 2,000 Linux minutes a month. Daily fetches use under 5% of that.
- **Speed tricks in `fetch.yml`:**
  - The AOT binary is cached by source hash plus month, so on most days no .NET SDK is installed.
  - The Playwright browser and npm packages are cached.
  - `timeout-minutes: 10` caps a hung run.

**If Actions minutes run out** (private repo only), or you want to use fewer:
- **What happens:** scheduled runs stop until the next billing cycle. The site stays up with the last data. No rebuild happens, so stale badges do not appear; only the "Last updated" date shows how old the data is.
- Reduce the cron, for example `0 3 * * 1,4` for twice a week.
- Drop the Bangladesh Bank snapshot step, the slowest step. MidMarket then carries its last value forward as stale.
- Add a spending limit of $0 (the default) so you're never billed.

GitHub disables scheduled workflows in public repos after 60 days without repository activity. The daily data commit counts as activity, but if fetches stop committing, re-enable the workflow from the Actions tab.

### Cloudflare Pages (free plan)

| Limit | Free plan | ratebd usage |
|---|---|---|
| Builds | 500 / month, 1 concurrent | ~31 daily data builds + your pushes |
| Build timeout | 20 min | ~1 min (`npm ci` + `astro build`) |
| Files per deployment | 20,000 | ~30 |
| File size | 25 MiB | largest is the 40 KB OG image |
| `_redirects` | 2,000 static + 100 dynamic | 5 |

**If you hit the 500-builds limit:**
- **What happens:** new builds are refused until the month resets. The last successful deployment keeps serving.
- Set build watch paths (step 4 above) so engine and CI-only commits don't trigger builds.
- Add `[CF-Pages-skip]` to a commit message to skip that one build.
- Batch local commits before pushing.
- As a last resort, deploy manually with `npx wrangler pages deploy site/dist` from your machine. This needs a Cloudflare login, not a GitHub secret.

### Third-party sources

Every source is free and keyless. They can still change or block automated requests:
- **Failing provider:** it shows as `stale` (last good quote) or `failed` in the data and on the site. The run continues.
- **Remitly** answered `429` to GitHub-hosted runners until the provider started sending the page-issued device ID; the first run with it (17 Oct 2026) got fresh quotes on both corridors. See its provider notes.
- **Bangladesh Bank** needs the headless-browser snapshot. If that fails, the benchmark is carried forward as stale.
- **Alerts:** check the job summary of each `fetch` run, which has a status table per corridor.
