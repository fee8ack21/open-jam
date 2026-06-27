/* ============================================================
   i18n-flatten — shared helpers for the locale Excel <-> JSON
   round-trip. Flattens nested message trees to dot-keyed rows
   (arrays become numeric segments, e.g. `hero.keywords.0`) and
   rebuilds the nested tree, restoring arrays from numeric keys.
   ============================================================ */

/** Flatten a nested message object into a `{ "a.b.0": value }` map. */
export function flatten(node, prefix = '', out = {}) {
  if (Array.isArray(node)) {
    node.forEach((item, i) => flatten(item, prefix ? `${prefix}.${i}` : String(i), out));
  } else if (node !== null && typeof node === 'object') {
    for (const [key, value] of Object.entries(node)) {
      flatten(value, prefix ? `${prefix}.${key}` : key, out);
    }
  } else {
    out[prefix] = node;
  }
  return out;
}

/** Recursively turn objects whose keys are exactly 0..n-1 into arrays. */
function arraify(node) {
  if (node === null || typeof node !== 'object') return node;
  const keys = Object.keys(node);
  const isArray =
    keys.length > 0 && keys.every((k, i) => k === String(i));
  if (isArray) {
    return keys.map((k) => arraify(node[k]));
  }
  const out = {};
  for (const k of keys) out[k] = arraify(node[k]);
  return out;
}

/** Rebuild a nested message tree from a flat `{ "a.b.0": value }` map. */
export function unflatten(flat) {
  const root = {};
  for (const [path, value] of Object.entries(flat)) {
    const segments = path.split('.');
    let cursor = root;
    segments.forEach((seg, i) => {
      if (i === segments.length - 1) {
        cursor[seg] = value;
      } else {
        if (typeof cursor[seg] !== 'object' || cursor[seg] === null) cursor[seg] = {};
        cursor = cursor[seg];
      }
    });
  }
  return arraify(root);
}
