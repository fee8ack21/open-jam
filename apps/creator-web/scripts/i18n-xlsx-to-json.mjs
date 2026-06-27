/* ============================================================
   i18n-xlsx-to-json — the daily flow. Reads the editable
   workbook translators maintain and regenerates the locale JSON
   the app imports:
       pnpm gen:i18n

   Column layout (row 1 is the header):
       key | zh-TW | en | ...
   Each non-`key` column becomes src/i18n/locales/<header>.json.
   Numeric key segments (e.g. `hero.keywords.0`) are restored to
   arrays. Generated JSON is not hand-edited.
   ============================================================ */
import { fileURLToPath } from 'node:url';
import { writeFileSync } from 'node:fs';
import path from 'node:path';
import ExcelJS from 'exceljs';
import { unflatten } from './i18n-flatten.mjs';

const ROOT = path.resolve(fileURLToPath(import.meta.url), '../..');
const JSON_DIR = path.join(ROOT, 'src/i18n/locales');
const XLSX_PATH = path.join(ROOT, 'i18n.xlsx');

function cellText(cell) {
  const v = cell.value;
  if (v === null || v === undefined) return '';
  // exceljs may return rich-text / hyperlink objects — normalise to text
  if (typeof v === 'object') {
    if (Array.isArray(v.richText)) return v.richText.map((r) => r.text).join('');
    if ('text' in v) return String(v.text);
    if ('result' in v) return String(v.result);
  }
  return String(v);
}

async function main() {
  const wb = new ExcelJS.Workbook();
  await wb.xlsx.readFile(XLSX_PATH);
  const ws = wb.worksheets[0];

  const header = ws.getRow(1);
  const locales = []; // { name, col }
  let keyCol = null;
  header.eachCell((cell, col) => {
    const name = cellText(cell).trim();
    if (name.toLowerCase() === 'key') keyCol = col;
    else if (name) locales.push({ name, col });
  });
  if (keyCol === null) throw new Error('No "key" column found in row 1');
  if (!locales.length) throw new Error('No locale columns found in row 1');

  const flats = Object.fromEntries(locales.map((l) => [l.name, {}]));

  ws.eachRow((row, rowNumber) => {
    if (rowNumber === 1) return;
    const key = cellText(row.getCell(keyCol)).trim();
    if (!key) return;
    for (const { name, col } of locales) {
      flats[name][key] = cellText(row.getCell(col));
    }
  });

  for (const { name } of locales) {
    const tree = unflatten(flats[name]);
    const file = path.join(JSON_DIR, `${name}.json`);
    writeFileSync(file, JSON.stringify(tree, null, 2) + '\n', 'utf8');
    console.log(`✓ wrote ${path.relative(ROOT, file)} — ${Object.keys(flats[name]).length} keys`);
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
