/* June Auth — main app (jQuery): state, screen routing, validation, flow */
(function () {
  const icon = window.icon;
  const esc = window.esc;
  const $ = window.jQuery;

  // ---------------- brand copy per screen ----------------
  const BRAND = {
    login:    { h: '歡迎回來，<br/>繼續 <span class="hl hl-lime">創作</span>。', s: '登入 Open Jam，管理你的作品、追蹤銷售，與全球創作者一起發光。' },
    register: { h: '把作品<br/>變成 <span class="hl hl-yellow">收入</span>。', s: '加入 12,400+ 創作者，今天就開始在 Open Jam 上架你的數位作品。' },
    forgot:   { h: '別擔心，<br/>我們<span class="hl hl-cyan">幫你</span>。', s: '輸入註冊信箱，我們會立刻把重置連結寄給你。' },
    reset:    { h: '設定一組<br/><span class="hl hl-lime">新密碼</span>。', s: '選一組夠強的密碼，好好保護你的創作資產。' },
  };
  function brandFor(screen) {
    const map = { 'register-sent': 'register', 'forgot-sent': 'forgot', 'reset-done': 'reset', 'login-done': 'login' };
    return BRAND[map[screen] || screen] || BRAND.login;
  }

  const ACCENT_GRAD = {
    violet: 'radial-gradient(620px 460px at 12% 6%, rgba(255,200,58,.42), transparent 58%), radial-gradient(680px 520px at 92% 18%, rgba(255,77,157,.55), transparent 60%), radial-gradient(720px 620px at 78% 96%, rgba(31,214,198,.42), transparent 62%), radial-gradient(820px 720px at 18% 92%, rgba(108,76,241,.55), transparent 64%), linear-gradient(150deg, #6c4cf1, #8a3df1 46%, #c33ad6)',
    sunset: 'radial-gradient(620px 460px at 12% 6%, rgba(255,200,58,.5), transparent 58%), radial-gradient(680px 520px at 92% 18%, rgba(255,77,157,.55), transparent 60%), radial-gradient(720px 620px at 78% 96%, rgba(255,122,47,.5), transparent 62%), radial-gradient(820px 720px at 18% 92%, rgba(255,77,157,.5), transparent 64%), linear-gradient(150deg, #ff7a2f, #ff5a6e 50%, #ff4d9d)',
    ocean:  'radial-gradient(620px 460px at 12% 6%, rgba(174,240,62,.4), transparent 58%), radial-gradient(680px 520px at 92% 18%, rgba(31,214,198,.55), transparent 60%), radial-gradient(720px 620px at 78% 96%, rgba(108,76,241,.5), transparent 62%), radial-gradient(820px 720px at 18% 92%, rgba(31,214,198,.5), transparent 64%), linear-gradient(150deg, #1fd6c6, #2f8ff1 50%, #6c4cf1)',
  };

  // ---------------- state ----------------
  const state = {
    screen: 'login',
    data: {},
    form: { remember: true },
    errors: {},
    show: {},
    loading: false,
    font: 'bricolage',
    accent: 'violet',
    tweaksOpen: false,
    legal: null,
    read: { terms: false, privacy: false },
    left: 45,
    justSent: false,
  };

  let timer = null;
  function clearTimer() { if (timer) { clearInterval(timer); timer = null; } }
  function startTimer() {
    clearTimer();
    timer = setInterval(function () {
      if (state.left > 0) state.left--;
      $('.resend').html(resendHTML());
      if (state.left <= 0) clearTimer();
    }, 1000);
  }

  // ---------------- shared bits ----------------
  function popBtn(o) {
    const cls = 'btn-pop' + (o.variant ? ' ' + o.variant : '');
    let inner;
    if (state.loading) inner = '<span class="spinner"></span>';
    else inner = icon(o.icon, { size: o.size || 18, stroke: o.stroke }) + ' ' + o.label;
    return '<button class="' + cls + '" data-submit' + (state.loading ? ' disabled' : '') + '>' + inner + '</button>';
  }

  function readTick(on) {
    return on ? icon('check', { size: 13, stroke: 2.8, style: 'color:#11a36a;margin-left:3px;vertical-align:-2px;' }) : '';
  }

  function resendHTML() {
    const mm = Math.floor(state.left / 60);
    const ss = String(state.left % 60).padStart(2, '0');
    const isReg = state.screen === 'register-sent';
    const lead = isReg ? '沒收到信？' : '沒收到？';
    const label = isReg ? '重新寄送確認信' : '重新寄送至此信箱';
    if (state.justSent) return '<span class="resend-ok">' + icon('check', { size: 13, stroke: 2.6 }) + ' 已重新寄送至此信箱</span>';
    if (state.left > 0) return lead + '可於 <b>' + mm + ':' + ss + '</b> 後重新寄送';
    return lead + '<button class="link-strong" data-resend>' + label + '</button>';
  }

  // ---------------- screens ----------------
  function loginHTML() {
    return '' +
      '<div class="screen">' +
        '<p class="form-eyebrow">' + icon('sparkle', { size: 13 }) + ' 歡迎回來</p>' +
        '<h2 class="form-title">登入 Open Jam</h2>' +
        '<p class="form-sub">還沒準備好？先逛逛也沒關係。輸入帳號繼續你的創作旅程。</p>' +
        '<div class="form-body">' +
          window.fieldHTML({ id: 'email', label: '電子信箱', type: 'email', icon: 'mail', value: state.form.email, placeholder: 'you@example.com', error: state.errors.email, autoComplete: 'email' }) +
          window.passwordFieldHTML({ id: 'pw', label: '密碼', value: state.form.pw, placeholder: '輸入你的密碼', error: state.errors.pw, autoComplete: 'current-password', show: state.show.pw, labelRight: '<button class="link-strong" data-go="forgot">忘記密碼？</button>' }) +
          '<div class="form-row">' + window.checkboxHTML({ id: 'remember', checked: state.form.remember, label: '記住我' }) + '</div>' +
          popBtn({ variant: 'violet', icon: 'arrowRight', size: 18, label: '登入' }) +
        '</div>' +
        '<p class="form-foot">還沒有帳號？<button class="link-strong" data-go="register">免費註冊</button></p>' +
      '</div>';
  }

  function registerHTML() {
    const agreeLabel =
      '我已閱讀並同意 ' +
      '<button type="button" class="legal-link" data-legal-open="terms">服務條款' + readTick(state.read.terms) + '</button>' +
      ' 與 ' +
      '<button type="button" class="legal-link" data-legal-open="privacy">隱私政策' + readTick(state.read.privacy) + '</button>';
    const agreeErr = state.errors.agree
      ? '<span class="field-err" style="margin-top:4px">' + icon('alert', { size: 13 }) + ' ' + esc(state.errors.agree) + '</span>'
      : '';
    return '' +
      '<div class="screen">' +
        '<p class="form-eyebrow">' + icon('sparkle', { size: 13 }) + ' 開始創作</p>' +
        '<h2 class="form-title">建立帳號</h2>' +
        '<p class="form-sub">免費開店、零月費，售出才收 3% 手續費。30 秒就能上手。</p>' +
        '<div class="form-body">' +
          window.fieldHTML({ id: 'name', label: '創作者暱稱', icon: 'user', value: state.form.name, placeholder: '例如：小雨工作室', error: state.errors.name, autoComplete: 'nickname' }) +
          window.fieldHTML({ id: 'email', label: '電子信箱', type: 'email', icon: 'mail', value: state.form.email, placeholder: 'you@example.com', error: state.errors.email, autoComplete: 'email' }) +
          window.passwordFieldHTML({ id: 'pw', label: '密碼', value: state.form.pw, placeholder: '設定一組密碼', error: state.errors.pw, showStrength: true, autoComplete: 'new-password', show: state.show.pw }) +
          '<div class="field" data-field="agree" style="margin-top:-2px">' +
            window.checkboxHTML({ id: 'agree', checked: state.form.agree, label: agreeLabel }) +
            agreeErr +
          '</div>' +
          popBtn({ icon: 'sparkle', size: 18, label: '建立帳號' }) +
        '</div>' +
        '<p class="form-foot">已經有帳號了？<button class="link-strong" data-go="login">前往登入</button></p>' +
      '</div>';
  }

  function forgotHTML() {
    return '' +
      '<div class="screen">' +
        '<button class="back-link" data-go="login">' + icon('arrowLeft', { size: 16 }) + ' 返回登入</button>' +
        '<p class="form-eyebrow">' + icon('key', { size: 13 }) + ' 帳號救援</p>' +
        '<h2 class="form-title">忘記密碼？</h2>' +
        '<p class="form-sub">輸入你的註冊信箱，我們會寄一條重置連結給你。連結 30 分鐘內有效。</p>' +
        '<div class="form-body">' +
          window.fieldHTML({ id: 'email', label: '電子信箱', type: 'email', icon: 'mail', value: state.form.email, placeholder: 'you@example.com', error: state.errors.email, autoComplete: 'email' }) +
          popBtn({ variant: 'violet', icon: 'send', size: 17, label: '寄送重置連結' }) +
        '</div>' +
        '<p class="form-foot">想起來了？<button class="link-strong" data-go="login">直接登入</button></p>' +
      '</div>';
  }

  function forgotSentHTML() {
    return '' +
      '<div class="screen">' +
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

  function resetHTML() {
    return '' +
      '<div class="screen">' +
        '<p class="form-eyebrow">' + icon('key', { size: 13 }) + ' 設定新密碼</p>' +
        '<h2 class="form-title">重置密碼</h2>' +
        '<p class="form-sub">為你的 Open Jam 帳號設定一組全新密碼。設定後其他裝置會自動登出。</p>' +
        '<div class="form-body">' +
          window.passwordFieldHTML({ id: 'pw', label: '新密碼', value: state.form.pw, placeholder: '輸入新密碼', error: state.errors.pw, showStrength: true, autoComplete: 'new-password', show: state.show.pw }) +
          window.passwordFieldHTML({ id: 'pw2', label: '確認新密碼', value: state.form.pw2, placeholder: '再次輸入新密碼', error: state.errors.pw2, autoComplete: 'new-password', show: state.show.pw2 }) +
          popBtn({ variant: 'violet', icon: 'check', size: 18, stroke: 2.4, label: '更新密碼' }) +
        '</div>' +
        '<p class="form-foot"><button class="link-strong" data-go="login">取消，返回登入</button></p>' +
      '</div>';
  }

  function resetDoneHTML() {
    return '' +
      '<div class="screen">' +
        '<div class="success-ring">' + icon('check', { size: 46, stroke: 2.6 }) + '</div>' +
        '<h2 class="form-title">密碼已更新 🎉</h2>' +
        '<p class="form-sub">你的密碼已成功重置，現在可以用新密碼登入 Open Jam 了。</p>' +
        '<div class="form-body">' +
          '<button class="btn-pop violet" data-go="login">' + icon('arrowRight', { size: 17 }) + ' 前往登入</button>' +
        '</div>' +
      '</div>';
  }

  function registerSentHTML() {
    return '' +
      '<div class="screen">' +
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

  function loginDoneHTML() {
    return '' +
      '<div class="screen">' +
        '<div class="success-ring">' + icon('check', { size: 46, stroke: 2.6 }) + '</div>' +
        '<h2 class="form-title">登入成功 🎉</h2>' +
        '<p class="form-sub">歡迎回來，<b>' + esc(state.data.email) + '</b>。正在帶你前往創作工作室…</p>' +
        '<div class="form-body">' +
          '<div class="btn-pop violet" style="cursor:default"><span class="spinner"></span> 載入工作室…</div>' +
        '</div>' +
      '</div>';
  }

  const SCREENS = {
    login: loginHTML, register: registerHTML, forgot: forgotHTML, 'forgot-sent': forgotSentHTML,
    reset: resetHTML, 'reset-done': resetDoneHTML, 'register-sent': registerSentHTML, 'login-done': loginDoneHTML,
  };

  // ---------------- tweaks ----------------
  function tweaksHTML() {
    if (!state.tweaksOpen) return '';
    const accents = [
      { id: 'violet', label: '紫粉', a: '#6c4cf1', b: '#ff4d9d' },
      { id: 'sunset', label: '日落', a: '#ff7a2f', b: '#ff4d9d' },
      { id: 'ocean',  label: '海洋', a: '#1fd6c6', b: '#6c4cf1' },
    ];
    const sw = accents.map(function (x) {
      return '<div class="swatch' + (state.accent === x.id ? ' on' : '') + '" data-accent="' + x.id + '" ' +
        'style="background:linear-gradient(135deg, ' + x.a + ', ' + x.b + ')" title="' + x.label + '"></div>';
    }).join('');
    return '' +
      '<div class="tweaks-panel">' +
        '<div class="tweaks-head"><span>Tweaks</span>' +
          '<button class="tweaks-x" data-tweaks-close><svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M6 6l12 12M18 6L6 18"></path></svg></button>' +
        '</div>' +
        '<div class="tweaks-body">' +
          '<div class="tweaks-section">展示字體</div>' +
          '<div class="tweaks-row"><span>標題字型</span>' +
            '<div class="seg">' +
              '<button class="' + (state.font === 'bricolage' ? 'on' : '') + '" data-font="bricolage">Bricolage</button>' +
              '<button class="' + (state.font === 'unbounded' ? 'on' : '') + '" data-font="unbounded">Unbounded</button>' +
            '</div>' +
          '</div>' +
          '<div class="tweaks-section">品牌色調</div>' +
          '<div class="tweaks-row"><span>面板漸層</span><div class="swatches">' + sw + '</div></div>' +
        '</div>' +
      '</div>';
  }

  // ---------------- render ----------------
  function render() {
    const b = brandFor(state.screen);
    const html =
      '<div class="auth-shell font-' + state.font + '" data-screen-label="' + state.screen + '">' +
        window.brandPanelHTML(b.h, b.s) +
        '<main class="form-panel">' +
          '<div class="mobile-brand">' +
            '<span class="brand-mark" style="background:linear-gradient(135deg, var(--c-violet), var(--c-pink))">' +
              '<span style="color:#fff;display:grid;place-items:center">' + window.brandMark(18) + '</span>' +
            '</span>' +
            '<span class="brand-name">Open Jam</span>' +
          '</div>' +
          '<div class="form-wrap">' + SCREENS[state.screen]() + '</div>' +
        '</main>' +
        tweaksHTML() +
        window.legalModalHTML(state.legal) +
        '<style>.brand-panel{background:' + ACCENT_GRAD[state.accent] + ' !important;} .mobile-brand .brand-mark svg{color:#fff;}</style>' +
      '</div>';
    $('#root').html(html);
  }

  // ---------------- navigation / flow ----------------
  function defaultForm(screen) {
    if (screen === 'login') return { remember: true };
    if (screen === 'register') return { agree: false };
    return {};
  }

  function go(screen) {
    clearTimer();
    state.screen = screen;
    state.errors = {};
    state.loading = false;
    state.show = {};
    state.form = defaultForm(screen);
    if (screen === 'forgot-sent' || screen === 'register-sent') {
      state.left = 45;
      state.justSent = false;
      startTimer();
    }
    window.scrollTo({ top: 0 });
    render();
  }

  function finish(screen, data) {
    state.data = data || {};
    go(screen);
  }

  function submit() {
    const f = state.form;
    if (state.screen === 'login') {
      const e = {};
      if (!window.isEmail(f.email)) e.email = '請輸入正確的電子信箱';
      if (!f.pw) e.pw = '請輸入密碼';
      state.errors = e;
      if (Object.keys(e).length) { render(); return; }
      state.loading = true; render();
      setTimeout(function () { finish('login-done', { email: f.email }); }, 1100);
    } else if (state.screen === 'register') {
      const e = {};
      if (!(f.name || '').trim()) e.name = '請輸入你的暱稱';
      if (!window.isEmail(f.email)) e.email = '請輸入正確的電子信箱';
      if (window.scorePassword(f.pw).level < 2) e.pw = '密碼太弱，請加強';
      if (!f.agree) e.agree = '請先同意服務條款';
      state.errors = e;
      if (Object.keys(e).length) { render(); return; }
      state.loading = true; render();
      setTimeout(function () { finish('register-sent', { email: f.email }); }, 1100);
    } else if (state.screen === 'forgot') {
      if (!window.isEmail(f.email)) { state.errors = { email: '請輸入正確的電子信箱' }; render(); return; }
      state.errors = {};
      state.loading = true; render();
      setTimeout(function () { finish('forgot-sent', { email: f.email }); }, 1100);
    } else if (state.screen === 'reset') {
      const e = {};
      if (window.scorePassword(f.pw).level < 2) e.pw = '密碼太弱，請加強';
      if (!f.pw2) e.pw2 = '請再次輸入密碼';
      else if (f.pw !== f.pw2) e.pw2 = '兩次輸入的密碼不一致';
      state.errors = e;
      if (Object.keys(e).length) { render(); return; }
      state.loading = true; render();
      setTimeout(function () { finish('reset-done', {}); }, 1100);
    }
  }

  function toggleCheckbox(id) {
    state.form[id] = !state.form[id];
    const $cb = $('.checkbox[data-checkbox="' + id + '"]');
    $cb.toggleClass('on', !!state.form[id]).attr('aria-checked', !!state.form[id]);
    if (id === 'agree' && state.errors.agree) {
      $('.field[data-field="agree"] .field-err').remove();
      delete state.errors.agree;
    }
  }

  function openLegal(which) { state.legal = which; render(); }
  function closeLegal() { state.legal = null; render(); }
  function acknowledgeLegal() {
    if (state.legal) state.read[state.legal] = true;
    state.legal = null;
    render();
  }

  function resend() {
    if (state.left > 0) return;
    state.left = 45;
    state.justSent = true;
    $('.resend').html(resendHTML());
    startTimer();
    setTimeout(function () { state.justSent = false; $('.resend').html(resendHTML()); }, 2600);
  }

  // ---------------- events (delegated, bound once) ----------------
  function bind() {
    const $d = $(document);

    $d.on('click', '[data-go]', function () { go($(this).attr('data-go')); });
    $d.on('click', '[data-submit]', function () { submit(); });

    $d.on('input', 'input[data-input]', function () {
      const id = $(this).attr('data-input');
      state.form[id] = this.value;
      const $field = $(this).closest('.field');
      if ($field.attr('data-strength')) {
        $field.find('.pw-strength').remove();
        const h = window.strengthHTML(this.value);
        if (h) $field.find('.input-shell').after(h);
      }
    });

    $d.on('keydown', 'input[data-input]', function (e) {
      if (e.key === 'Enter') submit();
    });

    $d.on('click', '[data-pwtoggle]', function () {
      const id = $(this).attr('data-pwtoggle');
      state.show[id] = !state.show[id];
      const $field = $('.field[data-field="' + id + '"]');
      $field.find('input[data-pw]').attr('type', state.show[id] ? 'text' : 'password');
      $(this).attr('title', state.show[id] ? '隱藏密碼' : '顯示密碼')
        .html(icon(state.show[id] ? 'eyeOff' : 'eye', { size: 18 }));
    });

    $d.on('click', '[data-checkbox]', function (e) {
      if ($(e.target).closest('a, button').length) return; // ignore legal links
      toggleCheckbox($(this).attr('data-checkbox'));
    });
    $d.on('keydown', '[data-checkbox]', function (e) {
      if (e.key === ' ' || e.key === 'Enter') { e.preventDefault(); toggleCheckbox($(this).attr('data-checkbox')); }
    });

    $d.on('click', '[data-legal-open]', function (e) { e.stopPropagation(); openLegal($(this).attr('data-legal-open')); });
    $d.on('click', '[data-legal-close]', function () { closeLegal(); });
    $d.on('click', '[data-legal-ack]', function () { acknowledgeLegal(); });
    $d.on('click', '[data-legal-scrim]', function (e) { if (e.target === this) closeLegal(); });

    $d.on('click', '[data-resend]', function () { resend(); });

    $d.on('click', '[data-tweaks-close]', function () {
      state.tweaksOpen = false; render();
      window.parent.postMessage({ type: '__edit_mode_dismissed' }, '*');
    });
    $d.on('click', '[data-font]', function () { state.font = $(this).attr('data-font'); render(); });
    $d.on('click', '[data-accent]', function () { state.accent = $(this).attr('data-accent'); render(); });

    $d.on('keydown', function (e) { if (e.key === 'Escape' && state.legal) closeLegal(); });

    window.addEventListener('message', function (e) {
      const t = e && e.data && e.data.type;
      if (t === '__activate_edit_mode') { state.tweaksOpen = true; render(); }
      else if (t === '__deactivate_edit_mode') { state.tweaksOpen = false; render(); }
    });
    window.parent.postMessage({ type: '__edit_mode_available' }, '*');
  }

  $(function () {
    bind();
    render();
  });
})();
