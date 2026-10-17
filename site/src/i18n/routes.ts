import type { Locale } from "./ui";

/** Path for a page in a locale. English is unprefixed; Bengali lives under /bn. `path` starts with "/". */
export function localePath(locale: Locale, path: string): string {
  if (locale === "en") return path;
  return path === "/" ? "/bn" : `/bn${path}`;
}

/** Locale-neutral path of the current page, e.g. "/bn/how-rates-work" → "/how-rates-work". */
export function neutralPath(pathname: string): string {
  const clean = pathname.replace(/\.html$/, "").replace(/\/+$/, "") || "/";
  if (clean === "/bn") return "/";
  return clean.startsWith("/bn/") ? clean.slice(3) : clean;
}

export const corridorPath = (from: string) => `/send-money-${from}-to-bangladesh`;
export const providerPath = (slug: string) => `/providers/${slug}`;
export const goPath = (slug: string) => `/go/${slug}`;
