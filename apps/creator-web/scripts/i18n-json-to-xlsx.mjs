/* ============================================================
   i18n-json-to-xlsx — bootstrap / sync the editable workbook
   from the locale JSON the app consumes.

   Run after adding keys in code (authoring JSON directly) to
   refresh i18n.xlsx for translators:
       pnpm gen:i18n:xlsx
   The reverse (the daily flow) is i18n-xlsx-to-json.mjs.
   ============================================================ */
import { fileURLToPath } from 'node:url';
import { readFileSync } from 'node:fs';
import path from 'node:path';
import ExcelJS from 'exceljs';
import { flatten } from './i18n-flatten.mjs';

const ROOT = path.resolve(fileURLToPath(import.meta.url), '../..');
const JSON_DIR = path.join(ROOT, 'src/i18n/locales');
const XLSX_PATH = path.join(ROOT, 'i18n.xlsx');

// base locale first — its key order drives the row order in the sheet
const LOCALES = ['zh-TW', 'en'];

function loadFlat(locale) {
  const raw = JSON.parse(readFileSync(path.join(JSON_DIR, `${locale}.json`), 'utf8'));
  return flatten(raw);
}

async function main() {
  const flats = Object.fromEntries(LOCALES.map((l) => [l, loadFlat(l)]));

  // union of keys, ordered by the base locale, then any extras from others
  const keys = [];
  const seen = new Set();
  for (const locale of LOCALES) {
    for (const key of Object.keys(flats[locale])) {
      if (!seen.has(key)) {
        seen.add(key);
        keys.push(key);
      }
    }
  }

  const wb = new ExcelJS.Workbook();
  const ws = wb.addWorksheet('locales', { views: [{ state: 'frozen', ySplit: 1 }] });
  ws.columns = [
    { header: 'key', key: 'key', width: 42 },
    ...LOCALES.map((l) => ({ header: l, key: l, width: 60 })),
  ];
  ws.getRow(1).font = { bold: true };

  for (const key of keys) {
    const row = { key };
    for (const locale of LOCALES) row[locale] = flats[locale][key] ?? '';
    ws.addRow(row);
  }

  ws.eachRow((row) => {
    row.alignment = { vertical: 'top', wrapText: true };
  });

  await wb.xlsx.writeFile(XLSX_PATH);
  console.log(`✓ wrote ${path.relative(ROOT, XLSX_PATH)} — ${keys.length} keys × ${LOCALES.length} locales`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
