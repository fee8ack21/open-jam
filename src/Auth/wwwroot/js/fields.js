/* June Auth — form field primitives (jQuery / string builders) */
(function () {
  const icon = window.icon;
  const esc = window.esc;

  // ---- error span ----
  function errHTML(msg) {
    if (!msg) return '';
    return '<span class="field-err">' + icon('alert', { size: 13 }) + ' ' + esc(msg) + '</span>';
  }
  window.fieldErrHTML = errHTML;

  // ---- text/email field ----
  // o: { id, label, type, icon, value, placeholder, error, autoComplete, labelRight }
  window.fieldHTML = function (o) {
    const lead = o.icon ? '<span class="lead">' + icon(o.icon, { size: 18 }) + '</span>' : '';
    const labelInner = o.labelRight
      ? '<span>' + o.label + '</span>' + o.labelRight
      : o.label;
    return (
      '<div class="field" data-field="' + o.id + '">' +
        '<label class="field-label">' + labelInner + '</label>' +
        '<div class="input-shell' + (o.error ? ' err' : '') + '">' +
          lead +
          '<input type="' + (o.type || 'text') + '" data-input="' + o.id + '" ' +
            'value="' + esc(o.value || '') + '" placeholder="' + esc(o.placeholder || '') + '" ' +
            'autocomplete="' + (o.autoComplete || 'off') + '">' +
        '</div>' +
        errHTML(o.error) +
      '</div>'
    );
  };

  // ---- password strength block ----
  window.strengthHTML = function (value) {
    if (!value) return '';
    const s = window.scorePassword(value);
    let bars = '';
    for (let i = 0; i < 4; i++) {
      bars += '<div class="pw-bar" style="background:' + (i < s.level ? s.color : 'var(--border)') + '"></div>';
    }
    return (
      '<div class="pw-strength">' +
        '<div class="pw-bars">' + bars + '</div>' +
        '<div class="pw-meta">' +
          '<span class="pw-label" style="color:' + s.color + '">' + s.text + '</span>' +
          '<span class="pw-hint">' + s.hint + '</span>' +
        '</div>' +
      '</div>'
    );
  };

  // ---- password field w/ show/hide + optional strength meter ----
  // o: { id, label, value, placeholder, error, showStrength, autoComplete, labelRight, show }
  window.passwordFieldHTML = function (o) {
    const labelInner = '<span>' + o.label + '</span>' + (o.labelRight || '');
    const type = o.show ? 'text' : 'password';
    const toggleIcon = o.show ? 'eyeOff' : 'eye';
    const toggleTitle = o.show ? '隱藏密碼' : '顯示密碼';
    const strength = o.showStrength ? window.strengthHTML(o.value) : '';
    return (
      '<div class="field" data-field="' + o.id + '"' + (o.showStrength ? ' data-strength="1"' : '') + '>' +
        '<label class="field-label">' + labelInner + '</label>' +
        '<div class="input-shell' + (o.error ? ' err' : '') + '">' +
          '<span class="lead">' + icon('lock', { size: 18 }) + '</span>' +
          '<input type="' + type + '" data-input="' + o.id + '" data-pw="1" ' +
            'value="' + esc(o.value || '') + '" placeholder="' + esc(o.placeholder || '') + '" ' +
            'autocomplete="' + (o.autoComplete || 'off') + '">' +
          '<button type="button" class="pw-toggle" data-pwtoggle="' + o.id + '" title="' + toggleTitle + '">' +
            icon(toggleIcon, { size: 18 }) +
          '</button>' +
        '</div>' +
        strength +
        errHTML(o.error) +
      '</div>'
    );
  };

  // ---- checkbox ----
  // o: { id, checked, label }  (label is raw HTML)
  window.checkboxHTML = function (o) {
    return (
      '<div class="checkbox' + (o.checked ? ' on' : '') + '" data-checkbox="' + o.id + '" ' +
        'role="checkbox" aria-checked="' + (!!o.checked) + '" tabindex="0">' +
        '<span class="box">' + icon('check', { size: 13, stroke: 2.6 }) + '</span>' +
        '<span>' + o.label + '</span>' +
      '</div>'
    );
  };

  // ---- password scoring ----
  window.scorePassword = function (pw) {
    pw = pw || '';
    let score = 0;
    if (pw.length >= 8) score++;
    if (/[A-Z]/.test(pw) && /[a-z]/.test(pw)) score++;
    if (/\d/.test(pw)) score++;
    if (/[^A-Za-z0-9]/.test(pw)) score++;
    if (pw.length >= 12 && score >= 3) score = 4;
    const map = [
      { level: 0, color: 'var(--border)', text: '太短', hint: '至少 8 個字元' },
      { level: 1, color: '#ff4d9d', text: '弱', hint: '加入大小寫' },
      { level: 2, color: '#ff7a2f', text: '普通', hint: '加入數字' },
      { level: 3, color: '#ffc83a', text: '不錯', hint: '加入符號更強' },
      { level: 4, color: '#11a36a', text: '很強', hint: '安全 👍' },
    ];
    return map[Math.min(score, 4)];
  };

  // ---- validators ----
  window.isEmail = function (v) { return /^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(v || ''); };
})();
