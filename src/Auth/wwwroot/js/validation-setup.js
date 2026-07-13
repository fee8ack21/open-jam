/* validation-setup.js
   Loads AFTER jquery.validate, BEFORE jquery.validate.unobtrusive.
   Defines custom validators and wires up the design system's error display. */
(function () {
  var $ = window.jQuery;

  // ---- password strength scorer (shared with form-ui.js) ----
  // Rules mirror server-side PasswordValidator:
  //   8–20 chars · uppercase · lowercase · digit · special char · no whitespace
  window.scorePassword = function (pw) {
    pw = pw || '';
    if (!pw) return { level: 0, color: 'var(--surface)', text: '', hint: '' };

    if (pw.length > 20)
      return { level: 0, color: '#d6479b', text: '過長',  hint: '密碼最多 20 個字元' };
    if (pw.length < 8)
      return { level: 0, color: 'var(--surface)', text: '太短', hint: '至少 8 個字元' };
    if (/\s/.test(pw))
      return { level: 0, color: '#d6479b', text: '無效',  hint: '不得含空白字元' };

    var hasUpper   = /[A-Z]/.test(pw);
    var hasLower   = /[a-z]/.test(pw);
    var hasDigit   = /[0-9]/.test(pw);
    var hasSpecial = /[!"#$%&'()*+,\-./:;<=>?@[\\\]^_`{|}~]/.test(pw);

    if (!hasUpper)   return { level: 1, color: '#d6479b', text: '弱',    hint: '缺少大寫字母 (A–Z)' };
    if (!hasLower)   return { level: 1, color: '#d6479b', text: '弱',    hint: '缺少小寫字母 (a–z)' };
    if (!hasDigit)   return { level: 2, color: '#ff6b35', text: '普通',  hint: '缺少數字 (0–9)' };
    if (!hasSpecial) return { level: 2, color: '#ff6b35', text: '普通',  hint: '缺少特殊符號（例如 !@#$）' };

    return pw.length >= 16
      ? { level: 4, color: '#2e9e5b', text: '很強',   hint: '安全 👍' }
      : { level: 3, color: '#ffb020', text: '符合規則', hint: '加長密碼可更安全' };
  };

  // ---- custom validation methods ----
  $.validator.addMethod('mustbetrue', function (value, element) {
    return element.checked;
  }, '');

  // Mirrors PasswordValidator.IsValid — all criteria must be met before submit
  $.validator.addMethod('minpasswordstrength', function (value) {
    if (!value) return true; // [Required] handles empty
    if (value.length < 8 || value.length > 20) return false;
    if (!/[A-Z]/.test(value))  return false;
    if (!/[a-z]/.test(value))  return false;
    if (!/[0-9]/.test(value))  return false;
    if (!/[!"#$%&'()*+,\-./:;<=>?@[\\\]^_`{|}~]/.test(value)) return false;
    if (/\s/.test(value))      return false;
    return true;
  }, '');

  // ---- unobtrusive adapters ----
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
