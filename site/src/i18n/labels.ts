// The engine writes English delivery/speed labels. Translate the ones we know; show anything else as-is.
import { integer, money } from "./format";
import type { Locale } from "./ui";

// Keys are matched case-insensitively: exchange houses write "Same Day", "Same day", "Next Working Day", …
const translations: Record<string, string> = {
  "Bank deposit": "ব্যাংক অ্যাকাউন্টে জমা",
  "Bank card": "ব্যাংক কার্ডে জমা",
  "Cash pickup": "নগদ সংগ্রহ",
  "Mobile wallet": "মোবাইল ওয়ালেট",
  "Mobile wallet (bKash)": "মোবাইল ওয়ালেট (বিকাশ)",
  "Mobile wallet (Nagad)": "মোবাইল ওয়ালেট (নগদ)",
  "Debit Card Deposit": "ডেবিট কার্ডে জমা",
  "Not specified": "উল্লেখ নেই",
  "Not applicable": "প্রযোজ্য নয়",
  "Same day": "একই দিনে",
  "Instant": "সঙ্গে সঙ্গে",
  "In a few minutes": "কয়েক মিনিটে",
  "In a few hours": "কয়েক ঘণ্টায়",
  "In 1 hour": "১ ঘণ্টায়",
  "Next working day": "পরের কর্মদিবসে",
  "Next bank working day": "পরের ব্যাংক কর্মদিবসে",
  "1-2 working days": "১–২ কর্মদিবসে",
  "Typically minutes": "সাধারণত কয়েক মিনিটে",
};
const fixed = new Map(Object.entries(translations).map(([en, bn]) => [en.toLowerCase(), bn]));
/** The key's own spelling, so English shows one casing per label ("Next working day", not also "Next Working Day"). */
const canonical = new Map(Object.keys(translations).map((en) => [en.toLowerCase(), en]));

const patterns: [RegExp, (n: string) => string][] = [
  [/^Within 1 hour$/, () => "১ ঘণ্টার মধ্যে"],
  [/^Within (\d+) hours?$/, (n) => `${integer("bn", Number(n))} ঘণ্টার মধ্যে`],
  [/^Within (\d+) days?$/, (n) => `${integer("bn", Number(n))} দিনের মধ্যে`],
  [/^Typically within (\d+) hours?$/, (n) => `সাধারণত ${integer("bn", Number(n))} ঘণ্টার মধ্যে`],
  [/^Typically within (\d+) days?$/, (n) => `সাধারণত ${integer("bn", Number(n))} দিনের মধ্যে`],
];

export function label(locale: Locale, text: string): string {
  if (locale === "en") return canonical.get(text.toLowerCase()) ?? text;
  const known = fixed.get(text.toLowerCase());
  if (known) return known;
  for (const [pattern, render] of patterns) {
    const match = pattern.exec(text);
    if (match) return render(match[1] ?? "");
  }
  return text;
}

/** Fee wording an exchange house publishes next to its rate, e.g. "Starting from QAR 20" or "20 QR to 30 QR". */
export function feeNote(locale: Locale, text: string): string {
  if (locale === "en") return text;
  const from = /^Starting from QAR (\d+)$/i.exec(text);
  if (from) return `${money("bn", Number(from[1]), "QAR", 0)} থেকে শুরু`;
  const range = /^(\d+) QR to (\d+) QR$/i.exec(text);
  if (range) return `${integer("bn", Number(range[1]))}–${money("bn", Number(range[2]), "QAR", 0)}`;
  return text;
}

/** Whether a label means "the source gave no value", for muted styling. */
export function isUnspecified(text: string): boolean {
  return text === "Not specified" || text === "Not applicable";
}
