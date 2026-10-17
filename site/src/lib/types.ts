// Mirrors the JSON written by the engine (engine/src/RateBd.Engine/Models).

export interface Quote {
  provider: string;
  corridor: string;
  sendCurrency: string;
  sendAmount: number;
  rate: number;
  fee: number;
  receiveAmount: number;
  deliveryMethod: string;
  speed: string;
  fetchedAt: string;
  stale: boolean;
}

export type ProviderStatusCode = "ok" | "failed" | "stale" | "not_supported";

export interface ProviderStatus {
  name: string;
  status: ProviderStatusCode;
  message: string | null;
}

export interface MidMarketInfo {
  rate: number;
  usdBdt: number;
  usdBdtDate: string | null;
  usdBdtSource: string;
  sendPerUsd: number;
  crossRateDate: string | null;
  crossRateSource: string;
  fetchedAt: string;
  stale: boolean;
}

export interface CorridorSnapshot {
  corridor: string;
  sendCurrency: string;
  receiveCurrency: string;
  generatedAt: string;
  midMarket: MidMarketInfo | null;
  providers: ProviderStatus[];
  quotes: Quote[];
}
