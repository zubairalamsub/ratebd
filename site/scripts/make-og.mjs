// Renders public/og.png (1200×630) for social previews. Run with `npm run og` after changing the design;
// the PNG is committed so builds do not depend on system fonts.
import sharp from "sharp";
import { fileURLToPath } from "node:url";

const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="630" viewBox="0 0 1200 630">
  <rect width="1200" height="630" fill="#005c41"/>
  <circle cx="1010" cy="315" r="190" fill="#c8102e"/>
  <text x="80" y="200" font-family="Segoe UI, Arial, sans-serif" font-size="96" font-weight="700" fill="#ffffff">ratebd</text>
  <text x="80" y="300" font-family="Segoe UI, Arial, sans-serif" font-size="52" fill="#ffffff">Compare money transfer</text>
  <text x="80" y="370" font-family="Segoe UI, Arial, sans-serif" font-size="52" fill="#ffffff">rates to Bangladesh</text>
  <text x="80" y="480" font-family="Segoe UI, Arial, sans-serif" font-size="34" fill="#cce8dc">6 sending countries · updated daily</text>
</svg>`;

const out = fileURLToPath(new URL("../public/og.png", import.meta.url));
await sharp(Buffer.from(svg)).png({ compressionLevel: 9 }).toFile(out);
console.log(`wrote ${out}`);
