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
| [`site/`](site) | Astro static site built from `/data` (part 2) |
| [`.github/workflows/`](.github/workflows) | `ci.yml` (build/test/AOT publish) and `fetch.yml` (daily fetch + commit) |

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
| Remitly | `GET api.remitly.io/v3/calculator/estimate` | ✅ | ✅ |
| Taptap Send | `GET api.taptapsend.com/api/fxRates` | ✅ | ✅ |
| WorldRemit | `POST api.worldremit.com/graphql` | ✅ | Not supported: the UAE isn't a WorldRemit send country |
| Xe | `POST launchpad-api.xe.com/v2/quotes` | ✅ | Not supported: Xe Money Transfer isn't offered to UAE residents |
| MidMarket (benchmark) | Bangladesh Bank USD/BDT WAR, crossed with [fawazahmed0/currency-api](https://github.com/fawazahmed0/exchange-api) | ✅ | ✅ |

Source notes:
- **Remitly** returns a first-transfer promo price. ratebd shows the standard price instead: `base_rate` with the undiscounted fee.
- **Remitly** rate-limits bursts with HTTP 429. Requests are paced and retried with backoff.
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
