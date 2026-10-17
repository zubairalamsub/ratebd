// The engine writes English delivery/speed labels. Translate the ones we know; show anything else as-is.
import { integer } from "./format";
import type { Locale } from "./ui";

const fixed: Record<string, string> = {
  "Bank deposit": "ব্যাংক অ্যাকাউন্টে জমা",
  "Cash pickup": "নগদ সংগ্রহ",
  "Mobile wallet": "মোবাইল ওয়ালেট",
  "Mobile wallet (bKash)": "মোবাইল ওয়ালেট (বিকাশ)",
  "Debit Card Deposit": "ডেবিট কার্ডে জমা",
  "Not specified": "উল্লেখ নেই",
  "Not applicable": "প্রযোজ্য নয়",
  "Same day": "একই দিনে",
  "Typically minutes": "সাধারণত কয়েক মিনিটে",
};

const patterns: [RegExp, (n: string) => string][] = [
  [/^Within 1 hour$/, () => "১ ঘণ্টার মধ্যে"],
  [/^Within (\d+) hours?$/, (n) => `${integer("bn", Number(n))} ঘণ্টার মধ্যে`],
  [/^Within (\d+) days?$/, (n) => `${integer("bn", Number(n))} দিনের মধ্যে`],
  [/^Typically within (\d+) hours?$/, (n) => `সাধারণত ${integer("bn", Number(n))} ঘণ্টার মধ্যে`],
  [/^Typically within (\d+) days?$/, (n) => `সাধারণত ${integer("bn", Number(n))} দিনের মধ্যে`],
];

export function label(locale: Locale, text: string): string {
  if (locale === "en") return text;
  if (fixed[text]) return fixed[text];
  for (const [pattern, render] of patterns) {
    const match = pattern.exec(text);
    if (match) return render(match[1] ?? "");
  }
  return text;
}

/** Whether a label means "the source gave no value", for muted styling. */
export function isUnspecified(text: string): boolean {
  return text === "Not specified" || text === "Not applicable";
}
