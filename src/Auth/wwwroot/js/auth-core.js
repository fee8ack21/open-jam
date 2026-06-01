/* June Auth — event controller (jQuery).
   Razor views render the initial HTML; this file handles interactions
   and renders the 5 post-submit screens into .form-wrap. */
(function () {
  const $ = window.jQuery;

  // ---- minimal SVG icon helper (used only in post-submit screens + field errors + pw-toggle) ----
  const PATHS = {
    mail:       'M3 5h18v14H3zM3 6l9 7 9-7',
    check:      'M5 12.5l4.5 4.5L19 7',
    arrowRight: 'M5 12h14M13 6l6 6-6 6',
    alert:      'M12 8v5M12 16.5v.5 M10.3 3.8 2.4 18a1.9 1.9 0 0 0 1.7 2.9h15.8a1.9 1.9 0 0 0 1.7-2.9L13.7 3.8a1.9 1.9 0 0 0-3.4 0z',
    shield:     'M12 3l8 3v5c0 5-3.4 8.5-8 10-4.6-1.5-8-5-8-10V6l8-3z',
    eye:        'M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7z M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z',
    eyeOff:     'M3 3l18 18 M10.6 10.6a3 3 0 0 0 4 4 M9.4 5.2A9.7 9.7 0 0 1 12 5c6.5 0 10 7 10 7a16 16 0 0 1-3.3 4 M6.3 6.3A16 16 0 0 0 2 12s3.5 7 10 7a9.7 9.7 0 0 0 3.3-.6',
  };
  function icon(name, opts) {
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
      '<path d="' + d + '"></path></svg>'
    );
  }
  function esc(s) {
    return String(s == null ? '' : s)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
  }

  // ---- accent gradients ----
  const ACCENT_GRAD = {
    violet: 'radial-gradient(620px 460px at 12% 6%, rgba(255,200,58,.42), transparent 58%), radial-gradient(680px 520px at 92% 18%, rgba(255,77,157,.55), transparent 60%), radial-gradient(720px 620px at 78% 96%, rgba(31,214,198,.42), transparent 62%), radial-gradient(820px 720px at 18% 92%, rgba(108,76,241,.55), transparent 64%), linear-gradient(150deg, #6c4cf1, #8a3df1 46%, #c33ad6)',
    sunset: 'radial-gradient(620px 460px at 12% 6%, rgba(255,200,58,.5), transparent 58%), radial-gradient(680px 520px at 92% 18%, rgba(255,77,157,.55), transparent 60%), radial-gradient(720px 520px at 78% 96%, rgba(255,122,47,.5), transparent 62%), radial-gradient(820px 720px at 18% 92%, rgba(255,77,157,.5), transparent 64%), linear-gradient(150deg, #ff7a2f, #ff5a6e 50%, #ff4d9d)',
    ocean:  'radial-gradient(620px 460px at 12% 6%, rgba(174,240,62,.4), transparent 58%), radial-gradient(680px 520px at 92% 18%, rgba(31,214,198,.55), transparent 60%), radial-gradient(720px 620px at 78% 96%, rgba(108,76,241,.5), transparent 62%), radial-gradient(820px 720px at 18% 92%, rgba(31,214,198,.5), transparent 64%), linear-gradient(150deg, #1fd6c6, #2f8ff1 50%, #6c4cf1)',
  };

  // ---- state ----
  const state = {
    screen: 'login', form: {}, show: {}, loading: false,
    font: 'bricolage', accent: 'violet',
    legal: null, read: { terms: false, privacy: false },
    left: 45, justSent: false,
    data: {},
  };

  // ---- prefs (localStorage) ----
  function loadPrefs() {
    try {
      const p = JSON.parse(localStorage.getItem('ojAuthPrefs') || '{}');
      if (p.font)   state.font   = p.font;
      if (p.accent) state.accent = p.accent;
    } catch (e) { /* ignore */ }
  }
  function savePrefs() {
    try { localStorage.setItem('ojAuthPrefs', JSON.stringify({ font: state.font, accent: state.accent })); } catch (e) { /* ignore */ }
  }

  // ---- apply prefs to DOM ----
  function applyPrefs() {
    $('#auth-shell').removeClass('font-bricolage font-unbounded').addClass('font-' + state.font);
    $('#accent-style').html('.brand-panel{background:' + ACCENT_GRAD[state.accent] + ' !important;} .mobile-brand .brand-mark svg{color:#fff;}');
    $('.seg button').removeClass('on');
    $('.seg button[data-font="' + state.font + '"]').addClass('on');
    $('.swatch').removeClass('on');
    $('.swatch[data-accent="' + state.accent + '"]').addClass('on');
  }

  // ---- page navigation ----
  const PAGE_URL = { login: '/login', register: '/register', forgot: '/forgot', reset: '/reset' };

  function navigate(screen, data) {
    if (data && data.email != null) {
      try { sessionStorage.setItem('ojAuthEmail', data.email); } catch (e) { /* ignore */ }
    }
    if (POST_SCREENS[screen]) {
      state.screen = screen;
      if (data) state.data = { email: data.email || state.data.email };
      if (screen === 'forgot-sent' || screen === 'register-sent') startTimer();
      $('.form-wrap').html(POST_SCREENS[screen]());
      return;
    }
    window.location.href = PAGE_URL[screen] || '/login';
  }

  // ---- resend countdown ----
  let timer = null;
  function clearTimer() { if (timer) { clearInterval(timer); timer = null; } }
  function startTimer() {
    state.left = 45;
    clearTimer();
    timer = setInterval(function () {
      if (state.left > 0) state.left--;
      $('.resend').html(resendHTML());
      if (state.left <= 0) clearTimer();
    }, 1000);
  }
  function resendHTML() {
    const mm = Math.floor(state.left / 60);
    const ss = String(state.left % 60).padStart(2, '0');
    const isReg = state.screen === 'register-sent';
    const lead  = isReg ? '沒收到信？' : '沒收到？';
    const label = isReg ? '重新寄送確認信' : '重新寄送至此信箱';
    if (state.justSent) return '<span class="resend-ok">' + icon('check', { size: 13, stroke: 2.6 }) + ' 已重新寄送至此信箱</span>';
    if (state.left > 0)  return lead + '可於 <b>' + mm + ':' + ss + '</b> 後重新寄送';
    return lead + '<button class="link-strong" data-resend>' + label + '</button>';
  }

  // ---- post-submit screens (JS-rendered into .form-wrap) ----
  function forgotSentHTML() {
    return '<div class="screen">' +
      '<div class="success-ring violet">' + icon('mail', { size: 40 }) + '</div>' +
      '<h2 class="form-title">信件已寄出！</h2>' +
      '<p class="form-sub">我們把重置連結寄到了：</p>' +
      '<div class="email-chip">' + icon('mail', { size: 16 }) + ' ' + esc(state.data.email) + '</div>' +
      '<p class="form-sub" style="margin-top:18px">打開信件並點擊連結即可設定新密碼。沒看到？記得檢查垃圾信匣。</p>' +
      '<div class="form-body">' +
        '<button class="btn-pop violet" data-go="reset">' + icon('arrowRight', { size: 17 }) + ' 我收到連結了，去重置</button>' +
        '<button class="btn-ghost" data-go="forgot">換一個信箱</button>' +
      '</div>' +
      '<p class="resend" style="text-align:center">' + resendHTML() + '</p>' +
    '</div>';
  }

  function registerSentHTML() {
    return '<div class="screen">' +
      '<div class="success-ring cyan">' + icon('mail', { size: 40 }) + '</div>' +
      '<h2 class="form-title">確認你的信箱</h2>' +
      '<p class="form-sub">帳號快好了！我們寄了一封確認信到：</p>' +
      '<div class="email-chip">' + icon('mail', { size: 16 }) + ' ' + esc(state.data.email) + '</div>' +
      '<p class="form-sub" style="margin-top:18px">點擊信中的按鈕完成驗證，就能開始上架你的第一件作品。</p>' +
      '<div class="form-body">' +
        '<button class="btn-pop" data-go="login">' + icon('check', { size: 18, stroke: 2.4 }) + ' 我驗證好了，去登入</button>' +
        '<button class="btn-ghost" data-go="register">換一個信箱</button>' +
      '</div>' +
      '<p class="resend" style="text-align:center">' + resendHTML() + '</p>' +
    '</div>';
  }

  function resetDoneHTML() {
    return '<div class="screen">' +
      '<div class="success-ring">' + icon('check', { size: 46, stroke: 2.6 }) + '</div>' +
      '<h2 class="form-title">密碼已更新 🎉</h2>' +
      '<p class="form-sub">你的密碼已成功重置，現在可以用新密碼登入 Open Jam 了。</p>' +
      '<div class="form-body">' +
        '<button class="btn-pop violet" data-go="login">' + icon('arrowRight', { size: 17 }) + ' 前往登入</button>' +
      '</div>' +
    '</div>';
  }

  function loginDoneHTML() {
    return '<div class="screen">' +
      '<div class="success-ring">' + icon('check', { size: 46, stroke: 2.6 }) + '</div>' +
      '<h2 class="form-title">登入成功 🎉</h2>' +
      '<p class="form-sub">歡迎回來，<b>' + esc(state.data.email) + '</b>。正在帶你前往創作工作室…</p>' +
      '<div class="form-body">' +
        '<div class="btn-pop violet" style="cursor:default"><span class="spinner"></span> 載入工作室…</div>' +
      '</div>' +
    '</div>';
  }

  function loginErrorHTML() {
    return '<div class="screen">' +
      '<div class="success-ring danger">' + icon('alert', { size: 42, stroke: 2.2 }) + '</div>' +
      '<h2 class="form-title">登入沒有完成</h2>' +
      '<p class="form-sub">我們無法完成這次的第三方登入驗證。多半是授權逾時，或瀏覽器在分頁之間遺失了登入狀態所致 — 你的帳號是安全的。</p>' +
      '<div class="callout">' + icon('shield', { size: 18 }) +
        '<span><b>請完全關閉瀏覽器</b>，重新開啟後再試一次登入。這會清除殘留的登入狀態，通常就能解決問題。</span>' +
      '</div>' +
      '<div class="form-body">' +
        '<button class="btn-pop violet" data-go="login">' + icon('arrowRight', { size: 17 }) + ' 重新登入</button>' +
        '<button class="btn-ghost" data-go="forgot">改用密碼登入</button>' +
      '</div>' +
      '<p class="err-code"><span class="dot"></span>錯誤代碼 <b>OIDC_CORRELATION_FAILED</b></p>' +
    '</div>';
  }

  const POST_SCREENS = {
    'forgot-sent':    forgotSentHTML,
    'register-sent':  registerSentHTML,
    'reset-done':     resetDoneHTML,
    'login-done':     loginDoneHTML,
    'login-error':    loginErrorHTML,
  };

  // ---- DOM helpers ----
  function setFieldError(id, msg) {
    const $field = $('.field[data-field="' + id + '"]');
    $field.find('.field-err').remove();
    $field.find('.input-shell').toggleClass('err', !!msg);
    if (msg) $field.append('<span class="field-err">' + icon('alert', { size: 13 }) + ' ' + esc(msg) + '</span>');
  }

  function setLoading(on) {
    const $btn = $('[data-submit]');
    if (on) $btn.attr('disabled', true).html('<span class="spinner"></span>');
  }

  // ---- password strength ----
  function scorePassword(pw) {
    pw = pw || '';
    let score = 0;
    if (pw.length >= 8) score++;
    if (/[A-Z]/.test(pw) && /[a-z]/.test(pw)) score++;
    if (/\d/.test(pw)) score++;
    if (/[^A-Za-z0-9]/.test(pw)) score++;
    if (pw.length >= 12 && score >= 3) score = 4;
    return [
      { level: 0, color: 'var(--border)', text: '太短',   hint: '至少 8 個字元' },
      { level: 1, color: '#ff4d9d',       text: '弱',     hint: '加入大小寫' },
      { level: 2, color: '#ff7a2f',       text: '普通',   hint: '加入數字' },
      { level: 3, color: '#ffc83a',       text: '不錯',   hint: '加入符號更強' },
      { level: 4, color: '#11a36a',       text: '很強',   hint: '安全 👍' },
    ][Math.min(score, 4)];
  }

  function strengthHTML(value) {
    if (!value) return '';
    const s = scorePassword(value);
    let bars = '';
    for (let i = 0; i < 4; i++) {
      bars += '<div class="pw-bar" style="background:' + (i < s.level ? s.color : 'var(--border)') + '"></div>';
    }
    return '<div class="pw-strength"><div class="pw-bars">' + bars + '</div>' +
      '<div class="pw-meta"><span class="pw-label" style="color:' + s.color + '">' + s.text + '</span>' +
      '<span class="pw-hint">' + s.hint + '</span></div></div>';
  }

  // ---- validators ----
  function isEmail(v) { return /^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(v || ''); }

  // ---- form submission ----
  function submit() {
    const f = state.form;
    if (state.screen === 'login') {
      const e = {};
      if (!isEmail(f.email)) e.email = '請輸入正確的電子信箱';
      if (!f.pw) e.pw = '請輸入密碼';
      if (Object.keys(e).length) { Object.keys(e).forEach(function (k) { setFieldError(k, e[k]); }); return; }
      setLoading(true);
      setTimeout(function () { navigate('login-done', { email: f.email }); }, 1100);

    } else if (state.screen === 'register') {
      const e = {};
      if (!(f.name || '').trim()) e.name = '請輸入你的暱稱';
      if (!isEmail(f.email))      e.email = '請輸入正確的電子信箱';
      if (scorePassword(f.pw).level < 2) e.pw = '密碼太弱，請加強';
      if (!f.agree) e.agree = '請先同意服務條款';
      if (Object.keys(e).length) { Object.keys(e).forEach(function (k) { setFieldError(k, e[k]); }); return; }
      setLoading(true);
      setTimeout(function () { navigate('register-sent', { email: f.email }); }, 1100);

    } else if (state.screen === 'forgot') {
      if (!isEmail(f.email)) { setFieldError('email', '請輸入正確的電子信箱'); return; }
      setFieldError('email', null);
      setLoading(true);
      setTimeout(function () { navigate('forgot-sent', { email: f.email }); }, 1100);

    } else if (state.screen === 'reset') {
      const e = {};
      if (scorePassword(f.pw).level < 2) e.pw = '密碼太弱，請加強';
      if (!f.pw2) e.pw2 = '請再次輸入密碼';
      else if (f.pw !== f.pw2) e.pw2 = '兩次輸入的密碼不一致';
      if (Object.keys(e).length) { Object.keys(e).forEach(function (k) { setFieldError(k, e[k]); }); return; }
      setLoading(true);
      setTimeout(function () { navigate('reset-done', {}); }, 1100);
    }
  }

  // ---- legal modal (pre-rendered in Razor, toggled by JS) ----
  function openLegal(which) {
    state.legal = which;
    $('#legal-scrim').css('display', '');
    $('#legal-terms-card').toggle(which === 'terms');
    $('#legal-privacy-card').toggle(which === 'privacy');
  }
  function closeLegal() {
    state.legal = null;
    $('#legal-scrim').hide();
    $('#legal-terms-card, #legal-privacy-card').hide();
  }
  function acknowledgeLegal() {
    if (state.legal) {
      state.read[state.legal] = true;
      $('.read-tick[data-tick="' + state.legal + '"]').html(
        icon('check', { size: 13, stroke: 2.8, style: 'color:#11a36a;margin-left:3px;vertical-align:-2px;' })
      );
    }
    closeLegal();
  }

  // ---- checkbox ----
  function toggleCheckbox(id) {
    state.form[id] = !state.form[id];
    const $cb = $('.checkbox[data-checkbox="' + id + '"]');
    $cb.toggleClass('on', !!state.form[id]).attr('aria-checked', String(!!state.form[id]));
    if (state.form[id]) setFieldError(id, null);
  }

  // ---- resend ----
  function resend() {
    if (state.left > 0) return;
    state.justSent = true;
    $('.resend').html(resendHTML());
    startTimer();
    setTimeout(function () { state.justSent = false; $('.resend').html(resendHTML()); }, 2600);
  }

  // ---- default form state per screen ----
  function defaultForm(screen) {
    if (screen === 'login')    return { remember: true };
    if (screen === 'register') return { agree: false };
    return {};
  }

  // ---- event binding ----
  function bind() {
    const $d = $(document);

    $d.on('click', '[data-go]', function () { navigate($(this).attr('data-go')); });
    $d.on('click', '[data-submit]', function () { submit(); });

    $d.on('input', 'input[data-input]', function () {
      const id = $(this).attr('data-input');
      state.form[id] = this.value;
      setFieldError(id, null);
      const $field = $(this).closest('.field');
      if ($field.attr('data-strength')) {
        $field.find('.pw-strength').remove();
        const h = strengthHTML(this.value);
        if (h) $field.find('.input-shell').after(h);
      }
    });
    $d.on('keydown', 'input[data-input]', function (e) { if (e.key === 'Enter') submit(); });

    $d.on('click', '[data-pwtoggle]', function () {
      const id = $(this).attr('data-pwtoggle');
      state.show[id] = !state.show[id];
      $('.field[data-field="' + id + '"] input[data-pw]').attr('type', state.show[id] ? 'text' : 'password');
      $(this).attr('title', state.show[id] ? '隱藏密碼' : '顯示密碼')
             .html(icon(state.show[id] ? 'eyeOff' : 'eye', { size: 18 }));
    });

    $d.on('click', '[data-checkbox]', function (e) {
      if ($(e.target).closest('a, button').length) return;
      toggleCheckbox($(this).attr('data-checkbox'));
    });
    $d.on('keydown', '[data-checkbox]', function (e) {
      if (e.key === ' ' || e.key === 'Enter') { e.preventDefault(); toggleCheckbox($(this).attr('data-checkbox')); }
    });

    $d.on('click', '[data-legal-open]',  function (e) { e.stopPropagation(); openLegal($(this).attr('data-legal-open')); });
    $d.on('click', '[data-legal-close]', function ()  { closeLegal(); });
    $d.on('click', '[data-legal-ack]',   function ()  { acknowledgeLegal(); });
    $d.on('click', '[data-legal-scrim]', function (e) { if (e.target === this) closeLegal(); });

    $d.on('click', '[data-resend]', function () { resend(); });

    $d.on('click', '[data-tweaks-close]', function () {
      $('#tweaks-panel').hide();
      window.parent.postMessage({ type: '__edit_mode_dismissed' }, '*');
    });
    $d.on('click', '[data-font]', function () {
      state.font = $(this).attr('data-font');
      savePrefs();
      $('#auth-shell').removeClass('font-bricolage font-unbounded').addClass('font-' + state.font);
      $('.seg button').removeClass('on');
      $('.seg button[data-font="' + state.font + '"]').addClass('on');
    });
    $d.on('click', '[data-accent]', function () {
      state.accent = $(this).attr('data-accent');
      savePrefs();
      $('#accent-style').html('.brand-panel{background:' + ACCENT_GRAD[state.accent] + ' !important;}');
      $('.swatch').removeClass('on');
      $('.swatch[data-accent="' + state.accent + '"]').addClass('on');
    });

    $d.on('keydown', function (e) { if (e.key === 'Escape' && state.legal) closeLegal(); });

    window.addEventListener('message', function (e) {
      const t = e && e.data && e.data.type;
      if (t === '__activate_edit_mode')   $('#tweaks-panel').css('display', '');
      else if (t === '__deactivate_edit_mode') $('#tweaks-panel').hide();
    });
    window.parent.postMessage({ type: '__edit_mode_available' }, '*');
  }

  // ---- init ----
  $(function () {
    loadPrefs();
    state.screen = $('#auth-shell').attr('data-screen') || 'login';
    state.form   = defaultForm(state.screen);
    try { state.data = { email: sessionStorage.getItem('ojAuthEmail') || '' }; } catch (e) { state.data = { email: '' }; }
    applyPrefs();
    if (POST_SCREENS[state.screen]) {
      if (state.screen === 'forgot-sent' || state.screen === 'register-sent') startTimer();
      $('.form-wrap').html(POST_SCREENS[state.screen]());
    }
    bind();
  });
})();
