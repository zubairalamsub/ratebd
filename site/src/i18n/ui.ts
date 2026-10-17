// UI strings. English is the source; Bengali translations should be reviewed by a native speaker (see README).
export const LOCALES = ["en", "bn"] as const;
export type Locale = (typeof LOCALES)[number];

const en = {
  "site.name": "ratebd",
  "site.tagline": "Compare money transfer rates to Bangladesh",
  "skip": "Skip to content",
  "nav.home": "Home",
  "nav.providers": "Providers",
  "nav.howRatesWork": "How rates work",
  "nav.incentive": "Government incentive",
  "nav.label": "Main",
  "lang.switch": "বাংলা",
  "lang.switchLabel": "পাতাটি বাংলায় পড়ুন (Read in Bengali)",

  "country.uk": "the UK",
  "country.uae": "the UAE",
  "country.uk.short": "UK",
  "country.uae.short": "UAE",

  "home.title": "Compare money transfer rates to Bangladesh",
  "home.description":
    "Daily comparison of what Wise, Remitly, Taptap Send, WorldRemit and Xe deliver in taka from the UK and UAE, measured against a mid-market benchmark based on Bangladesh Bank's USD/BDT rate.",
  "home.lead":
    "Every day we ask each provider what your family in Bangladesh would receive and compare it with the mid-market rate, so you can see the real cost: the fee plus the exchange-rate margin.",
  "home.bestFor": "Most taka for {amount} today",
  "home.bestForStale": "Most taka for {amount} (no fresh quotes on the last update)",
  "home.compare": "Compare all providers",
  "home.routes": "Choose where you send from",
  "home.from": "From {country}",
  "home.why.title": "What we compare",
  "home.why.1": "The taka that arrives, after fees, for the same amount sent.",
  "home.why.2": "How far each rate is from a mid-market benchmark based on Bangladesh Bank's USD/BDT rate.",
  "home.why.3": "Delivery method and speed, as each provider reports them.",
  "home.noQuotes": "No live quotes on this route today.",

  "corridor.title": "Send money from {country} to Bangladesh ({from} → BDT)",
  "corridor.h1": "Send money from {country} to Bangladesh",
  "corridor.description":
    "Today's {from} to BDT rates, fees and the taka you receive from {providers}, compared with a mid-market benchmark based on Bangladesh Bank's USD/BDT rate. Updated daily.",
  "corridor.lead":
    "Amounts below are what the recipient gets when you pay the stated amount in total, fees included. Sorted by the most taka received.",
  "corridor.amount": "Amount you send",
  "corridor.send": "Send {amount}",

  "table.caption": "{from} → BDT quotes for sending {amount} in total, most taka first",
  "table.provider": "Provider",
  "table.rate": "Rate",
  "table.fee": "Fee",
  "table.receive": "You receive",
  "table.margin": "Margin vs mid-market",
  "table.delivery": "Delivery",
  "table.speed": "Speed",
  "table.trend": "7-day trend",
  "table.visit": "Go to {name}",
  "table.visitHeader": "Provider website",
  "table.best": "Best",
  "table.empty": "No provider returned a quote for this amount.",

  "badge.stale": "Stale",
  "badge.staleTitle": "Could not reach {name} on the last update; showing its quote from {date}",

  "margin.better": "{value} above mid-market",
  "margin.title": "How much less you get than at the mid-market rate, fees included",

  "trend.none": "Not enough history yet",
  "trend.label": "{name}: taka received on {n} days between {first} and {last}",

  "mid.title": "Mid-market benchmark",
  "mid.rate": "1 {from} = {rate} BDT",
  "mid.explain":
    "Bangladesh Bank's USD/BDT weighted average rate of {usdBdt} (rate sheet effective {usdBdtDate}), divided by {sendPerUsd} {from} per US dollar ({crossDate}).",
  "mid.receive": "At this rate with no fee, {amount} would deliver {receive}.",
  "mid.stale": "The benchmark could not be refreshed on the last update; this value is from {date}.",
  "mid.unavailable": "The mid-market benchmark is not available right now.",

  "updated.label": "Last updated {date}",
  "updated.schedule": "Rates are collected once a day at 03:00 UTC (09:00 in Dhaka).",

  "unavailable.title": "Not available on this route",
  "status.not_supported": "Not offered",
  "status.failed": "Could not fetch a quote on the last update",

  "disclaimer":
    "Quotes are indicative, collected automatically from each provider's public calculator, and can change at any time. Always confirm the final amount with the provider before you send. Links to providers may become affiliate links; that never changes the order of the table, which is always sorted by the amount received.",

  "faq.title": "Questions",
  "faq.best.q": "Which provider gives the most taka for {amount} today?",
  "faq.best.a": "{name}: {receive} for {amount} sent (rate {rate}, fee {fee}), checked {date}.",
  "faq.best.none": "No provider returned a quote for {amount} on the last update.",
  "faq.mid.q": "What is the mid-market rate?",
  "faq.mid.a":
    "It is the midpoint between buying and selling prices in the wholesale market, with no margin added. We use Bangladesh Bank's published USD/BDT weighted average rate and convert it with a daily USD cross rate. No provider sells at exactly this rate; it is a yardstick for how much each one keeps.",
  "faq.margin.q": "Why do I receive less than at the mid-market rate?",
  "faq.margin.a":
    "Providers earn from an upfront fee, from the gap between their exchange rate and the mid-market rate, or both. The margin column combines the two: it compares what arrives with what the mid-market rate would deliver for the same total payment.",
  "faq.update.q": "How often are these rates updated?",
  "faq.update.a":
    "Once a day at 03:00 UTC. If a provider cannot be reached, we show its previous quote with a Stale badge instead of hiding it.",
  "faq.incentive.q": "Does the government of Bangladesh add anything on top?",
  "faq.incentive.a":
    "Remittances sent through formal channels can qualify for a government cash incentive paid to the recipient in Bangladesh. It is not included in the amounts above. See the government incentive page for the current rules.",

  "provider.title": "{name}: rates to Bangladesh",
  "provider.description": "Today's {name} exchange rates and fees for sending money to Bangladesh from {countries}, with a 7-day history.",
  "provider.lead": "What {name} delivered in taka on each route we track, from its public price calculator.",
  "provider.route": "{from} → BDT",
  "provider.notSupported": "Not available: {reason}",
  "provider.failed": "No quote on the last update.",
  "provider.history": "Taka received for {amount} over the last days",
  "provider.date": "Date",
  "provider.source": "Where this data comes from",
  "provider.compare": "Compare with other providers from {country}",
  "provider.allTitle": "Providers we track",

  "source.Wise": "Wise's public quote endpoint, the one its website calculator uses, paying by bank transfer. Wise deducts its fee from the amount you send.",
  "source.Remitly":
    "Remitly's public calculator. We show the standard price (everyday rate and fee), not the one-off discount Remitly gives on a first transfer.",
  "source.TaptapSend":
    "Taptap Send's public rate feed, which gives one exchange rate per route. It lists no fee for Bangladesh, and gives no delivery method or delivery time.",
  "source.WorldRemit":
    "WorldRemit's public calculator. We price each payout method it offers for Bangladesh and show the one that delivers the most. Its fee is added on top, so we deduct it from the amount sent.",
  "source.Xe":
    "Xe's public money-transfer quote service, using Xe's default payment and delivery option. Its fee is added on top, so we deduct it from the amount sent.",

  "how.title": "How we compare remittance rates",
  "how.description":
    "How ratebd collects remittance quotes, calculates the margin against a mid-market benchmark built from Bangladesh Bank's USD/BDT rate, and handles stale or missing data.",

  "incentive.title": "Bangladesh government remittance incentive",
  "incentive.description":
    "What the Bangladesh government's cash incentive on inward remittances is, who gets it and how it relates to the provider quotes on ratebd.",

  "footer.data": "Data updated daily from public provider calculators. Not financial advice.",
  "footer.source": "Source code and data",
  "notFound.title": "Page not found",
  "notFound.body": "That page does not exist. Try the comparison for your route:",
} as const;

export type UiKey = keyof typeof en;

const bn: Record<UiKey, string> = {
  "site.name": "ratebd",
  "site.tagline": "বাংলাদেশে টাকা পাঠানোর রেট তুলনা করুন",
  "skip": "মূল অংশে যান",
  "nav.home": "হোম",
  "nav.providers": "প্রতিষ্ঠানসমূহ",
  "nav.howRatesWork": "রেট কীভাবে কাজ করে",
  "nav.incentive": "সরকারি প্রণোদনা",
  "nav.label": "প্রধান",
  "lang.switch": "English",
  "lang.switchLabel": "Read this page in English (ইংরেজিতে পড়ুন)",

  "country.uk": "যুক্তরাজ্য",
  "country.uae": "সংযুক্ত আরব আমিরাত",
  "country.uk.short": "যুক্তরাজ্য",
  "country.uae.short": "আমিরাত",

  "home.title": "বাংলাদেশে টাকা পাঠানোর রেট তুলনা করুন",
  "home.description":
    "যুক্তরাজ্য ও আমিরাত থেকে Wise, Remitly, Taptap Send, WorldRemit ও Xe কত টাকা পৌঁছে দেয়, বাংলাদেশ ব্যাংকের ডলার/টাকা রেটভিত্তিক মিড-মার্কেট মানদণ্ডের সঙ্গে তার প্রতিদিনের তুলনা।",
  "home.lead":
    "প্রতিদিন আমরা প্রতিটি প্রতিষ্ঠানের কাছ থেকে জেনে নিই বাংলাদেশে আপনার পরিবার কত টাকা পাবে, এবং তা মিড-মার্কেট রেটের সঙ্গে তুলনা করি। এতে আসল খরচ বোঝা যায়: ফি এবং বিনিময় হারের মার্জিন।",
  "home.bestFor": "আজ {amount} পাঠালে সবচেয়ে বেশি টাকা",
  "home.bestForStale": "{amount} পাঠালে সবচেয়ে বেশি টাকা (সর্বশেষ হালনাগাদে নতুন কোট পাওয়া যায়নি)",
  "home.compare": "সব প্রতিষ্ঠানের তুলনা দেখুন",
  "home.routes": "কোথা থেকে পাঠাবেন বেছে নিন",
  "home.from": "{country} থেকে",
  "home.why.title": "আমরা কী তুলনা করি",
  "home.why.1": "একই পরিমাণ পাঠালে ফি বাদে কত টাকা পৌঁছায়।",
  "home.why.2": "প্রতিটি রেট বাংলাদেশ ব্যাংকের ডলার/টাকা রেটভিত্তিক মিড-মার্কেট মানদণ্ড থেকে কতটা দূরে।",
  "home.why.3": "প্রতিষ্ঠানগুলোর জানানো পাঠানোর পদ্ধতি ও সময়।",
  "home.noQuotes": "আজ এই পথে কোনো লাইভ কোট নেই।",

  "corridor.title": "{country} থেকে বাংলাদেশে টাকা পাঠান ({from} → BDT)",
  "corridor.h1": "{country} থেকে বাংলাদেশে টাকা পাঠান",
  "corridor.description":
    "{providers} থেকে আজকের {from} থেকে টাকার রেট, ফি ও প্রাপ্ত টাকার পরিমাণ, বাংলাদেশ ব্যাংকের ডলার/টাকা রেটভিত্তিক মিড-মার্কেট মানদণ্ডের সঙ্গে তুলনা। প্রতিদিন হালনাগাদ।",
  "corridor.lead":
    "নিচের পরিমাণগুলো হলো ফিসহ মোট উল্লিখিত অঙ্ক পরিশোধ করলে প্রাপক যত টাকা পান। সবচেয়ে বেশি টাকা যেখানে, সেটি সবার আগে।",
  "corridor.amount": "আপনি কত পাঠাবেন",
  "corridor.send": "{amount} পাঠালে",

  "table.caption": "মোট {amount} পাঠালে {from} → BDT কোট, সবচেয়ে বেশি টাকা আগে",
  "table.provider": "প্রতিষ্ঠান",
  "table.rate": "রেট",
  "table.fee": "ফি",
  "table.receive": "প্রাপক পাবেন",
  "table.margin": "মিড-মার্কেটের তুলনায় মার্জিন",
  "table.delivery": "পাঠানোর পদ্ধতি",
  "table.speed": "সময়",
  "table.trend": "৭ দিনের প্রবণতা",
  "table.visit": "{name}-এ যান",
  "table.visitHeader": "প্রতিষ্ঠানের ওয়েবসাইট",
  "table.best": "সেরা",
  "table.empty": "এই পরিমাণের জন্য কোনো প্রতিষ্ঠান কোট দেয়নি।",

  "badge.stale": "পুরোনো",
  "badge.staleTitle": "সর্বশেষ হালনাগাদে {name}-এর সঙ্গে যোগাযোগ করা যায়নি; {date} তারিখের কোট দেখানো হচ্ছে",

  "margin.better": "মিড-মার্কেটের চেয়ে {value} বেশি",
  "margin.title": "ফিসহ হিসাবে মিড-মার্কেট রেটের তুলনায় কতটা কম পাওয়া যায়",

  "trend.none": "এখনো যথেষ্ট ইতিহাস নেই",
  "trend.label": "{name}: {first} থেকে {last} পর্যন্ত {n} দিনে প্রাপ্ত টাকা",

  "mid.title": "মিড-মার্কেট মানদণ্ড",
  "mid.rate": "১ {from} = {rate} টাকা",
  "mid.explain":
    "বাংলাদেশ ব্যাংকের ডলার/টাকা ভারিত গড় রেট {usdBdt} (রেট শিট কার্যকর {usdBdtDate}), প্রতি মার্কিন ডলারে {sendPerUsd} {from} দিয়ে ভাগ করা ({crossDate})।",
  "mid.receive": "ফি ছাড়া এই রেটে {amount} পাঠালে পৌঁছাত {receive}।",
  "mid.stale": "সর্বশেষ হালনাগাদে মানদণ্ডটি নতুন করা যায়নি; এই মান {date} তারিখের।",
  "mid.unavailable": "মিড-মার্কেট মানদণ্ড এখন পাওয়া যাচ্ছে না।",

  "updated.label": "সর্বশেষ হালনাগাদ {date}",
  "updated.schedule": "রেট প্রতিদিন একবার সংগ্রহ করা হয়, ০৩:০০ UTC-তে (ঢাকায় সকাল ৯টা)।",

  "unavailable.title": "এই পথে পাওয়া যায় না",
  "status.not_supported": "সেবা নেই",
  "status.failed": "সর্বশেষ হালনাগাদে কোট আনা যায়নি",

  "disclaimer":
    "কোটগুলো আনুমানিক, প্রতিটি প্রতিষ্ঠানের পাবলিক ক্যালকুলেটর থেকে স্বয়ংক্রিয়ভাবে সংগ্রহ করা এবং যেকোনো সময় বদলাতে পারে। পাঠানোর আগে প্রতিষ্ঠানের কাছ থেকে চূড়ান্ত পরিমাণ নিশ্চিত করুন। প্রতিষ্ঠানের লিংকগুলো ভবিষ্যতে অ্যাফিলিয়েট লিংক হতে পারে; তাতে টেবিলের ক্রম বদলায় না, যা সবসময় প্রাপ্ত টাকার পরিমাণ অনুযায়ী সাজানো।",

  "faq.title": "প্রশ্নোত্তর",
  "faq.best.q": "আজ {amount} পাঠালে কোন প্রতিষ্ঠান সবচেয়ে বেশি টাকা দেয়?",
  "faq.best.a": "{name}: {amount} পাঠালে {receive} (রেট {rate}, ফি {fee}), যাচাই {date}।",
  "faq.best.none": "সর্বশেষ হালনাগাদে {amount}-এর জন্য কোনো প্রতিষ্ঠান কোট দেয়নি।",
  "faq.mid.q": "মিড-মার্কেট রেট কী?",
  "faq.mid.a":
    "পাইকারি বাজারে কেনা ও বেচার দামের মাঝামাঝি রেট, যাতে কোনো মার্জিন যোগ করা নেই। আমরা বাংলাদেশ ব্যাংকের প্রকাশিত ডলার/টাকা ভারিত গড় রেট ব্যবহার করি এবং দৈনিক ডলার ক্রস রেট দিয়ে রূপান্তর করি। কোনো প্রতিষ্ঠান ঠিক এই রেটে বিক্রি করে না; এটি দেখায় প্রতিটি প্রতিষ্ঠান কতটা রেখে দেয়।",
  "faq.margin.q": "মিড-মার্কেট রেটের চেয়ে কম টাকা কেন পাই?",
  "faq.margin.a":
    "প্রতিষ্ঠানগুলো আয় করে আগাম ফি থেকে, তাদের বিনিময় হার ও মিড-মার্কেট রেটের পার্থক্য থেকে, বা দুটো থেকেই। মার্জিন কলাম দুটো মিলিয়ে দেখায়: একই মোট অর্থ পরিশোধে যা পৌঁছায়, তা মিড-মার্কেট রেটে যা পৌঁছাত তার সঙ্গে তুলনা করে।",
  "faq.update.q": "এই রেট কত ঘন ঘন হালনাগাদ হয়?",
  "faq.update.a":
    "প্রতিদিন একবার, ০৩:০০ UTC-তে। কোনো প্রতিষ্ঠানের সঙ্গে যোগাযোগ করা না গেলে আমরা সেটি লুকাই না, বরং ‘পুরোনো’ চিহ্নসহ আগের কোট দেখাই।",
  "faq.incentive.q": "বাংলাদেশ সরকার কি এর ওপর কিছু যোগ করে?",
  "faq.incentive.a":
    "বৈধ পথে পাঠানো রেমিট্যান্স বাংলাদেশে প্রাপকের জন্য সরকারি নগদ প্রণোদনার যোগ্য হতে পারে। ওপরের পরিমাণে এটি ধরা হয়নি। বর্তমান নিয়ম জানতে সরকারি প্রণোদনা পাতাটি দেখুন।",

  "provider.title": "{name}: বাংলাদেশে পাঠানোর রেট",
  "provider.description": "{countries} থেকে বাংলাদেশে টাকা পাঠাতে {name}-এর আজকের বিনিময় হার ও ফি, ৭ দিনের ইতিহাসসহ।",
  "provider.lead": "আমরা যে পথগুলো দেখি, তাতে {name}-এর পাবলিক ক্যালকুলেটর অনুযায়ী কত টাকা পৌঁছেছে।",
  "provider.route": "{from} → BDT",
  "provider.notSupported": "পাওয়া যায় না: {reason}",
  "provider.failed": "সর্বশেষ হালনাগাদে কোনো কোট নেই।",
  "provider.history": "গত কয়েক দিনে {amount} পাঠালে প্রাপ্ত টাকা",
  "provider.date": "তারিখ",
  "provider.source": "এই তথ্য কোথা থেকে আসে",
  "provider.compare": "{country} থেকে অন্যান্য প্রতিষ্ঠানের সঙ্গে তুলনা করুন",
  "provider.allTitle": "যেসব প্রতিষ্ঠান আমরা দেখি",

  "source.Wise": "Wise-এর পাবলিক কোট সেবা (তাদের ওয়েবসাইটের ক্যালকুলেটরও এই সেবাটিই ব্যবহার করে), ব্যাংক ট্রান্সফারে পরিশোধ ধরে। Wise পাঠানো অঙ্ক থেকেই ফি কেটে নেয়।",
  "source.Remitly":
    "Remitly-এর পাবলিক ক্যালকুলেটর। আমরা সাধারণ দাম (নিয়মিত রেট ও ফি) দেখাই, প্রথম ট্রান্সফারে দেওয়া এককালীন ছাড় নয়।",
  "source.TaptapSend":
    "Taptap Send-এর পাবলিক রেট ফিড, যা প্রতিটি পথে একটি বিনিময় হার দেয়। বাংলাদেশের জন্য এতে কোনো ফি উল্লেখ নেই, এবং পাঠানোর পদ্ধতি বা সময়ও জানানো হয় না।",
  "source.WorldRemit":
    "WorldRemit-এর পাবলিক ক্যালকুলেটর। বাংলাদেশের জন্য তাদের প্রতিটি পেআউট পদ্ধতির দাম দেখে যেটিতে সবচেয়ে বেশি টাকা পৌঁছায় সেটি দেখাই। ফি আলাদাভাবে যোগ হয়, তাই আমরা তা পাঠানো অঙ্ক থেকে বাদ দিই।",
  "source.Xe":
    "Xe-এর পাবলিক মানি ট্রান্সফার কোট সেবা, Xe-এর ডিফল্ট পরিশোধ ও পাঠানোর পদ্ধতি ধরে। ফি আলাদাভাবে যোগ হয়, তাই আমরা তা পাঠানো অঙ্ক থেকে বাদ দিই।",

  "how.title": "আমরা কীভাবে রেমিট্যান্স রেট তুলনা করি",
  "how.description":
    "ratebd কীভাবে রেমিট্যান্স কোট সংগ্রহ করে, বাংলাদেশ ব্যাংকের ডলার/টাকা রেটভিত্তিক মিড-মার্কেট মানদণ্ডের তুলনায় মার্জিন হিসাব করে, এবং পুরোনো বা অনুপস্থিত তথ্য সামলায়।",

  "incentive.title": "বাংলাদেশ সরকারের রেমিট্যান্স প্রণোদনা",
  "incentive.description":
    "প্রবাসী আয়ে বাংলাদেশ সরকারের নগদ প্রণোদনা কী, কে পান এবং ratebd-এর কোটগুলোর সঙ্গে এর সম্পর্ক কী।",

  "footer.data": "প্রতিষ্ঠানগুলোর পাবলিক ক্যালকুলেটর থেকে প্রতিদিন হালনাগাদ। এটি আর্থিক পরামর্শ নয়।",
  "footer.source": "সোর্স কোড ও তথ্য",
  "notFound.title": "পাতাটি পাওয়া যায়নি",
  "notFound.body": "এই পাতাটি নেই। আপনার পথের তুলনা দেখুন:",
};

const dictionaries: Record<Locale, Record<UiKey, string>> = { en, bn };

export function t(locale: Locale, key: UiKey, vars: Record<string, string | number> = {}): string {
  const template = dictionaries[locale][key] ?? en[key];
  return template.replace(/\{(\w+)\}/g, (_, name: string) => String(vars[name] ?? `{${name}}`));
}
