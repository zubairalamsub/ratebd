// UI strings. English is the source; Bengali translations should be reviewed by a native speaker (see README).
export const LOCALES = ["en", "bn"] as const;
export type Locale = (typeof LOCALES)[number];

const en = {
  "site.name": "ratebd",
  "site.tagline": "Compare money transfer rates to Bangladesh",
  "skip": "Skip to content",
  "nav.home": "Home",
  "nav.routes": "All routes",
  "nav.providers": "Providers",
  "nav.howRatesWork": "How rates work",
  "nav.incentive": "Government incentive",
  "nav.legal": "Legal notice and disclaimer",
  "nav.label": "Main",
  "lang.switch": "বাংলা",
  "lang.switchLabel": "পাতাটি বাংলায় পড়ুন (Read in Bengali)",

  "country.uk": "the UK",
  "country.uae": "the UAE",
  "country.us": "the USA",
  "country.it": "Italy",
  "country.my": "Malaysia",
  "country.sg": "Singapore",
  "country.sa": "Saudi Arabia",
  "country.kw": "Kuwait",
  "country.qa": "Qatar",
  "country.om": "Oman",
  "region.west": "Europe and North America",
  "region.gulf": "Middle East",
  "region.asia": "Asia",

  "home.title": "Compare money transfer rates to Bangladesh",
  "home.description":
    "Daily comparison of the taka that money transfer services and exchange houses deliver to Bangladesh, measured against a mid-market benchmark based on Bangladesh Bank's USD/BDT rate. We only collect from sources whose published terms and robots.txt don't prohibit it.",
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
  "home.stats": "{countries} sending countries · {providers} providers · updated daily at 09:00 Dhaka time",
  "home.bestPosted": "Best exchange-house rate today (fee not included)",
  "home.bestPostedStale": "Best exchange-house rate (no fresh rates on the last update)",

  "corridor.title": "Send money from {country} to Bangladesh ({from} → BDT)",
  "corridor.h1": "Send money from {country} to Bangladesh",
  "corridor.description":
    "Today's {from} to BDT rates, fees and the taka you receive from {providers}, compared with a mid-market benchmark based on Bangladesh Bank's USD/BDT rate. Updated daily.",
  "corridor.descriptionPosted":
    "Today's {from} to BDT exchange-house rates from {providers}, compared with a mid-market benchmark based on Bangladesh Bank's USD/BDT rate. Fees are not included. Updated daily.",
  "corridor.lead":
    "Amounts below are what the recipient gets when you pay the stated amount in total, fees included. Sorted by the most taka received.",
  "corridor.leadPosted":
    "None of the providers we track publishes a full quote (rate and fee) for sending from {country}. Below are the rates exchange houses post, best rate first; their fees are not included, so ask before you send.",
  "corridor.amount": "Amount you send",
  "corridor.send": "Send {amount}",
  "corridor.otherRoutes": "Other sending countries",
  "corridor.changeCountry": "Change country",

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
  "table.visitShort": "Visit",
  "table.best": "Best",
  "table.empty": "No provider returned a quote for this amount.",

  "posted.title": "Exchange-house rates (fee not included)",
  "posted.lead":
    "These exchange houses publish a rate, but we have no exact fee for them, so they are listed by rate and not ranked against full quotes. Fees differ from house to house; ask for the fee before you send.",
  "posted.caption": "{from} → BDT rates posted by exchange houses, best rate first",
  "posted.house": "Exchange house",
  "posted.beforeFees": "{amount} at this rate (before any fee)",
  "posted.feeNone": "Check with provider",
  "posted.via": "via {name}",
  "posted.rateValue": "1 {from} = {rate} BDT",

  "badge.stale": "Stale",
  "badge.staleTitle": "Could not reach {name} on the last update; showing its quote from {date}",

  "margin.better": "{value} above mid-market",
  "margin.title": "How much less you get than at the mid-market rate, fees included",
  "margin.short": "Margin",
  "margin.betterShort": "{value} above mid",

  "trend.none": "Not enough history yet",
  "trend.label": "{name}: taka received on {n} days between {first} and {last}",
  "trend.rateLabel": "{name}: posted rate on {n} days between {first} and {last}",

  "mid.title": "Mid-market benchmark",
  "mid.rate": "1 {from} = {rate} BDT",
  "mid.explain":
    "Bangladesh Bank's USD/BDT weighted average rate of {usdBdt} (rate sheet effective {usdBdtDate}), divided by {sendPerUsd} {from} per US dollar ({crossDate}).",
  "mid.receive": "At this rate with no fee, {amount} would deliver {receive}.",
  "mid.stale": "The benchmark could not be refreshed on the last update; this value is from {date}.",
  "mid.unavailable": "The mid-market benchmark is not available right now.",
  "mid.how": "How is this calculated?",

  "updated.label": "Last updated {date}",
  "updated.schedule": "Rates are collected once a day at 03:00 UTC (09:00 in Dhaka).",

  "unavailable.title": "Not available on this route",
  "unavailable.count": "Not available on this route ({n})",
  "status.not_supported": "Not offered",
  "status.failed": "Could not fetch a quote on the last update",

  "disclaimer":
    "Quotes are indicative, collected automatically from each provider's public calculator or rate page, and can change at any time. Always confirm the final amount with the provider before you send. Links to providers may become affiliate links; that never changes the order of the table, which is always sorted by the amount received.",

  "faq.title": "Questions",
  "faq.best.q": "Which provider gives the most taka for {amount} today?",
  "faq.best.a": "{name}: {receive} for {amount} sent (rate {rate}, fee {fee}), checked {date}.",
  "faq.best.none": "No provider returned a quote for {amount} on the last update.",
  "faq.posted.q": "Which exchange house posts the best rate today?",
  "faq.posted.a": "{name}: 1 {from} = {rate} BDT, checked {date}. Its fee is not included here, so ask for it before you send.",
  "faq.posted.aRange": "{name}: 1 {from} = {rate} BDT, checked {date}. The fee is given only as \"{fee}\", so confirm it before you send.",
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
  "provider.descriptionMany": "Today's {name} exchange rates and fees for sending money to Bangladesh from {n} countries, with a 7-day history.",
  "provider.lead": "What {name} delivered in taka on each route we track, from its public price calculator.",
  "provider.independent": "ratebd is independent and is not affiliated with or endorsed by {name}.",
  "provider.descriptionPosted": "The rate {name} posts for sending money to Bangladesh from {countries}, with a 7-day history.",
  "provider.leadPosted":
    "The rate {name} publishes on each route we track. We collect only its rate, not a fee, so the rate is listed separately rather than ranked against full quotes.",
  "provider.leadPostedRange":
    "The rates {name} lists on each route we track. It gives the fee only as a range, so its rates are listed separately rather than ranked against full quotes.",
  "provider.rateHistory": "Best posted rate over the last days",
  "provider.route": "{from} → BDT",
  "provider.notSupported": "Not available: {reason}",
  "provider.failed": "No quote on the last update.",
  "provider.history": "Taka received for {amount} over the last days",
  "provider.date": "Date",
  "provider.source": "Where this data comes from",
  "provider.compare": "Compare with other providers from {country}",
  "provider.allTitle": "Providers we track",

  "source.AlMulla":
    "Al Mulla Exchange's public calculator (Kuwait). It prices each correspondent-bank route; we show the best rate, with its fee (added on top) deducted from the amount sent.",
  "source.HabibQatar": "The currency converter on Habib Qatar International Exchange's website. No fee or payout method is given.",
  "source.AlMirqab": "The DRAFT/TT rate in Al Mirqab Exchange's public rates table (Qatar). No fee is published.",
  "source.Unimoni":
    "Unimoni Oman's live-rate converter. A service fee is charged on top but not published, and the rate is the same for every payout method.",
  "source.PKExchange":
    "The TT (bank transfer) rate on Purshottam Kanji Exchange's dated \"Today's Exchange Rates\" page (Oman), which it labels indicative. The commission is shown only on the receipt.",
  "source.Aman":
    "The transfer rate on Aman Exchange's rate board (Kuwait). A rate the board dates more than three days back is not treated as current. No fee is published.",
  "source.GccExchange":
    "The public rate feed behind the calculator on GCC Exchange's website (UAE). The calculator always shows a fee of AED 0.00, which we don't take as its actual fee; no payout method is published.",
  "source.OrientExchange":
    "The dated \"Transfer Rates\" table on Orient Exchange's Currency Rates page (UAE). Orient lists its remittance charges in a separate document on its website, which we don't collect; no payout method is given with the rate.",
  "source.LariExchange":
    "The \"Transfer Money\" rate in the dated converter on Lari Exchange's Exchange Rates page (UAE), which it labels indicative. The page says fees are not included and names no payout method.",
  "source.HadiExpress":
    "The \"Money Transfer Rate\" converter on Hadi Express Exchange's home page (UAE), which calls its rates indicative and updated every 24 hours. Its remittance charges are listed separately, in the FAQ on its remittance services page; we don't collect them, so check before you send. No payout method or update time is given with the rate.",
  "source.AhaliaExchange":
    "The \"Bank Transfer Rate\" board on Ahalia Exchange's home page (UAE), as the page's own script displays it. It calls the rates indicative and gives no fee or update time with them; its key facts statement for customers gives fees only as a range, which we don't collect.",
  "source.BAExpress":
    "BA Express USA's public calculator, which gives a rate and a service fee for each amount. We treat the fee as added on top and deduct it from the amount sent. No payout method or delivery time is published.",
  "source.SonaliExchange":
    "The USD to BDT conversion rate on Sonali Exchange's \"Exchange Rates and Tariff\" page (USA). Sonali's terms say it does not currently charge the sender a fee; the page itself shows no fee, payout method or update date.",

  "how.title": "How we compare remittance rates",
  "how.description":
    "How ratebd collects remittance quotes, calculates the margin against a mid-market benchmark built from Bangladesh Bank's USD/BDT rate, and handles stale or missing data.",

  "incentive.title": "Bangladesh government remittance incentive",
  "incentive.description":
    "What the Bangladesh government's cash incentive on inward remittances is, who gets it and how it relates to the provider quotes on ratebd.",

  "footer.data": "Data updated daily from providers' public calculators and rate pages. Not financial advice.",
  "footer.source": "Source code and data",
  "footer.independent":
    "Independent comparison, not affiliated with any provider. Provider names are trademarks of their respective owners.",
  "footer.routes": "Sending countries",
  "footer.about": "About the data",
  "legal.title": "Legal notice and disclaimer",
  "legal.description":
    "What ratebd is and is not, where its rates come from, how accurate they are, trademarks, affiliate links, privacy, and how providers can request a correction or removal.",
  "legal.contents": "On this page",
  "legal.updated": "Last updated {date}",
  "notFound.title": "Page not found",
  "notFound.body": "That page does not exist. Try the comparison for your route:",
} as const;

export type UiKey = keyof typeof en;

const bn: Record<UiKey, string> = {
  "site.name": "ratebd",
  "site.tagline": "বাংলাদেশে টাকা পাঠানোর রেট তুলনা করুন",
  "skip": "মূল অংশে যান",
  "nav.home": "হোম",
  "nav.routes": "সব দেশ",
  "nav.providers": "প্রতিষ্ঠানসমূহ",
  "nav.howRatesWork": "রেট কীভাবে কাজ করে",
  "nav.incentive": "সরকারি প্রণোদনা",
  "nav.legal": "আইনি নোটিশ ও দায়মুক্তি",
  "nav.label": "প্রধান",
  "lang.switch": "English",
  "lang.switchLabel": "Read this page in English (ইংরেজিতে পড়ুন)",

  "country.uk": "যুক্তরাজ্য",
  "country.uae": "সংযুক্ত আরব আমিরাত",
  "country.us": "যুক্তরাষ্ট্র",
  "country.it": "ইতালি",
  "country.my": "মালয়েশিয়া",
  "country.sg": "সিঙ্গাপুর",
  "country.sa": "সৌদি আরব",
  "country.kw": "কুয়েত",
  "country.qa": "কাতার",
  "country.om": "ওমান",
  "region.west": "ইউরোপ ও উত্তর আমেরিকা",
  "region.gulf": "মধ্যপ্রাচ্য",
  "region.asia": "এশিয়া",

  "home.title": "বাংলাদেশে টাকা পাঠানোর রেট তুলনা করুন",
  "home.description":
    "মানি ট্রান্সফার সেবা ও এক্সচেঞ্জ হাউসগুলো বাংলাদেশে কত টাকা পৌঁছে দেয়, বাংলাদেশ ব্যাংকের ডলার/টাকা রেটভিত্তিক মিড-মার্কেট মানদণ্ডের সঙ্গে তার প্রতিদিনের তুলনা। যেসব উৎসের প্রকাশিত শর্তাবলি ও robots.txt এটি নিষেধ করে না, শুধু সেগুলো থেকেই তথ্য সংগ্রহ করা হয়।",
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
  "home.stats": "{countries}টি প্রেরক দেশ · {providers}টি প্রতিষ্ঠান · প্রতিদিন ঢাকা সময় সকাল ৯টায় হালনাগাদ",
  "home.bestPosted": "আজ এক্সচেঞ্জ হাউসের সেরা রেট (ফি ধরা হয়নি)",
  "home.bestPostedStale": "এক্সচেঞ্জ হাউসের সেরা রেট (সর্বশেষ হালনাগাদে নতুন রেট পাওয়া যায়নি)",

  "corridor.title": "{country} থেকে বাংলাদেশে টাকা পাঠান ({from} → BDT)",
  "corridor.h1": "{country} থেকে বাংলাদেশে টাকা পাঠান",
  "corridor.description":
    "{providers} থেকে আজকের {from} থেকে টাকার রেট, ফি ও প্রাপ্ত টাকার পরিমাণ, বাংলাদেশ ব্যাংকের ডলার/টাকা রেটভিত্তিক মিড-মার্কেট মানদণ্ডের সঙ্গে তুলনা। প্রতিদিন হালনাগাদ।",
  "corridor.descriptionPosted":
    "{providers} থেকে আজকের {from} থেকে টাকার এক্সচেঞ্জ হাউস রেট, বাংলাদেশ ব্যাংকের ডলার/টাকা রেটভিত্তিক মিড-মার্কেট মানদণ্ডের সঙ্গে তুলনা। ফি ধরা হয়নি। প্রতিদিন হালনাগাদ।",
  "corridor.lead":
    "নিচের পরিমাণগুলো হলো ফিসহ মোট উল্লিখিত অঙ্ক পরিশোধ করলে প্রাপক যত টাকা পান। সবচেয়ে বেশি টাকা যেখানে, সেটি সবার আগে।",
  "corridor.leadPosted":
    "{country} থেকে পাঠানোর জন্য আমাদের তালিকার কোনো প্রতিষ্ঠান পূর্ণ কোট (রেট ও ফি) প্রকাশ করে না। নিচে এক্সচেঞ্জ হাউসগুলোর ঘোষিত রেট, সেরা রেট আগে; এতে তাদের ফি ধরা হয়নি, তাই পাঠানোর আগে জেনে নিন।",
  "corridor.amount": "আপনি কত পাঠাবেন",
  "corridor.send": "{amount} পাঠালে",
  "corridor.otherRoutes": "অন্যান্য প্রেরক দেশ",
  "corridor.changeCountry": "দেশ বদলান",

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
  "table.visitShort": "ভিজিট",
  "table.best": "সেরা",
  "table.empty": "এই পরিমাণের জন্য কোনো প্রতিষ্ঠান কোট দেয়নি।",

  "posted.title": "এক্সচেঞ্জ হাউসের রেট (ফি ধরা হয়নি)",
  "posted.lead":
    "এই এক্সচেঞ্জ হাউসগুলো রেট প্রকাশ করে, কিন্তু তাদের নির্দিষ্ট ফি আমাদের কাছে নেই, তাই এগুলো রেট অনুযায়ী আলাদা তালিকায় দেখানো হয়েছে, পূর্ণ কোটের সঙ্গে তুলনা করে সাজানো হয়নি। ফি একেক হাউসে একেক রকম; পাঠানোর আগে ফি জেনে নিন।",
  "posted.caption": "এক্সচেঞ্জ হাউসগুলোর ঘোষিত {from} → BDT রেট, সেরা রেট আগে",
  "posted.house": "এক্সচেঞ্জ হাউস",
  "posted.beforeFees": "এই রেটে {amount} (কোনো ফি ধরার আগে)",
  "posted.feeNone": "প্রতিষ্ঠানের কাছে জেনে নিন",
  "posted.via": "{name}-এর মাধ্যমে",
  "posted.rateValue": "১ {from} = {rate} টাকা",

  "badge.stale": "পুরোনো",
  "badge.staleTitle": "সর্বশেষ হালনাগাদে {name}-এর সঙ্গে যোগাযোগ করা যায়নি; {date} তারিখের কোট দেখানো হচ্ছে",

  "margin.better": "মিড-মার্কেটের চেয়ে {value} বেশি",
  "margin.title": "ফিসহ হিসাবে মিড-মার্কেট রেটের তুলনায় কতটা কম পাওয়া যায়",
  "margin.short": "মার্জিন",
  "margin.betterShort": "মিড-মার্কেটের চেয়ে {value} বেশি",

  "trend.none": "এখনো যথেষ্ট ইতিহাস নেই",
  "trend.label": "{name}: {first} থেকে {last} পর্যন্ত {n} দিনে প্রাপ্ত টাকা",
  "trend.rateLabel": "{name}: {first} থেকে {last} পর্যন্ত {n} দিনের ঘোষিত রেট",

  "mid.title": "মিড-মার্কেট মানদণ্ড",
  "mid.rate": "১ {from} = {rate} টাকা",
  "mid.explain":
    "বাংলাদেশ ব্যাংকের ডলার/টাকা ভারিত গড় রেট {usdBdt} (রেট শিট কার্যকর {usdBdtDate}), প্রতি মার্কিন ডলারে {sendPerUsd} {from} দিয়ে ভাগ করা ({crossDate})।",
  "mid.receive": "ফি ছাড়া এই রেটে {amount} পাঠালে পৌঁছাত {receive}।",
  "mid.stale": "সর্বশেষ হালনাগাদে মানদণ্ডটি নতুন করা যায়নি; এই মান {date} তারিখের।",
  "mid.unavailable": "মিড-মার্কেট মানদণ্ড এখন পাওয়া যাচ্ছে না।",
  "mid.how": "এটি কীভাবে হিসাব করা হয়?",

  "updated.label": "সর্বশেষ হালনাগাদ {date}",
  "updated.schedule": "রেট প্রতিদিন একবার সংগ্রহ করা হয়, ০৩:০০ UTC-তে (ঢাকায় সকাল ৯টা)।",

  "unavailable.title": "এই পথে পাওয়া যায় না",
  "unavailable.count": "এই পথে পাওয়া যায় না ({n})",
  "status.not_supported": "সেবা নেই",
  "status.failed": "সর্বশেষ হালনাগাদে কোট আনা যায়নি",

  "disclaimer":
    "কোটগুলো আনুমানিক, প্রতিটি প্রতিষ্ঠানের পাবলিক ক্যালকুলেটর বা রেট পাতা থেকে স্বয়ংক্রিয়ভাবে সংগ্রহ করা এবং যেকোনো সময় বদলাতে পারে। পাঠানোর আগে প্রতিষ্ঠানের কাছ থেকে চূড়ান্ত পরিমাণ নিশ্চিত করুন। প্রতিষ্ঠানের লিংকগুলো ভবিষ্যতে অ্যাফিলিয়েট লিংক হতে পারে; তাতে টেবিলের ক্রম বদলায় না, যা সবসময় প্রাপ্ত টাকার পরিমাণ অনুযায়ী সাজানো।",

  "faq.title": "প্রশ্নোত্তর",
  "faq.best.q": "আজ {amount} পাঠালে কোন প্রতিষ্ঠান সবচেয়ে বেশি টাকা দেয়?",
  "faq.best.a": "{name}: {amount} পাঠালে {receive} (রেট {rate}, ফি {fee}), যাচাই {date}।",
  "faq.best.none": "সর্বশেষ হালনাগাদে {amount}-এর জন্য কোনো প্রতিষ্ঠান কোট দেয়নি।",
  "faq.posted.q": "আজ কোন এক্সচেঞ্জ হাউসের ঘোষিত রেট সবচেয়ে ভালো?",
  "faq.posted.a": "{name}: ১ {from} = {rate} টাকা, যাচাই {date}। এখানে এর ফি ধরা হয়নি, তাই পাঠানোর আগে জেনে নিন।",
  "faq.posted.aRange": "{name}: ১ {from} = {rate} টাকা, যাচাই {date}। ফি শুধু “{fee}” হিসেবে দেওয়া আছে, তাই পাঠানোর আগে নিশ্চিত হয়ে নিন।",
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
  "provider.descriptionMany": "{n}টি দেশ থেকে বাংলাদেশে টাকা পাঠাতে {name}-এর আজকের বিনিময় হার ও ফি, ৭ দিনের ইতিহাসসহ।",
  "provider.lead": "আমরা যে পথগুলো দেখি, তাতে {name}-এর পাবলিক ক্যালকুলেটর অনুযায়ী কত টাকা পৌঁছেছে।",
  "provider.independent": "ratebd একটি স্বাধীন সেবা; {name}-এর সঙ্গে এর কোনো সম্পর্ক নেই এবং {name} এটি অনুমোদন করেনি।",
  "provider.descriptionPosted": "{countries} থেকে বাংলাদেশে টাকা পাঠাতে {name}-এর ঘোষিত রেট, ৭ দিনের ইতিহাসসহ।",
  "provider.leadPosted":
    "আমরা যে পথগুলো দেখি, তাতে {name}-এর প্রকাশিত রেট। আমরা শুধু এর রেট সংগ্রহ করি, ফি নয়, তাই রেটটি পূর্ণ কোটের সঙ্গে তুলনা করে না সাজিয়ে আলাদাভাবে দেখানো হয়।",
  "provider.leadPostedRange":
    "আমরা যে পথগুলো দেখি, তাতে {name}-এর তালিকাভুক্ত রেট। এটি ফি শুধু একটি সীমা হিসেবে দেয়, তাই এর রেটগুলো পূর্ণ কোটের সঙ্গে তুলনা করে না সাজিয়ে আলাদাভাবে দেখানো হয়।",
  "provider.rateHistory": "গত কয়েক দিনের সেরা ঘোষিত রেট",
  "provider.route": "{from} → BDT",
  "provider.notSupported": "পাওয়া যায় না: {reason}",
  "provider.failed": "সর্বশেষ হালনাগাদে কোনো কোট নেই।",
  "provider.history": "গত কয়েক দিনে {amount} পাঠালে প্রাপ্ত টাকা",
  "provider.date": "তারিখ",
  "provider.source": "এই তথ্য কোথা থেকে আসে",
  "provider.compare": "{country} থেকে অন্যান্য প্রতিষ্ঠানের সঙ্গে তুলনা করুন",
  "provider.allTitle": "যেসব প্রতিষ্ঠান আমরা দেখি",

  "source.AlMulla":
    "Al Mulla Exchange-এর পাবলিক ক্যালকুলেটর (কুয়েত)। এটি প্রতিটি করেসপন্ডেন্ট ব্যাংকের পথের দাম দেয়; আমরা সেরা রেটটি দেখাই, এবং এর ফি (আলাদাভাবে যোগ হয়) পাঠানো অঙ্ক থেকে বাদ দিই।",
  "source.HabibQatar": "Habib Qatar International Exchange-এর ওয়েবসাইটের মুদ্রা রূপান্তরকারী। কোনো ফি বা পাঠানোর পদ্ধতি জানানো হয় না।",
  "source.AlMirqab": "Al Mirqab Exchange-এর পাবলিক রেট তালিকার DRAFT/TT রেট (কাতার)। কোনো ফি প্রকাশ করা হয় না।",
  "source.Unimoni":
    "Unimoni Oman-এর লাইভ রেট রূপান্তরকারী। সার্ভিস ফি আলাদাভাবে নেওয়া হয় কিন্তু প্রকাশ করা হয় না, এবং সব পাঠানোর পদ্ধতিতে রেট একই।",
  "source.PKExchange":
    "Purshottam Kanji Exchange-এর তারিখযুক্ত ‘Today's Exchange Rates’ পাতার TT (ব্যাংক ট্রান্সফার) রেট (ওমান), যাকে তারা আনুমানিক বলে উল্লেখ করে। কমিশন শুধু রসিদে দেখানো হয়।",
  "source.Aman":
    "Aman Exchange-এর রেট বোর্ডের ট্রান্সফার রেট (কুয়েত)। বোর্ডে তিন দিনের বেশি পুরোনো তারিখের রেটকে বর্তমান ধরা হয় না। কোনো ফি প্রকাশ করা হয় না।",
  "source.GccExchange":
    "GCC Exchange-এর ওয়েবসাইটের ক্যালকুলেটর যে পাবলিক রেট ফিড থেকে রেট নেয়, সেই রেট (সংযুক্ত আরব আমিরাত)। ক্যালকুলেটরটি সবসময় AED 0.00 ফি দেখায়, যাকে আমরা তাদের প্রকৃত ফি হিসেবে ধরি না; কোনো পাঠানোর পদ্ধতি প্রকাশ করা হয় না।",
  "source.OrientExchange":
    "Orient Exchange-এর Currency Rates পাতার তারিখযুক্ত ‘Transfer Rates’ তালিকার রেট (সংযুক্ত আরব আমিরাত)। Orient তাদের রেমিট্যান্স চার্জ ওয়েবসাইটের একটি আলাদা নথিতে দেয়, যা আমরা সংগ্রহ করি না; রেটের সঙ্গে কোনো পাঠানোর পদ্ধতি উল্লেখ নেই।",
  "source.LariExchange":
    "Lari Exchange-এর Exchange Rates পাতার তারিখযুক্ত রূপান্তরকারীর ‘Transfer Money’ রেট (সংযুক্ত আরব আমিরাত), যাকে তারা আনুমানিক বলে উল্লেখ করে। পাতায় বলা আছে যে এতে ফি ধরা নেই, এবং কোনো পাঠানোর পদ্ধতি উল্লেখ নেই।",
  "source.HadiExpress":
    "Hadi Express Exchange-এর হোম পেজের ‘Money Transfer Rate’ রূপান্তরকারী (সংযুক্ত আরব আমিরাত); তারা রেটগুলোকে আনুমানিক বলে এবং জানায় যে প্রতি ২৪ ঘণ্টায় হালনাগাদ হয়। তাদের রেমিট্যান্স চার্জ আলাদাভাবে, রেমিট্যান্স সেবার পাতার প্রশ্নোত্তর অংশে দেওয়া আছে; আমরা তা সংগ্রহ করি না, তাই পাঠানোর আগে দেখে নিন। রেটের সঙ্গে কোনো পাঠানোর পদ্ধতি বা হালনাগাদের সময় দেওয়া হয় না।",
  "source.AhaliaExchange":
    "Ahalia Exchange-এর হোম পেজের ‘Bank Transfer Rate’ বোর্ড (সংযুক্ত আরব আমিরাত), পাতার নিজস্ব স্ক্রিপ্ট যে মান দেখায় সেটিই। তারা রেটগুলোকে আনুমানিক বলে, এবং এগুলোর সঙ্গে কোনো ফি বা হালনাগাদের সময় দেয় না; গ্রাহকদের জন্য তাদের মূল তথ্য বিবরণীতে (key facts statement) ফি শুধু একটি সীমা হিসেবে দেওয়া আছে, যা আমরা সংগ্রহ করি না।",
  "source.BAExpress":
    "BA Express USA-এর পাবলিক ক্যালকুলেটর, যা প্রতিটি অঙ্কের জন্য রেট ও সার্ভিস ফি দেয়। আমরা ফি আলাদাভাবে যোগ হয় ধরে নিয়ে তা পাঠানো অঙ্ক থেকে বাদ দিই। কোনো পাঠানোর পদ্ধতি বা সময় প্রকাশ করা হয় না।",
  "source.SonaliExchange":
    "Sonali Exchange-এর ‘Exchange Rates and Tariff’ পাতায় দেওয়া ডলার থেকে টাকার রূপান্তর রেট (যুক্তরাষ্ট্র)। Sonali-র শর্তাবলিতে বলা আছে যে বর্তমানে তারা প্রেরকের কাছ থেকে কোনো ফি নেয় না; পাতাটিতে কোনো ফি, পাঠানোর পদ্ধতি বা হালনাগাদের তারিখ দেখানো হয় না।",

  "how.title": "আমরা কীভাবে রেমিট্যান্স রেট তুলনা করি",
  "how.description":
    "ratebd কীভাবে রেমিট্যান্স কোট সংগ্রহ করে, বাংলাদেশ ব্যাংকের ডলার/টাকা রেটভিত্তিক মিড-মার্কেট মানদণ্ডের তুলনায় মার্জিন হিসাব করে, এবং পুরোনো বা অনুপস্থিত তথ্য সামলায়।",

  "incentive.title": "বাংলাদেশ সরকারের রেমিট্যান্স প্রণোদনা",
  "incentive.description":
    "প্রবাসী আয়ে বাংলাদেশ সরকারের নগদ প্রণোদনা কী, কে পান এবং ratebd-এর কোটগুলোর সঙ্গে এর সম্পর্ক কী।",

  "footer.data": "প্রতিষ্ঠানগুলোর পাবলিক ক্যালকুলেটর ও রেট পাতা থেকে প্রতিদিন হালনাগাদ। এটি আর্থিক পরামর্শ নয়।",
  "footer.source": "সোর্স কোড ও তথ্য",
  "footer.independent":
    "স্বাধীন তুলনা; কোনো প্রতিষ্ঠানের সঙ্গে সম্পর্কিত নয়। প্রতিষ্ঠানগুলোর নাম তাদের নিজ নিজ মালিকের ট্রেডমার্ক।",
  "footer.routes": "প্রেরক দেশ",
  "footer.about": "তথ্য সম্পর্কে",
  "legal.title": "আইনি নোটিশ ও দায়মুক্তি",
  "legal.description":
    "ratebd কী এবং কী নয়, রেট কোথা থেকে আসে, কতটা নির্ভুল, ট্রেডমার্ক, অ্যাফিলিয়েট লিংক, গোপনীয়তা, এবং প্রতিষ্ঠানগুলো কীভাবে সংশোধন বা অপসারণের অনুরোধ করতে পারে।",
  "legal.contents": "এই পাতায়",
  "legal.updated": "সর্বশেষ হালনাগাদ {date}",
  "notFound.title": "পাতাটি পাওয়া যায়নি",
  "notFound.body": "এই পাতাটি নেই। আপনার পথের তুলনা দেখুন:",
};

const dictionaries: Record<Locale, Record<UiKey, string>> = { en, bn };

export function t(locale: Locale, key: UiKey, vars: Record<string, string | number> = {}): string {
  const template = dictionaries[locale][key] ?? en[key];
  return template.replace(/\{(\w+)\}/g, (_, name: string) => String(vars[name] ?? `{${name}}`));
}
