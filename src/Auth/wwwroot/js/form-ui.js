/* form-ui.js — UI behaviours (validation is handled by jquery-validation unobtrusive) */
(function () {
  var $ = window.jQuery;

  // ---- icon helper (used by strength meter, pw-toggle, resend) ----
  // v3 貼紙 icon：完整 inner markup（筆觸內建），與 IconHelper.cs 同一套語彙
  var ICONS = {
    check:  '<path d="M4.5 12.5 l5 5 10 -11" fill="none" stroke="currentColor" stroke-width="2.8" stroke-linecap="round" stroke-linejoin="round"></path>',
    eye:    '<path d="M2.5 12 c2.5 -4.6 5.7 -6.9 9.5 -6.9 s7 2.3 9.5 6.9 c-2.5 4.6 -5.7 6.9 -9.5 6.9 s-7 -2.3 -9.5 -6.9 z" fill="#FFFFFF" stroke="currentColor" stroke-width="2"></path><circle cx="12" cy="12" r="3" fill="currentColor"></circle>',
    eyeOff: '<path d="M2.5 12 c2.5 -4.6 5.7 -6.9 9.5 -6.9 s7 2.3 9.5 6.9 c-2.5 4.6 -5.7 6.9 -9.5 6.9 s-7 -2.3 -9.5 -6.9 z" fill="#FFFFFF" stroke="currentColor" stroke-width="2"></path><circle cx="12" cy="12" r="3" fill="currentColor"></circle><path d="M4.5 20.5 L19.5 3.5" stroke="currentColor" stroke-width="2.4" stroke-linecap="round"></path>',
  };
  function icon(name, opts) {
    opts = opts || {};
    var size = opts.size == null ? 20 : opts.size;
    return (
      '<svg width="' + size + '" height="' + size + '" viewBox="0 0 24 24" ' +
      'style="display:block;flex:none;">' + (ICONS[name] || '') + '</svg>'
    );
  }
  function esc(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
  }

  // ---- password strength meter (visual only; validation enforced by jquery-validation) ----
  function strengthHTML(value) {
    if (!value) return '';
    var s = window.scorePassword(value);
    var bars = '';
    for (var i = 0; i < 4; i++) {
      bars += '<div class="pw-bar" style="background:' + (i < s.level ? s.color : 'var(--surface)') + '"></div>';
    }
    return '<div class="pw-strength"><div class="pw-bars">' + bars + '</div>' +
      '<div class="pw-meta"><span class="pw-label" style="color:' + s.color + '">' + s.text + '</span>' +
      '<span class="pw-hint">' + s.hint + '</span></div></div>';
  }

  // ---- checkbox toggle (syncs visual div with real hidden input) ----
  function toggleCheckbox(id) {
    var $input = $('#' + id);
    var newVal = !$input.prop('checked');
    // 帶 data-requires-legal 的同意勾選框：所有條款 dialog 都「點閱過」前不可勾選
    if (newVal && $input.is('[data-requires-legal]') && !allLegalRead()) {
      $('[data-legal-hint]').css('display', 'block');
      return;
    }
    $input.prop('checked', newVal);
    $('.checkbox[data-checkbox="' + id + '"]')
      .toggleClass('on', newVal)
      .attr('aria-checked', String(newVal));
    if ($input.is('[data-val]')) $input.valid();
  }

  // ---- legal modal ----
  // 條款 dialog 由 _LegalModal.cshtml 依資料庫啟用版本渲染（data-legal-card），
  // 需點閱的 key 從頁面上的 data-legal-open 收集，全部按過「我了解了」才可勾選同意。
  var legalOpen = null;
  var legalRead = {};
  function requiredLegalKeys() {
    var keys = [];
    $('[data-legal-open]').each(function () {
      var k = $(this).attr('data-legal-open');
      if (keys.indexOf(k) < 0) keys.push(k);
    });
    return keys;
  }
  function allLegalRead() {
    var keys = requiredLegalKeys();
    for (var i = 0; i < keys.length; i++) {
      if (!legalRead[keys[i]]) return false;
    }
    return true;
  }
  function openLegal(which) {
    legalOpen = which;
    $('#legal-scrim').css('display', '');
    $('.modal-card[data-legal-card]').each(function () {
      $(this).toggle($(this).attr('data-legal-card') === which);
    });
  }
  function closeLegal() {
    legalOpen = null;
    $('#legal-scrim').hide();
    $('.modal-card[data-legal-card]').hide();
  }
  function acknowledgeLegal() {
    if (legalOpen) {
      legalRead[legalOpen] = true;
      $('.read-tick[data-tick="' + legalOpen + '"]').html(
        icon('check', { size: 13, stroke: 2.8 })
      );
      if (allLegalRead()) $('[data-legal-hint]').hide();
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

  // ---- brand collage pointer parallax (mirrors portal-web hero) ----
  // Cards/shapes drift toward the cursor at per-element --depth (set in
  // _BrandPanel.cshtml), smoothed with a rAF lerp. We only write --mx/--my
  // on .collage; the compose-with-rotation transform lives in site.css.
  function collageParallax() {
    var root = document.querySelector('.collage');
    if (!root) return;
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

    var tmx = 0, tmy = 0; // target pointer offset, normalised -1..1
    var cmx = 0, cmy = 0; // current (lerped) offset
    var raf = 0;

    function onPointer(e) {
      tmx = (e.clientX / window.innerWidth) * 2 - 1;
      tmy = (e.clientY / window.innerHeight) * 2 - 1;
    }
    function tick() {
      cmx += (tmx - cmx) * 0.06;
      cmy += (tmy - cmy) * 0.06;
      root.style.setProperty('--mx', cmx.toFixed(4));
      root.style.setProperty('--my', cmy.toFixed(4));
      raf = requestAnimationFrame(tick);
    }
    window.addEventListener('pointermove', onPointer, { passive: true });
    raf = requestAnimationFrame(tick);
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
          .toggleClass('showing', !showing)
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

  }

  // ---- init ----
  $(function () {
    bind();
    collageParallax();
  });
}());
