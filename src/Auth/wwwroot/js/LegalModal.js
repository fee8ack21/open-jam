/* Open Jam Auth — legal modal (服務條款 / 隱私政策), jQuery string builder */
(function () {
  const icon = window.icon;

  const TERMS = {
    title: '服務條款', badge: 'terms', icon: 'note', updated: '最後更新 2026-05-31',
    secs: [
      { t: '歡迎加入 Open Jam', b: 'Open Jam 是一個讓創作者上架與販售數位作品（音樂、圖像、電子書等）的市集平台。當你建立帳號或使用本服務，即表示你同意以下條款。' },
      { t: '帳號與資格', b: '你必須年滿 13 歲才能註冊。你需對帳號活動與密碼安全負責，並承諾提供正確的註冊資訊。' },
      { t: '創作者責任', b: '你上架的所有作品必須為你本人擁有或已取得授權的內容。嚴禁上架以下類型：', list: ['侵害他人智慧財產權的內容', '惡意程式、病毒或誤導性檔案', '違反當地法律或本平台社群守則之內容'] },
      { t: '費用與結算', b: 'Open Jam 採免月費制，作品成功售出後平台收取 3% 手續費。款項將依結算週期撥付至你綁定的收款帳戶。' },
      { t: '內容授權', b: '你保留作品的完整著作權。為了在平台上展示與銷售，你授予 Open Jam 一項非專屬、全球性的授權以託管、展示與推廣你的作品。' },
      { t: '帳號終止', b: '若你違反本條款，我們有權暫停或終止你的帳號。你也可以隨時於設定中刪除帳號。' },
    ],
  };

  const PRIVACY = {
    title: '隱私政策', badge: 'privacy', icon: 'shield', updated: '最後更新 2026-05-31',
    secs: [
      { t: '我們重視你的隱私', b: '本政策說明 Open Jam 如何蒐集、使用與保護你的個人資料。使用本服務即表示你了解並同意以下作法。' },
      { t: '我們蒐集的資料', b: '為提供服務，我們會蒐集以下資訊：', list: ['帳號資料：暱稱、電子信箱、密碼（加密儲存）', '交易資料：購買與銷售紀錄、收款帳戶', '使用資料：裝置、瀏覽器與互動行為紀錄'] },
      { t: '資料的使用方式', b: '我們使用你的資料來營運平台、處理交易、提供客戶支援，以及在你同意的前提下寄送產品更新與行銷通知。' },
      { t: '資料分享', b: '我們不會販售你的個人資料。僅在金流處理、法律要求或經你授權的情況下，與必要的第三方服務商分享。' },
      { t: '資料安全', b: '我們以業界標準加密保護你的密碼與敏感資料，並定期檢視安全機制。但沒有任何系統能保證百分之百安全。' },
      { t: '你的權利', b: '你有權查詢、更正或刪除你的個人資料，也可隨時取消行銷通知的訂閱。相關需求可透過設定頁或聯絡客服處理。' },
    ],
  };

  const DOCS = { terms: TERMS, privacy: PRIVACY };
  window.LEGAL_DOCS = DOCS;

  // returns modal HTML for a given doc key, or '' if none
  window.legalModalHTML = function (which) {
    if (!which) return '';
    const doc = DOCS[which];
    let secs = '';
    doc.secs.forEach(function (s, i) {
      const num = String(i + 1).padStart(2, '0');
      const list = s.list
        ? '<ul>' + s.list.map(function (li) { return '<li>' + li + '</li>'; }).join('') + '</ul>'
        : '';
      secs +=
        '<div class="legal-sec">' +
          '<h4><span class="num">' + num + '</span> ' + s.t + '</h4>' +
          '<p>' + s.b + '</p>' + list +
        '</div>';
    });

    return (
      '<div class="modal-scrim" data-legal-scrim>' +
        '<div class="modal-card" role="dialog" aria-modal="true" aria-label="' + doc.title + '">' +
          '<div class="modal-head">' +
            '<div class="modal-badge ' + doc.badge + '">' + icon(doc.icon, { size: 22 }) + '</div>' +
            '<h3 class="modal-title">' + doc.title + '</h3>' +
            '<p class="modal-meta">Open Jam · ' + doc.updated + '</p>' +
            '<button class="modal-x" data-legal-close aria-label="關閉">' +
              '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"><path d="M6 6l12 12M18 6L6 18"></path></svg>' +
            '</button>' +
          '</div>' +
          '<div class="modal-body">' + secs + '</div>' +
          '<div class="modal-foot">' +
            '<button class="btn-ghost" data-legal-close>關閉</button>' +
            '<button class="btn-pop violet" data-legal-ack>' + icon('check', { size: 17, stroke: 2.4 }) + ' 我了解了</button>' +
          '</div>' +
        '</div>' +
      '</div>'
    );
  };
})();
