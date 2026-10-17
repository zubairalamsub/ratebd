// @ts-check
import { defineConfig } from "astro/config";
import sitemap from "@astrojs/sitemap";
import tailwindcss from "@tailwindcss/vite";
import { fileURLToPath } from "node:url";
import { goRedirects } from "./integrations/go-redirects.mjs";

// The Cloudflare Pages *.pages.dev URL until a custom domain is attached. Override with SITE_URL.
const site = process.env.SITE_URL ?? "https://ratebd.pages.dev";

// /data lives at the repository root, next to /site. Cloudflare Pages clones the whole repo, so it is
// available at build time.
const dataDir = process.env.RATEBD_DATA_DIR ?? fileURLToPath(new URL("../data/", import.meta.url));

export default defineConfig({
  site,
  // Pages are emitted as foo.html and served by Cloudflare Pages at /foo, so canonical URLs have no trailing slash.
  trailingSlash: "never",
  build: {
    format: "file",
    // Tailwind's output is small; inlining it removes the only render-blocking request.
    inlineStylesheets: "always",
  },
  i18n: {
    defaultLocale: "en",
    locales: ["en", "bn"],
    routing: { prefixDefaultLocale: false },
  },
  integrations: [
    sitemap({
      i18n: { defaultLocale: "en", locales: { en: "en-GB", bn: "bn-BD" } },
    }),
    goRedirects(),
  ],
  vite: {
    plugins: [tailwindcss()],
    define: {
      __RATEBD_DATA_DIR__: JSON.stringify(dataDir),
    },
  },
});
