/* auth-core.js — UI behaviours (validation is handled by jquery-validation unobtrusive) */
(function () {
  var $ = window.jQuery;

  // ---- icon helper (used by strength meter, pw-toggle, resend) ----
  var PATHS = {
    check:   'M5 12.5l4.5 4.5L19 7',
    alert:   'M12 8v5M12 16.5v.5 M10.3 3.8 2.4 18a1.9 1.9 0 0 0 1.7 2.9h15.8a1.9 1.9 0 0 0 1.7-2.9L13.7 3.8a1.9 1.9 0 0 0-3.4 0z',
    eye:     'M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7z M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z',
    eyeOff:  'M3 3l18 18 M10.6 10.6a3 3 0 0 0 4 4 M9.4 5.2A9.7 9.7 0 0 1 12 5c6.5 0 10 7 10 7a16 16 0 0 1-3.3 4 M6.3 6.3A16 16 0 0 0 2 12s3.5 7 10 7a9.7 9.7 0 0 0 3.3-.6',
  };
  function icon(name, opts) {
    opts = opts || {};
    var size   = opts.size   == null ? 20 : opts.size;
    var stroke = opts.stroke == null ? 1.9 : opts.stroke;
    var d = PATHS[name] || '';
    return (
      '<svg width="' + size + '" height="' + size + '" viewBox="0 0 24 24" ' +
      'fill="none" stroke="currentColor" stroke-width="' + stroke + '" ' +
      'stroke-linecap="round" stroke-linejoin="round" style="display:block;flex:none;">' +
      '<path d="' + d + '"></path></svg>'
    );
  }
  function esc(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
  }

  // ---- accent gradients ----
  var ACCENT_GRAD = {
    violet: 'radial-gradient(620px 460px at 12% 6%, rgba(255,200,58,.42), transparent 58%), radial-gradient(680px 520px at 92% 18%, rgba(255,77,157,.55), transparent 60%), radial-gradient(720px 620px at 78% 96%, rgba(31,214,198,.42), transparent 62%), radial-gradient(820px 720px at 18% 92%, rgba(108,76,241,.55), transparent 64%), linear-gradient(150deg, #6c4cf1, #8a3df1 46%, #c33ad6)',
    sunset: 'radial-gradient(620px 460px at 12% 6%, rgba(255,200,58,.5), transparent 58%), radial-gradient(680px 520px at 92% 18%, rgba(255,77,157,.55), transparent 60%), radial-gradient(720px 520px at 78% 96%, rgba(255,122,47,.5), transparent 62%), radial-gradient(820px 720px at 18% 92%, rgba(255,77,157,.5), transparent 64%), linear-gradient(150deg, #ff7a2f, #ff5a6e 50%, #ff4d9d)',
    ocean:  'radial-gradient(620px 460px at 12% 6%, rgba(174,240,62,.4), transparent 58%), radial-gradient(680px 520px at 92% 18%, rgba(31,214,198,.55), transparent 60%), radial-gradient(720px 620px at 78% 96%, rgba(108,76,241,.5), transparent 62%), radial-gradient(820px 720px at 18% 92%, rgba(31,214,198,.5), transparent 64%), linear-gradient(150deg, #1fd6c6, #2f8ff1 50%, #6c4cf1)',
  };

  // ---- prefs (localStorage) ----
  var prefs = { font: 'bricolage', accent: 'violet' };
  function loadPrefs() {
    try {
      var p = JSON.parse(localStorage.getItem('ojAuthPrefs') || '{}');
      if (p.font)   prefs.font   = p.font;
      if (p.accent) prefs.accent = p.accent;
    } catch (e) { /* ignore */ }
  }
  function savePrefs() {
    try { localStorage.setItem('ojAuthPrefs', JSON.stringify(prefs)); } catch (e) { /* ignore */ }
  }
  function applyPrefs() {
    $('#auth-shell').removeClass('font-bricolage font-unbounded').addClass('font-' + prefs.font);
    $('#accent-style').html('.brand-panel{background:' + ACCENT_GRAD[prefs.accent] + ' !important;} .mobile-brand .brand-mark svg{color:#fff;}');
    $('.seg button').removeClass('on');
    $('.seg button[data-font="' + prefs.font + '"]').addClass('on');
    $('.swatch').removeClass('on');
    $('.swatch[data-accent="' + prefs.accent + '"]').addClass('on');
  }

  // ---- password strength meter (visual only; validation enforced by jquery-validation) ----
  function strengthHTML(value) {
    if (!value) return '';
    var s = window.scorePassword(value);
    var bars = '';
    for (var i = 0; i < 4; i++) {
      bars += '<div class="pw-bar" style="background:' + (i < s.level ? s.color : 'var(--border)') + '"></div>';
    }
    return '<div class="pw-strength"><div class="pw-bars">' + bars + '</div>' +
      '<div class="pw-meta"><span class="pw-label" style="color:' + s.color + '">' + s.text + '</span>' +
      '<span class="pw-hint">' + s.hint + '</span></div></div>';
  }

  // ---- checkbox toggle (syncs visual div with real hidden input) ----
  function toggleCheckbox(id) {
    var $input = $('#' + id);
    var newVal = !$input.prop('checked');
    $input.prop('checked', newVal);
    $('.checkbox[data-checkbox="' + id + '"]')
      .toggleClass('on', newVal)
      .attr('aria-checked', String(newVal));
    if ($input.is('[data-val]')) $input.valid();
  }

  // ---- legal modal ----
  var legalOpen = null;
  var legalRead = { terms: false, privacy: false };
  function openLegal(which) {
    legalOpen = which;
    $('#legal-scrim').css('display', '');
    $('#legal-terms-card').toggle(which === 'terms');
    $('#legal-privacy-card').toggle(which === 'privacy');
  }
  function closeLegal() {
    legalOpen = null;
    $('#legal-scrim').hide();
    $('#legal-terms-card, #legal-privacy-card').hide();
  }
  function acknowledgeLegal() {
    if (legalOpen) {
      legalRead[legalOpen] = true;
      $('.read-tick[data-tick="' + legalOpen + '"]').html(
        icon('check', { size: 13, stroke: 2.8 })
      );
    }
    closeLegal();
  }

  // ---- resend timer (on sent screens) ----
  var resendLeft = 45;
  var resendTimer = null;
  function resendHTML() {
    var mm = Math.floor(resendLeft / 60);
    var ss = String(resendLeft % 60).padStart(2, '0');
    if (resendLeft > 0) return '可於 <b>' + mm + ':' + ss + '</b> 後重新寄送';
    return '<button class="link-strong" data-resend>重新寄送至此信箱</button>';
  }
  function startResendTimer($slot) {
    resendLeft = 45;
    if (resendTimer) clearInterval(resendTimer);
    resendTimer = setInterval(function () {
      if (resendLeft > 0) resendLeft--;
      $slot.html(resendHTML());
      if (resendLeft <= 0) clearInterval(resendTimer);
    }, 1000);
  }

  // ---- event binding ----
  function bind() {
    var $d = $(document);

    // password strength meter
    $d.on('input', 'input[data-input]', function () {
      var $field = $(this).closest('.field');
      if ($field.attr('data-strength')) {
        $field.find('.pw-strength').remove();
        var h = strengthHTML(this.value);
        if (h) $field.find('.input-shell').after(h);
      }
    });

    // password visibility toggle
    $d.on('click', '[data-pwtoggle]', function () {
      var $btn   = $(this);
      var $input = $('.field[data-field="' + $btn.attr('data-pwtoggle') + '"] input[data-pw]');
      var showing = $input.attr('type') === 'text';
      $input.attr('type', showing ? 'password' : 'text');
      $btn.attr('title', showing ? '顯示密碼' : '隱藏密碼')
          .html(icon(showing ? 'eye' : 'eyeOff', { size: 18 }));
    });

    // custom checkbox
    $d.on('click', '[data-checkbox]', function (e) {
      if ($(e.target).closest('a, button').length) return;
      toggleCheckbox($(this).attr('data-checkbox'));
    });
    $d.on('keydown', '[data-checkbox]', function (e) {
      if (e.key === ' ' || e.key === 'Enter') {
        e.preventDefault();
        toggleCheckbox($(this).attr('data-checkbox'));
      }
    });

    // legal modal
    $d.on('click', '[data-legal-open]',  function (e) { e.stopPropagation(); openLegal($(this).attr('data-legal-open')); });
    $d.on('click', '[data-legal-close]', function ()  { closeLegal(); });
    $d.on('click', '[data-legal-ack]',   function ()  { acknowledgeLegal(); });
    $d.on('click', '[data-legal-scrim]', function (e) { if (e.target === this) closeLegal(); });
    $d.on('keydown', function (e) { if (e.key === 'Escape' && legalOpen) closeLegal(); });

    // resend timer on sent screens
    var $resendContainer = $('[data-resend-screen]');
    if ($resendContainer.length) {
      var $slot = $resendContainer.find('.resend-slot');
      startResendTimer($slot);
      $d.on('click', '[data-resend]', function () {
        if (resendLeft > 0) return;
        $slot.html('<span class="resend-ok">' + icon('check', { size: 13, stroke: 2.6 }) + ' 已重新寄送</span>');
        setTimeout(function () { startResendTimer($slot); }, 2600);
      });
    }

    // tweaks panel
    $d.on('click', '[data-tweaks-close]', function () {
      $('#tweaks-panel').hide();
      window.parent.postMessage({ type: '__edit_mode_dismissed' }, '*');
    });
    $d.on('click', '[data-font]', function () {
      prefs.font = $(this).attr('data-font');
      savePrefs();
      $('#auth-shell').removeClass('font-bricolage font-unbounded').addClass('font-' + prefs.font);
      $('.seg button').removeClass('on');
      $('.seg button[data-font="' + prefs.font + '"]').addClass('on');
    });
    $d.on('click', '[data-accent]', function () {
      prefs.accent = $(this).attr('data-accent');
      savePrefs();
      $('#accent-style').html('.brand-panel{background:' + ACCENT_GRAD[prefs.accent] + ' !important;}');
      $('.swatch').removeClass('on');
      $('.swatch[data-accent="' + prefs.accent + '"]').addClass('on');
    });

    window.addEventListener('message', function (e) {
      var t = e && e.data && e.data.type;
      if (t === '__activate_edit_mode')        $('#tweaks-panel').css('display', '');
      else if (t === '__deactivate_edit_mode') $('#tweaks-panel').hide();
    });
    window.parent.postMessage({ type: '__edit_mode_available' }, '*');
  }

  // ---- init ----
  $(function () {
    loadPrefs();
    applyPrefs();
    bind();
  });
}());
