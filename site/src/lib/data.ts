// Build-time access to /data. Everything here runs in Node during `astro build`; nothing ships to the browser.
import { existsSync, readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import providersConfig from "../config/providers.json";
import type { CorridorSnapshot, ProviderStatus, Quote } from "./types";

declare const __RATEBD_DATA_DIR__: string;
const DATA_DIR = __RATEBD_DATA_DIR__;

export const BENCHMARK = "MidMarket";
export const SEND_AMOUNTS = [1000, 500] as const;
export const SPARKLINE_DAYS = 7;

export interface CorridorMeta {
  id: string;
  /** URL segment, e.g. "gbp" in /send-money-gbp-to-bangladesh */
  from: string;
  sendCurrency: string;
  receiveCurrency: string;
  /** Key into the i18n dictionaries for the sending country name */
  countryKey: "country.uk" | "country.uae";
}

export const CORRIDORS: CorridorMeta[] = [
  { id: "gbp-bdt", from: "gbp", sendCurrency: "GBP", receiveCurrency: "BDT", countryKey: "country.uk" },
  { id: "aed-bdt", from: "aed", sendCurrency: "AED", receiveCurrency: "BDT", countryKey: "country.uae" },
];

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

/** The last {@link SPARKLINE_DAYS} daily snapshots for a corridor, oldest first. */
export function loadHistory(id: string): HistoryPoint[] {
  let history = historyCache.get(id);
  if (!history) {
    const dir = join(DATA_DIR, "history", id);
    const files = existsSync(dir)
      ? readdirSync(dir).filter((f) => /^\d{4}-\d{2}-\d{2}\.json$/.test(f)).sort()
      : [];
    history = files.slice(-SPARKLINE_DAYS).map((f) => ({
      date: f.slice(0, 10),
      snapshot: readJson<CorridorSnapshot>(join(dir, f)),
    }));
    historyCache.set(id, history);
  }
  return history;
}

export interface Row {
  quote: Quote;
  provider: ProviderMeta;
  /** (mid − effective rate) / mid, as a percentage; negative means better than the benchmark */
  marginPct: number | null;
  /** Receive amounts for the same provider and amount over the last days (null where missing) */
  trend: (number | null)[];
  trendDates: string[];
}

export function marginPct(quote: Quote, midRate: number | undefined): number | null {
  if (!midRate || quote.sendAmount <= 0) return null;
  const effective = quote.receiveAmount / quote.sendAmount;
  return ((midRate - effective) / midRate) * 100;
}

/** Provider quotes for one amount, best (most received) first. The benchmark is excluded. */
export function rowsFor(id: string, amount: number): Row[] {
  const snapshot = loadSnapshot(id);
  const history = loadHistory(id);
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
        trend: history.map(
          (h) => h.snapshot.quotes.find((q) => q.provider === quote.provider && q.sendAmount === amount)?.receiveAmount ?? null,
        ),
        trendDates: history.map((h) => h.date),
      }];
    });
}

export function benchmarkQuote(id: string, amount: number): Quote | undefined {
  return loadSnapshot(id).quotes.find((q) => q.provider === BENCHMARK && q.sendAmount === amount);
}

/** Providers that returned nothing usable this run (not supported, or failed with no earlier quote). */
export function unavailable(id: string): { provider: ProviderMeta; status: ProviderStatus }[] {
  return loadSnapshot(id)
    .providers.filter((s) => s.name !== BENCHMARK && (s.status === "not_supported" || s.status === "failed"))
    .flatMap((status) => {
      const provider = providerByEngineName(status.name);
      return provider ? [{ provider, status }] : [];
    });
}

export function statusFor(id: string, engineName: string): ProviderStatus | undefined {
  return loadSnapshot(id).providers.find((p) => p.name === engineName);
}

/** Most recent time any data on the site was generated. */
export function lastUpdated(): string {
  return CORRIDORS.map((c) => loadSnapshot(c.id).generatedAt).sort().at(-1)!;
}
