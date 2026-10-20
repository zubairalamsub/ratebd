# ratebd

Remittance rate comparison for sending money to Bangladesh. It only collects from sources whose published terms and
robots.txt don't prohibit it, and the site shows a route only once a permitted provider has data for it: today Kuwait
(KWD), Qatar (QAR) and Oman (OMR). Providers for the UAE (AED), the USA (USD) and Singapore (SGD) were added on
2026-10-20; each must first pass the probe from a GitHub-hosted runner (see "Third-party sources" below), and those
routes appear once a permitted provider has data. The engine also tracks Saudi Arabia, the UK, Italy and Malaysia,
which reappear the same way (see "Removed providers" below).

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
| [`.github/workflows/`](.github/workflows) | `ci.yml` (engine build/test/AOT publish), `site.yml` (site type-check/build), `fetch.yml` (daily fetch + commit), `probe.yml` (on-demand provider check from a GitHub runner; commits nothing) |

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

All sources are public, unauthenticated pages or endpoints that the providers' own websites show or call. No API keys
are needed.
Each source's robots.txt and published terms (including customer and user agreements) were checked before it was
added; a source whose terms forbid automated collection is not used. Each parser is tested against real responses saved
in `engine/tests/RateBd.Engine.Tests/Fixtures`.

| Provider | Source |
|---|---|
| Al Mulla Exchange (KW) | `POST app-kwt.almullaexchange.com/pub/xtrate/get`; best correspondent route |
| BA Express USA (US) | `POST www.baexpress.net/WebServices/WebService.asmx/Load_ExchangeRate_And_Fee`; one call per amount, rate plus service fee |
| MidMarket (benchmark) | Bangladesh Bank USD/BDT WAR, crossed with [fawazahmed0/currency-api](https://github.com/fawazahmed0/exchange-api) |

**Posted rates (rate only).** Many exchange houses, in the Gulf and elsewhere, publish a rate but no exact fee next to
it (some list their charges in a separate document, which the engine does not collect), so there is no total cost to
rank. They are stored as `postedRates` and shown in a separate list under each route's table, best rate first:

| Exchange house | Country | Source |
|---|---|---|
| Habib Qatar International Exchange | QA | `GET hqie.com.qa/Account/ConvertCurr` |
| Al Mirqab Exchange | QA | `GET almirqabexchange.com/view_rates.jsp` (DRAFT/TT column) |
| Unimoni | OM | `POST unimoni.om/crm/live/rate` |
| Purshottam Kanji Exchange | OM | `GET pkexchange.com.om/rates/` (TT column, dated page) |
| Aman Exchange | KW | `GET portal.amankuwait.com/amsweb/api/rate` (dated board) |
| GCC Exchange | AE | `GET gccexchange.com/exchangerate/getExchangeRate` (BDT row, dated) |
| Orient Exchange | AE | `GET www.orientexchange.com/Orient/GetExchangeRates` (`rateList`, dated) |
| Lari Exchange | AE | `GET www.lariexchange.com/ExchangeRates?Currency=BDT` (Transfer Money, dated; ~1.9 MB page) |
| Hadi Express Exchange | AE | `GET hadiexchange.ae/` (`data-rate` in the Money Transfer Rate converter) |
| Ahalia Exchange | AE | `GET ahaliaexchange.com/` (inline `cc_data`, Bank Transfer Rate board) |
| Sonali Exchange | US | `GET www.sonaliexchange.com/rates.asp` (`aspRate` field) |
| Mustafa | SG | `GET www.mustafa.com.sg/` ("Draft Rates" block, Bangladesh Taka) |

A posted rate that its source dates more than three days back counts as a failure, so the last good rate stays on the
site marked stale instead of being shown as current. A fee range a source publishes next to its rate is kept as
`feeNote` and shown in place of "Check with provider".

Each route uses providers from its own country. The Kuwait, Qatar and Oman providers were researched and re-verified on
2026-10-17. The UAE, USA and Singapore providers were added on 2026-10-20 and go live only once they pass the GitHub
runner probe (see "Third-party sources"):

| Route | Full quotes (ranked) | Posted rates (rate only) |
|---|---|---|
| GBP (UK) | – (hidden) | – |
| USD (USA) | BA Express | Sonali Exchange |
| EUR (Italy) | – (hidden) | – |
| AED (UAE) | – | GCC Exchange, Orient, Lari, Hadi Express, Ahalia |
| SAR (Saudi Arabia) | – (hidden) | – |
| KWD (Kuwait) | Al Mulla | Aman |
| QAR (Qatar) | – | Habib Qatar, Al Mirqab |
| OMR (Oman) | – | Unimoni, PK Exchange |
| MYR (Malaysia) | – (hidden) | – |
| SGD (Singapore) | – | Mustafa |

For the six Kuwait, Qatar and Oman providers, no restricting clause was found in any readable robots.txt, website terms,
customer terms or app listing (some in-app terms could not be opened).

The eight UAE, USA and Singapore providers were added on 2026-10-20 after the same review of robots.txt, website terms
and customer terms (researched on 2026-10-17; robots.txt re-checked on 2026-10-20). For all eight the test is the
absence of a restricting clause, not an explicit permission. The published terms of GCC, Lari, Hadi Express, Sonali
and BA Express contain no clause restricting automated access, copying or commercial reuse. Orient, Ahalia and Mustafa
publish no website terms of use: Orient and Ahalia publish customer terms with no restricting clause, and Mustafa,
whose remittances are counter-only, publishes none.

Checked and left out:
- **App or login only:** Al Rajhi (Tahweel, urpay), STC Bank, Enjaz, ANB TeleMoney, LuLu Money, Global Money Exchange and most Gulf banks.
- **No public quote source:** MoneyGram and Ria have no Gulf calculator.
- **Terms forbid it:** Alfardan Exchange (robots.txt and terms ban automated access), Al Dar Exchange (terms bar storing site content) and BEC Kuwait (content for internal, non-commercial use only).
- **Blocks non-browser clients:** KIECO (403 to .NET's HTTP client); no workaround is attempted.
- **Not reliable enough:** Musandam Exchange (TLS certificate expired 2026-10-05) and Wasel Exchange (hand-edited branch boards).
- **Restricting clauses on the other routes (researched 2026-10-17):** Ria, MoneyGram, ACE, Sendwave, Xoom, TransferGo, Instarem, Revolut, NALA, LemFi, Panda Remit, Profee, BOSS Money and Pangea all restrict automated collection, copying or commercial use, as do the UK's BRAC Saajan, Sonali Pay UK and B A Exchange.
- **Ambiguous, left out unless the owner decides otherwise:** Sharaf Exchange and Index Exchange (UAE) and Max Money (Malaysia). Janata Exchange (Italy) has no restricting clause, but its last posted rate is from June 2026, so it would always be stale.

### Removed providers

Removed on 2026-10-17 after a terms review of robots.txt, website terms and customer/user agreements. Each can return
only with the provider's written permission or through an official API (for example an affiliate programme). The first
five are in git history up to commit `577d86b`, the Gulf ones up to `6a0fea5`.

| Provider | Why |
|---|---|
| Xe | Its quote responses state that automatic extraction of rates is prohibited under its Terms of Use |
| Remitly | User agreements ban robots, spiders and scrapers; `api.remitly.com/robots.txt` disallows everything |
| Taptap Send | User agreements ban robots, spiders and scrapers and public or commercial reuse without permission |
| WorldRemit | Terms ban monitoring or copying its channels with robots or automatic devices without written permission |
| Wise | Ambiguous: its quote endpoint is publicly documented, but the customer agreement bans robots monitoring its websites and commercial use of its materials without permission. It can return through Wise's affiliate API with a token |
| Western Union | Online terms for Saudi Arabia, Kuwait and Qatar (clause 10(a)) ban robots, spiders, scrapers and other automated access |
| Skrill | robots.txt disallows the calculator's `?redirect` session URL, and `sso.skrill.com/robots.txt` errors (treated as disallow-all) |
| Joyalukkas Exchange | Customer terms forbid automated systems and robots, copying, data collection and commercial use |
| Gulf Exchange | Terms of use bar using site material in other electronic publications without written permission |
| Al Ansari Exchange | The group terms its Kuwait site points to ban automated scraping and republishing without written permission |
| Ooredoo Money | No element of the website may be copied or distributed without permission |
| Al Zaman Exchange | The terms on the host it serves rates from have a "No Data Mining" clause |
| Paysend | Owns all website content and bans reverse-engineering; ambiguous, removed to be safe |

Source notes:
- **Bangladesh Bank** serves a JavaScript bot challenge to plain HTTP clients. CI saves the page with headless Chromium (`engine/tools/bb-snapshot`, Playwright), and the engine parses that file with AngleSharp. Mid rates are `USD/BDT ÷ units-per-USD` for each send currency.

### How quotes are normalised

- `sendAmount` is the total the sender pays, fee included.
- `receiveAmount = (sendAmount − fee) × rate`. This is recalculated for providers that add the fee on top (Al Mulla,
  BA Express).
- One quote per provider per amount:
  - Al Mulla uses its best route.
  - BA Express is asked once per amount, with a 2 s pause between calls; its service fee and handling fee are added
    together.
- Missing data is never invented. A field the source doesn't give shows as "Not specified".

### Data files

`data/{corridor}.json` holds the latest snapshot. `data/history/{corridor}/{yyyy-MM-dd}.json` holds one snapshot per
UTC day, and a rerun on the same day overwrites it.

```jsonc
{
  "corridor": "kwd-bdt", "sendCurrency": "KWD", "receiveCurrency": "BDT",
  "generatedAt": "2026-10-17T08:14:07Z",
  "midMarket": { "rate": 397.138, "usdBdt": 123.45, "usdBdtDate": "2026-10-17", "sendPerUsd": 0.3108, "stale": false, ... },
  "providers": [ { "name": "AlMulla", "status": "ok|failed|stale|not_supported", "message": null }, ... ],
  "quotes": [ { "provider": "AlMulla", "sendAmount": 200, "rate": 400.8760545, "fee": 1.25, "receiveAmount": 79674.12,
                "deliveryMethod": "Bank deposit", "speed": "Within 2 days", "fetchedAt": "...", "stale": false }, ... ],
  "postedRateProviders": [ { "name": "Unimoni", "status": "ok", "message": null }, ... ],
  "postedRates": [ { "provider": "Unimoni", "rate": 319.9995, "deliveryMethod": "Not specified", "speed": "Not specified",
                     "partner": null, "feeNote": null, "publishedAt": null,
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
| `Engine:OnlyProviders` | empty | Run only these providers (`probe.yml` sets it from its `providers` input) |
| `Providers:{Name}:Enabled` | `true` | Turn a provider off |
| `Providers:{Name}:ApiKey` | empty | Reserved; no v1 provider needs one |
| `Providers:MidMarket:BangladeshBankHtmlPath` | empty | HTML saved by `tools/bb-snapshot` |

HTTP calls go through `Microsoft.Extensions.Http.Resilience` (Polly v8). Each call gets 3 retries with jittered
exponential backoff on 5xx, 408 and 429 (honouring `Retry-After`), a 15 s timeout per attempt, and a 60 s total.
Those timeouts cover the wait for response headers; reading the body (Lari Exchange's rate page is about 1.9 MB) is
bounded by `Engine:ProviderTimeoutSeconds`. Cookies are switched off on every client (`UseCookies = false`), so a retry
never sends back a cookie a server set.

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
- `/legal`: what ratebd is and is not, data sources, accuracy, trademarks, affiliate disclosure, privacy, and how a
  provider asks for a correction or removal (a GitHub issue form, `.github/ISSUE_TEMPLATE/correction-or-removal.yml`)

What the site does:
- **Corridor table:** sorted by amount received. Columns are rate, fee, you receive, margin vs mid-market, delivery, speed and an inline-SVG 7-day sparkline. Rows show a stale badge when a quote was carried forward.
- **Exchange-house rates:** posted rates without a fee get their own list under the table, best rate first, with the fee shown as "Check with provider" (or the fee wording the source publishes next to its rate). They are never ranked against full quotes. A route where none of the providers publishes a full quote (Qatar and Oman today; the UAE and Singapore once their providers have data) shows only this list.
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
| `probe.yml` | pushes to `probe/**` branches + manual | not measured yet | a few per new provider |

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
| `_redirects` | 2,000 static + 100 dynamic | 14 (one per provider) |

**If you hit the 500-builds limit:**
- **What happens:** new builds are refused until the month resets. The last successful deployment keeps serving.
- Set build watch paths (step 4 above) so engine and CI-only commits don't trigger builds.
- Add `[CF-Pages-skip]` to a commit message to skip that one build.
- Batch local commits before pushing.
- As a last resort, deploy manually with `npx wrangler pages deploy site/dist` from your machine. This needs a Cloudflare login, not a GitHub secret.

### Third-party sources

Every source is free and keyless. They can still change or block automated requests:
- **Failing provider:** it shows as `stale` (last good quote) or `failed` in the data and on the site. The run continues.
- **Bangladesh Bank** needs the headless-browser snapshot. If that fails, the benchmark is carried forward as stale.
- **Kuwait, Qatar and Oman sources** were verified from a machine outside the Gulf and have answered GitHub's runners on every daily run since 2026-10-18.
- **New providers are probed first.** The research probes went out from an IP in Bangladesh, but the daily fetch runs on GitHub's datacentre IPs, which Cloudflare, Imperva and other firewalls can treat differently. Before a new provider is merged to `main`, push the change to a `probe/**` branch (or dispatch `probe.yml` with a comma-separated `providers` list). The run fetches from a GitHub-hosted runner into a scratch folder, commits nothing, and posts each provider's status and rates as annotations. A provider that fails, is challenged or gets a 403 there is dropped, not worked around: remove its registration line and `appsettings.json` entry, its `site/src/config/providers.json` entry, its `source.*` strings in `site/src/i18n/ui.ts` and its rows here. The eight providers added on 2026-10-20 must pass this probe before they go live; Ahalia (behind Cloudflare) and Lari (behind a Barracuda firewall) are the most likely to be affected.
- **Alerts:** check the job summary of each `fetch` run, which has a status table per corridor.
