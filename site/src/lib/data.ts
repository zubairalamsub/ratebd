// Build-time access to /data. Everything here runs in Node during `astro build`; nothing ships to the browser.
import { existsSync, readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import providersConfig from "../config/providers.json";
import type { CorridorSnapshot, PostedRate, ProviderStatus, Quote } from "./types";

declare const __RATEBD_DATA_DIR__: string;
const DATA_DIR = __RATEBD_DATA_DIR__;

export const BENCHMARK = "MidMarket";
export const SPARKLINE_DAYS = 7;

export type CountryKey =
  | "country.uk"
  | "country.uae"
  | "country.us"
  | "country.it"
  | "country.my"
  | "country.sg"
  | "country.sa"
  | "country.kw"
  | "country.qa"
  | "country.om";

export type RegionKey = "region.west" | "region.gulf" | "region.asia";

export interface CorridorMeta {
  id: string;
  /** URL segment, e.g. "gbp" in /send-money-gbp-to-bangladesh */
  from: string;
  sendCurrency: string;
  receiveCurrency: string;
  /** Key into the i18n dictionaries for the sending country name */
  countryKey: CountryKey;
  region: RegionKey;
}

export const REGIONS: RegionKey[] = ["region.west", "region.gulf", "region.asia"];

const ALL_CORRIDORS: CorridorMeta[] = [
  { id: "gbp-bdt", from: "gbp", sendCurrency: "GBP", receiveCurrency: "BDT", countryKey: "country.uk", region: "region.west" },
  { id: "usd-bdt", from: "usd", sendCurrency: "USD", receiveCurrency: "BDT", countryKey: "country.us", region: "region.west" },
  { id: "eur-bdt", from: "eur", sendCurrency: "EUR", receiveCurrency: "BDT", countryKey: "country.it", region: "region.west" },
  { id: "aed-bdt", from: "aed", sendCurrency: "AED", receiveCurrency: "BDT", countryKey: "country.uae", region: "region.gulf" },
  { id: "sar-bdt", from: "sar", sendCurrency: "SAR", receiveCurrency: "BDT", countryKey: "country.sa", region: "region.gulf" },
  { id: "kwd-bdt", from: "kwd", sendCurrency: "KWD", receiveCurrency: "BDT", countryKey: "country.kw", region: "region.gulf" },
  { id: "qar-bdt", from: "qar", sendCurrency: "QAR", receiveCurrency: "BDT", countryKey: "country.qa", region: "region.gulf" },
  { id: "omr-bdt", from: "omr", sendCurrency: "OMR", receiveCurrency: "BDT", countryKey: "country.om", region: "region.gulf" },
  { id: "myr-bdt", from: "myr", sendCurrency: "MYR", receiveCurrency: "BDT", countryKey: "country.my", region: "region.asia" },
  { id: "sgd-bdt", from: "sgd", sendCurrency: "SGD", receiveCurrency: "BDT", countryKey: "country.sg", region: "region.asia" },
];

/**
 * Corridors that have data. A corridor added to the engine appears on the site after the first run that writes
 * data/{id}.json, so the build never fails on a new, not-yet-fetched corridor.
 */
export const CORRIDORS: CorridorMeta[] = ALL_CORRIDORS.filter((c) => existsSync(join(DATA_DIR, `${c.id}.json`)));

export interface ProviderMeta {
  slug: string;
  engineName: string;
  name: string;
  homepage: string;
  affiliateUrl: string | null;
}

export const PROVIDERS: ProviderMeta[] = providersConfig.providers;

export function providerByEngineName(engineName: string): ProviderMeta | undefined {
  return PROVIDERS.find((p) => p.engineName === engineName);
}

export function corridorByFrom(from: string): CorridorMeta {
  const corridor = CORRIDORS.find((c) => c.from === from);
  if (!corridor) throw new Error(`Unknown corridor ${from}`);
  return corridor;
}

const snapshotCache = new Map<string, CorridorSnapshot>();
const historyCache = new Map<string, HistoryPoint[]>();

function readJson<T>(path: string): T {
  return JSON.parse(readFileSync(path, "utf8")) as T;
}

/** Latest snapshot for a corridor (data/{id}.json). Fails the build if it is missing. */
export function loadSnapshot(id: string): CorridorSnapshot {
  let snapshot = snapshotCache.get(id);
  if (!snapshot) {
    const path = join(DATA_DIR, `${id}.json`);
    if (!existsSync(path)) {
      throw new Error(`Missing ${path}. Run the engine (see README) before building the site.`);
    }
    snapshot = readJson<CorridorSnapshot>(path);
    snapshotCache.set(id, snapshot);
  }
  return snapshot;
}

export interface HistoryPoint {
  date: string;
  snapshot: CorridorSnapshot;
}

/** yyyy-MM-dd of the latest snapshot: the last day of the sparkline window. */
export function windowEnd(id: string): string {
  return loadSnapshot(id).generatedAt.slice(0, 10);
}

/** Whole days from `date` back to `end` (both yyyy-MM-dd). */
export function daysBefore(date: string, end: string): number {
  return Math.round((Date.parse(`${end}T00:00:00Z`) - Date.parse(`${date}T00:00:00Z`)) / 86_400_000);
}

/**
 * Daily snapshots dated within the last {@link SPARKLINE_DAYS} calendar days up to the latest snapshot, oldest first.
 * Missing days are simply absent, so charts can show them as gaps.
 */
export function loadHistory(id: string): HistoryPoint[] {
  let history = historyCache.get(id);
  if (!history) {
    const dir = join(DATA_DIR, "history", id);
    const end = windowEnd(id);
    const files = existsSync(dir)
      ? readdirSync(dir).filter((f) => /^\d{4}-\d{2}-\d{2}\.json$/.test(f)).sort()
      : [];
    history = files
      .map((f) => f.slice(0, 10))
      .filter((date) => {
        const back = daysBefore(date, end);
        return back >= 0 && back < SPARKLINE_DAYS;
      })
      .map((date) => ({ date, snapshot: readJson<CorridorSnapshot>(join(dir, `${date}.json`)) }));
    historyCache.set(id, history);
  }
  return history;
}

export interface TrendPoint {
  date: string;
  value: number | null;
  /** The quote stored for that day was carried forward from an earlier fetch. */
  stale: boolean;
}

/** One provider's received amount for one send amount across the sparkline window. */
export function trendFor(id: string, engineName: string, amount: number): TrendPoint[] {
  return loadHistory(id).map((h) => {
    const q = h.snapshot.quotes.find((x) => x.provider === engineName && x.sendAmount === amount);
    return { date: h.date, value: q?.receiveAmount ?? null, stale: q?.stale ?? false };
  });
}

export interface Row {
  quote: Quote;
  provider: ProviderMeta;
  /** (mid − effective rate) / mid, as a percentage; negative means better than the benchmark */
  marginPct: number | null;
  trend: TrendPoint[];
}

export function marginPct(quote: Quote, midRate: number | undefined): number | null {
  if (!midRate || quote.sendAmount <= 0) return null;
  const effective = quote.receiveAmount / quote.sendAmount;
  return ((midRate - effective) / midRate) * 100;
}

/** Provider quotes for one amount, best (most received) first. The benchmark is excluded. */
export function rowsFor(id: string, amount: number): Row[] {
  const snapshot = loadSnapshot(id);
  const mid = snapshot.midMarket?.rate;
  return snapshot.quotes
    .filter((q) => q.provider !== BENCHMARK && q.sendAmount === amount)
    .sort((a, b) => b.receiveAmount - a.receiveAmount)
    .flatMap((quote) => {
      const provider = providerByEngineName(quote.provider);
      if (!provider) return [];
      return [{
        quote,
        provider,
        marginPct: marginPct(quote, mid),
        trend: trendFor(id, quote.provider, amount),
      }];
    });
}

/**
 * Rows that share the most taka received among fresh (non-stale) quotes. Several rows can tie: Al Mulla, for one,
 * often routes through Western Union at exactly Western Union's price. Empty when every quote is stale.
 */
export function topRows(rows: Row[]): Row[] {
  const fresh = rows.filter((r) => !r.quote.stale);
  if (fresh.length === 0) return [];
  const most = Math.max(...fresh.map((r) => r.quote.receiveAmount));
  return fresh.filter((r) => r.quote.receiveAmount === most);
}

/** Send amounts quoted on a corridor, largest (the headline amount) first. */
export function amountsFor(id: string): number[] {
  return [...new Set(loadSnapshot(id).quotes.map((q) => q.sendAmount))].sort((a, b) => b - a);
}

export function benchmarkQuote(id: string, amount: number): Quote | undefined {
  return loadSnapshot(id).quotes.find((q) => q.provider === BENCHMARK && q.sendAmount === amount);
}

/** Statuses of every provider run on a corridor: quoting providers first, then exchange houses with posted rates. */
function allStatuses(id: string): ProviderStatus[] {
  const snapshot = loadSnapshot(id);
  return [...snapshot.providers, ...(snapshot.postedRateProviders ?? [])];
}

/** Providers that returned nothing usable this run (not supported, or failed with no earlier data). */
export function unavailable(id: string): { provider: ProviderMeta; status: ProviderStatus }[] {
  return allStatuses(id)
    .filter((s) => s.name !== BENCHMARK && (s.status === "not_supported" || s.status === "failed"))
    .flatMap((status) => {
      const provider = providerByEngineName(status.name);
      return provider ? [{ provider, status }] : [];
    });
}

/**
 * A provider's status on a corridor. Undefined means it was not run there at all: providers collected only from
 * certain countries (a Kuwaiti exchange house, say) have no entry elsewhere.
 */
export function statusFor(id: string, engineName: string): ProviderStatus | undefined {
  return allStatuses(id).find((p) => p.name === engineName);
}

/** Providers with data on at least one route; a disabled or not-yet-run provider gets no page. */
export const LISTED_PROVIDERS: ProviderMeta[] = PROVIDERS.filter((p) =>
  CORRIDORS.some((c) => statusFor(c.id, p.engineName) !== undefined),
);

export interface PostedRow {
  posted: PostedRate;
  provider: ProviderMeta;
}

/** Exchange-house rates published without a fee, best rate first. */
export function postedRowsFor(id: string): PostedRow[] {
  return (loadSnapshot(id).postedRates ?? [])
    .slice()
    .sort((a, b) => b.rate - a.rate)
    .flatMap((posted) => {
      const provider = providerByEngineName(posted.provider);
      return provider ? [{ posted, provider }] : [];
    });
}

/** A provider's best posted rate per day across the sparkline window (rates without a fee, so no amount). */
export function postedTrendFor(id: string, engineName: string): TrendPoint[] {
  return loadHistory(id).map((h) => {
    const rates = (h.snapshot.postedRates ?? []).filter((r) => r.provider === engineName);
    const best = rates.length ? rates.reduce((a, b) => (b.rate > a.rate ? b : a)) : undefined;
    return { date: h.date, value: best?.rate ?? null, stale: best?.stale ?? false };
  });
}

/** Most recent time any data on the site was generated. */
export function lastUpdated(): string {
  return CORRIDORS.map((c) => loadSnapshot(c.id).generatedAt).sort().at(-1)!;
}
