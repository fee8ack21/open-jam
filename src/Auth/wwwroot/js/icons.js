/* June Auth — icon set (jQuery / string-based) */
(function () {
  const PATHS = {
    mail: 'M3 5h18v14H3zM3 6l9 7 9-7',
    lock: 'M6 11V8a6 6 0 0 1 12 0v3M5 11h14v9H5z',
    user: 'M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM4 21a8 8 0 0 1 16 0',
    eye: 'M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7z M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z',
    eyeOff: 'M3 3l18 18 M10.6 10.6a3 3 0 0 0 4 4 M9.4 5.2A9.7 9.7 0 0 1 12 5c6.5 0 10 7 10 7a16 16 0 0 1-3.3 4 M6.3 6.3A16 16 0 0 0 2 12s3.5 7 10 7a9.7 9.7 0 0 0 3.3-.6',
    check: 'M5 12.5l4.5 4.5L19 7',
    arrowLeft: 'M19 12H5M11 6l-6 6 6 6',
    alert: 'M12 8v5M12 16.5v.5 M10.3 3.8 2.4 18a1.9 1.9 0 0 0 1.7 2.9h15.8a1.9 1.9 0 0 0 1.7-2.9L13.7 3.8a1.9 1.9 0 0 0-3.4 0z',
    shield: 'M12 3l8 3v5c0 5-3.4 8.5-8 10-4.6-1.5-8-5-8-10V6l8-3z',
    sparkle: 'M12 3l1.8 5.2L19 10l-5.2 1.8L12 17l-1.8-5.2L5 10l5.2-1.8L12 3z',
    star: 'M12 3.5l2.6 5.3 5.9.9-4.2 4.1 1 5.8L12 17l-5.3 2.8 1-5.8L3.5 9.7l5.9-.9L12 3.5z',
    note: 'M9 18V6l10-2v12M9 13l10-2M9 18a2.5 2.5 0 1 1-5 0 2.5 2.5 0 0 1 5 0zM19 16a2.5 2.5 0 1 1-5 0 2.5 2.5 0 0 1 5 0z',
    image: 'M4 5h16v14H4zM4 15l4-4 4 4 3-3 5 5M9 9.5a1.5 1.5 0 1 1-3 0 1.5 1.5 0 0 1 3 0z',
    book: 'M4 5a2 2 0 0 1 2-2h13v16H6a2 2 0 0 0-2 2zM4 19a2 2 0 0 1 2-2h13',
    send: 'M22 2 11 13 M22 2 15 22l-4-9-9-4 20-7z',
    key: 'M15 7a4 4 0 1 1-3.9 5H8v2H6v2H3v-3l5.1-5.1A4 4 0 0 1 15 7zM16 8h.01',
    arrowRight: 'M5 12h14M13 6l6 6-6 6',
  };

  // window.icon(name, { size, fill, stroke, style }) -> SVG HTML string
  window.icon = function (name, opts) {
    opts = opts || {};
    const size = opts.size == null ? 20 : opts.size;
    const fill = !!opts.fill;
    const stroke = opts.stroke == null ? 1.9 : opts.stroke;
    const d = PATHS[name] || '';
    const styleStr = 'display:block;flex:none;' + (opts.style || '');
    return (
      '<svg width="' + size + '" height="' + size + '" viewBox="0 0 24 24" ' +
      'fill="' + (fill ? 'currentColor' : 'none') + '" ' +
      'stroke="' + (fill ? 'none' : 'currentColor') + '" ' +
      'stroke-width="' + (fill ? 0 : stroke) + '" ' +
      'stroke-linecap="round" stroke-linejoin="round" style="' + styleStr + '">' +
      '<path d="' + d + '"></path>' +
      '</svg>'
    );
  };

  // Open Jam brand mark — eighth note that reads as a "J"
  window.brandMark = function (size) {
    size = size || 17;
    return (
      '<svg width="' + size + '" height="' + size + '" viewBox="0 0 24 24" fill="none">' +
      '<path d="M15 16.4V4.5c3.7 1 5 3.9 2 6.8" stroke="currentColor" stroke-width="2.3" ' +
      'stroke-linecap="round" stroke-linejoin="round" fill="none"></path>' +
      '<ellipse cx="10.4" cy="16.8" rx="4.7" ry="3.5" fill="currentColor" transform="rotate(-22 10.4 16.8)"></ellipse>' +
      '</svg>'
    );
  };

  // small HTML-escape helper shared across modules
  window.esc = function (s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
  };
})();
