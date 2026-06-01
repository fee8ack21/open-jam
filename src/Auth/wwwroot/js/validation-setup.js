/* validation-setup.js
   Loads AFTER jquery.validate, BEFORE jquery.validate.unobtrusive.
   Defines custom validators and wires up the design system's error display. */
(function () {
  var $ = window.jQuery;

  // ---- password strength scorer (shared with auth-core.js) ----
  window.scorePassword = function (pw) {
    pw = pw || '';
    var score = 0;
    if (pw.length >= 8) score++;
    if (/[A-Z]/.test(pw) && /[a-z]/.test(pw)) score++;
    if (/\d/.test(pw)) score++;
    if (/[^A-Za-z0-9]/.test(pw)) score++;
    if (pw.length >= 12 && score >= 3) score = 4;
    return [
      { level: 0, color: 'var(--border)',  text: '太短',   hint: '至少 8 個字元' },
      { level: 1, color: '#ff4d9d',        text: '弱',     hint: '加入大小寫' },
      { level: 2, color: '#ff7a2f',        text: '普通',   hint: '加入數字' },
      { level: 3, color: '#ffc83a',        text: '不錯',   hint: '加入符號更強' },
      { level: 4, color: '#11a36a',        text: '很強',   hint: '安全 👍' },
    ][Math.min(score, 4)];
  };

  // ---- custom validation methods ----
  $.validator.addMethod('mustbetrue', function (value, element) {
    return element.checked;
  }, '');

  $.validator.addMethod('minpasswordstrength', function (value) {
    return !value || window.scorePassword(value).level >= 2;
  }, '');

  // ---- unobtrusive adapters ----
  // addBool reads a sub-attribute, which we don't have; use add() with empty params
  // so the rule is simply enabled (true) and the message comes from the attribute value.
  $.validator.unobtrusive.adapters.add('mustbetrue', [], function (options) {
    options.rules['mustbetrue'] = true;
    options.messages['mustbetrue'] = options.message;
  });
  $.validator.unobtrusive.adapters.add('minpasswordstrength', [], function (options) {
    options.rules['minpasswordstrength'] = true;
    options.messages['minpasswordstrength'] = options.message;
  });

  // ---- design system integration ----
  $.validator.setDefaults({
    highlight: function (element) {
      $(element).closest('.input-shell').addClass('err');
    },
    unhighlight: function (element) {
      $(element).closest('.input-shell').removeClass('err');
    },
    errorElement: 'span',
    errorClass: '',
  });
}());
