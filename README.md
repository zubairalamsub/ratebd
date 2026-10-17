# ratebd

Remittance rate comparison for sending money to Bangladesh from the UK (GBP), the USA (USD), Italy (EUR), the UAE (AED),
Saudi Arabia (SAR), Kuwait (KWD), Qatar (QAR), Oman (OMR), Malaysia (MYR) and Singapore (SGD).

**Live site:** https://ratebd.pages.dev (Cloudflare Pages, rebuilt on every push to `main`).
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
`IRateProvider`, or `IPostedRateProvider` for exchange houses that publish a rate but no fee (see below). A provider
that throws is logged and marked `failed` without stopping the run; its last good quotes are carried forward with
`stale: true`. Providers collected only from some countries implement `ISendCountryScoped` and are skipped elsewhere,
with no status entry.

### Providers and sources

All sources are public, unauthenticated endpoints that the providers' own websites call. No API keys are needed.
Each parser is tested against real responses saved in `engine/tests/RateBd.Engine.Tests/Fixtures`.

| Provider | Source |
|---|---|
| Wise | `POST api.wise.com/v3/quotes/`, bank-transfer pay-in |
| Remitly | `GET api.remitly.com/v3/calculator/estimate` with the page-issued device ID; bank deposit where offered |
| Taptap Send | `GET api.taptapsend.com/api/fxRates` (one feed for every corridor) |
| WorldRemit | `POST api.worldremit.com/graphql` |
| Western Union (SA, KW, QA) | `POST westernunion.com/wuconnect/prices/catalog`, the send-money form's price catalogue; bank deposit |
| Al Mulla Exchange (KW) | `POST app-kwt.almullaexchange.com/pub/xtrate/get`; best correspondent route (often Western Union's network, at Western Union's price) |
| Skrill (KW) | `POST transfers.skrill.com/api/transfers/v4/marketing/preview` after the calculator's anonymous session |
| Paysend (KW) | Rate and fee tariff embedded in `paysend.com/en/send-money/from-kuwait-to-bangladesh` |
| MidMarket (benchmark) | Bangladesh Bank USD/BDT WAR, crossed with [fawazahmed0/currency-api](https://github.com/fawazahmed0/exchange-api) |

**Posted rates (no fee published).** Many Gulf exchange houses publish a rate but not their fee, so there is no total
cost to rank. They are stored as `postedRates` and shown in a separate list under each route's table, best rate first:

| Exchange house | Country | Source |
|---|---|---|
| Ooredoo Money | QA | `POST mmapi.ooredoo.qa:4443/…/getIRDeliveryOptionList`: one rate per partner (MoneyGram, Western Union, Al Zaman, …) with a fee range |
| Gulf Exchange | QA | `POST gulfexchange.com.qa/en/convert-currency` with the home page's session and CSRF token |
| Al Zaman Exchange | QA | `GET alzamanexchange.com/api/getRate.php?cid=6` (TT rate) |
| Habib Qatar International Exchange | QA | `GET hqie.com.qa/Account/ConvertCurr` |
| Al Mirqab Exchange | QA | `GET almirqabexchange.com/view_rates.jsp` (DRAFT/TT column) |
| Unimoni | OM | `POST unimoni.om/crm/live/rate` |
| Purshottam Kanji Exchange | OM | `GET pkexchange.com.om/rates/` (TT column, dated page) |
| Joyalukkas Exchange | KW, OM | `GET admin.joyalukkasexchange.com/api/currency-converter?region=1\|3` (TT rate) |
| Al Ansari Exchange | KW | `alansariexchange.com.kw` converter (`admin-ajax.php`, page nonce) |
| Aman Exchange | KW | `GET portal.amankuwait.com/amsweb/api/rate` (dated board) |

A posted rate that its source dates more than three days back counts as a failure, so the last good rate stays on the
site marked stale instead of being shown as current. Ooredoo Money's fee range ("Starting from QAR 20") is kept as
`feeNote` and shown in place of "Not published".

Coverage, verified against each provider's own responses on 2026-10-17. ✗ means the provider's data shows it does not
serve that route; the site lists it as "not offered".

| Provider | GBP (UK) | USD (USA) | EUR (Italy) | AED (UAE) | MYR (Malaysia) | SGD (Singapore) |
|---|---|---|---|---|---|---|
| Wise | ✅ | ✅ | ✅ | ✗ only a disabled balance pay-in | ✅ | ✅ |
| Remitly | ✅ | ✅ | ✅ | ✅ | ✗ not a send country | ✅ |
| Taptap Send | ✅ | ✅ | ✅ | ✅ | ✗ | ✗ |
| WorldRemit | ✅ | ✅ | ✅ | ✗ | ✅ | ✗ |
| MidMarket | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

None of those four providers sends from Saudi Arabia, Kuwait, Qatar or Oman: Wise returns 422 `route.not.supported`,
Remitly returns 400 for those conduits, Taptap Send doesn't list them, and WorldRemit offers only airtime top-up.
The Gulf routes therefore use Gulf providers, researched and re-verified on 2026-10-17:

| Route | Full quotes (ranked) | Posted rates (no fee) |
|---|---|---|
| SAR (Saudi Arabia) | Western Union | – |
| KWD (Kuwait) | Western Union, Al Mulla, Skrill, Paysend | Joyalukkas, Al Ansari, Aman |
| QAR (Qatar) | Western Union | Ooredoo Money (8 partners), Gulf Exchange, Al Zaman, Habib Qatar, Al Mirqab |
| OMR (Oman) | – | Unimoni, PK Exchange, Joyalukkas |

Checked and left out:
- **App or login only:** Al Rajhi (Tahweel, urpay), STC Bank, Enjaz, ANB TeleMoney, LuLu Money, Global Money Exchange and most Gulf banks.
- **No public quote source:** MoneyGram and Ria have no Gulf calculator.
- **Terms forbid it:** Alfardan Exchange (robots.txt and terms ban automated access), Al Dar Exchange (terms bar storing site content) and BEC Kuwait (content for internal, non-commercial use only). Al Dar still appears as an Ooredoo Money partner.
- **Blocks non-browser clients:** KIECO (403 to .NET's HTTP client); no workaround is attempted.
- **Not reliable enough:** Musandam Exchange (TLS certificate expired 2026-10-05) and Wasel Exchange (hand-edited branch boards).

Source notes:
- **Remitly** returns a first-transfer promo price. ratebd shows the standard price instead: `base_rate` with the undiscounted fee.
- **Remitly** answers anonymous calls with HTTP 429 `NOT_ALLOWED` far more often when they lack the `Remitly-DeviceEnvironmentID` header its own site sends. The provider reads that server-issued ID from a public converter page once per run, sends the same headers, paces requests, and retries once with a fresh ID before carrying the last quote forward.
- **Taptap Send** publishes one rate per corridor. Its BD corridors carry no fee schedule, so the fee is 0. It gives no delivery method or speed, and those show as "Not specified".
- **Xe** is not collected: its quote responses say automatic extraction of rates is prohibited under its Terms of Use. It was removed on 2026-10-17.
- **Western Union** prices a request from a random `wu_device_id` cookie that its page script generates in the browser (no server involvement). Without one the API answers from a different price tier that the site never shows, so the provider sends a fresh random ID each run, exactly as a browser visit would, and rejects any response outside the website's price segment (`N00`). westernunion.com uses Akamai bot management; a 403 is reported as a failure and the last quote is carried forward.
- **Skrill** opens the same anonymous session its calculator does (three GETs, no login) and echoes its `XSRF-TOKEN`. Its first-transfer discount is ignored.
- **Bangladesh Bank** serves a JavaScript bot challenge to plain HTTP clients. CI saves the page with headless Chromium (`engine/tools/bb-snapshot`, Playwright), and the engine parses that file with AngleSharp. GBP and AED mid rates are `USD/BDT ÷ units-per-USD`.

### How quotes are normalised

- `sendAmount` is the total the sender pays, fee included.
- `receiveAmount = (sendAmount − fee) × rate`. This is recalculated for providers that add the fee on top (Remitly, WorldRemit, Taptap, Western Union, Al Mulla, Skrill). Wise and Paysend already deduct their fee from the amount sent.
- One quote per provider per amount:
  - Wise uses bank transfer.
  - WorldRemit uses whichever payout method delivers most.
  - Western Union and Skrill use bank deposit; Al Mulla its best route.
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
                "deliveryMethod": "Bank deposit", "speed": "Within 2 days", "fetchedAt": "...", "stale": false }, ... ],
  "postedRateProviders": [ { "name": "Unimoni", "status": "ok", "message": null }, ... ],
  "postedRates": [ { "provider": "OoredooMoney", "rate": 33.900144, "deliveryMethod": "Bank deposit", "speed": "Same Day",
                     "partner": "Alzaman Exchange", "feeNote": "Starting from QAR 20", "publishedAt": "...",
                     "fetchedAt": "...", "stale": false }, ... ]
}
```

Snapshots written before posted rates existed have neither `postedRate…` list; the engine and site read them as empty.

### Configuration

Configuration comes from `engine/src/RateBd.Engine/appsettings.json`. Environment variables override it, either
with the `RATEBD_` prefix or unprefixed, using `__` for nesting:

| Setting | Default | Purpose |
|---|---|---|
| `Engine:DataDirectory` | `data/` at the git root | Output folder |
| `Engine:SendAmounts` | `[500, 1000]` | Default amounts per corridor |
| `Engine:Corridors` | GBP, USD, EUR (IT), AED, SAR, KWD, QAR, OMR, MYR, SGD → BDT | Corridor list; each can set its own `SendAmounts` (MYR, SAR and QAR use `[1000, 2000]`, KWD and OMR `[100, 200]`) |
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
- `/send-money-{gbp|usd|eur|aed|sar|kwd|qar|omr|myr|sgd}-to-bangladesh`
- `/providers/{slug}` for every provider in `site/src/config/providers.json`
- `/how-rates-work`
- `/government-incentive`

What the site does:
- **Corridor table:** sorted by amount received. Columns are rate, fee, you receive, margin vs mid-market, delivery, speed and an inline-SVG 7-day sparkline. Rows show a stale badge when a quote was carried forward.
- **Exchange-house rates:** posted rates without a fee get their own list under the table, best rate first, with the fee shown as "Not published" (or the source's own fee wording). They are never ranked against full quotes. Oman, where no provider publishes a full quote, shows only this list.
- **Phones and tablets:** below 1024 px each provider is a card (amount received first, then rate, fee and margin) instead of a table row, so nothing needs sideways scrolling. Corridor pages open with the day's best deal and the benchmark; the country picker, the "not offered" list and the FAQ are native `<details>` disclosures.
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
   | Framework preset | None (the settings below are all that is needed) |
   | Root directory | `site` |
   | Build command | `npm run build` |
   | Build output | `dist` |

   [`site/wrangler.toml`](site/wrangler.toml) also sets `pages_build_output_dir`. The live project also sets the environment variable `NODE_VERSION=22`, matching `site/.nvmrc`.
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
| Files per deployment | 20,000 | ~75 |
| File size | 25 MiB | largest is the 40 KB OG image |
| `_redirects` | 2,000 static + 100 dynamic | 19 |

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
- **Gulf sources** were verified from a machine outside the Gulf; the first scheduled runs show whether any of them treat GitHub's runners differently. Western Union is the most likely to (Akamai).
- **Alerts:** check the job summary of each `fetch` run, which has a status table per corridor.
