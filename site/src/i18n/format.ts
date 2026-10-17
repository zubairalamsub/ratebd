import type { Locale } from "./ui";

const intlLocale: Record<Locale, string> = { en: "en-GB", bn: "bn-BD" };

export function money(locale: Locale, amount: number, currency: string, fractionDigits = 2): string {
  return new Intl.NumberFormat(intlLocale[locale], {
    style: "currency",
    currency,
    currencyDisplay: currency === "BDT" ? "narrowSymbol" : "symbol",
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  }).format(amount);
}

/** Whole-unit send amounts such as "£1,000" or "AED 500". */
export function sendAmount(locale: Locale, amount: number, currency: string): string {
  return money(locale, amount, currency, 0);
}

export function rate(locale: Locale, value: number): string {
  return new Intl.NumberFormat(intlLocale[locale], { minimumFractionDigits: 2, maximumFractionDigits: 4 }).format(value);
}

export function number(locale: Locale, value: number, fractionDigits = 2): string {
  return new Intl.NumberFormat(intlLocale[locale], {
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  }).format(value);
}

export function percent(locale: Locale, value: number): string {
  return new Intl.NumberFormat(intlLocale[locale], {
    style: "percent",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value / 100);
}

/** "17 Oct 2026, 08:37 UTC" */
export function dateTime(locale: Locale, iso: string): string {
  const formatted = new Intl.DateTimeFormat(intlLocale[locale], {
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
    timeZone: "UTC",
  }).format(new Date(iso));
  return `${formatted} UTC`;
}

/** "17 Oct 2026" from an ISO date or yyyy-MM-dd. */
export function date(locale: Locale, iso: string): string {
  return new Intl.DateTimeFormat(intlLocale[locale], {
    day: "numeric",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(iso.length === 10 ? `${iso}T00:00:00Z` : iso));
}

export function integer(locale: Locale, value: number): string {
  return new Intl.NumberFormat(intlLocale[locale]).format(value);
}
