/* June Auth — brand panel (split-screen left), jQuery string builder */
(function () {
  const icon = window.icon;
  const brandMark = window.brandMark;

  const FCARD = [
    { top: '20%', left: '60%', rot: 6,  c1: '#ff7a2f', c2: '#ff4d9d', cat: 'PHOTO', glyph: 'image', title: '霓虹城市夜景', price: '$24', star: '4.9' },
    { top: '46%', left: '52%', rot: -7, c1: '#1fd6c6', c2: '#6c4cf1', cat: 'SCORE', glyph: 'note',  title: 'Lo-Fi 節拍包', price: '$18', star: '4.8' },
    { top: '8%',  left: '8%',  rot: -5, c1: '#ffc83a', c2: '#ff7a2f', cat: 'EBOOK', glyph: 'book',  title: '插畫家手冊', price: '免費', star: '5.0' },
  ];

  const SHAPES = [
    { cls: 'sq',  style: 'top:74%;left:12%;width:70px;height:70px;background:var(--c-lime);transform:rotate(14deg);' },
    { cls: 'dot', style: 'top:16%;left:46%;width:46px;height:46px;background:var(--c-yellow);' },
    { cls: 'dot', style: 'top:64%;left:70%;width:30px;height:30px;background:var(--c-cyan);' },
    { cls: 'tri', style: 'top:40%;left:6%;border-left:26px solid transparent;border-right:26px solid transparent;border-bottom:44px solid var(--c-pink);transform:rotate(-16deg);filter:drop-shadow(3px 4px 0 rgba(26,22,38,.45));' },
  ];

  function floatCard(d) {
    return (
      '<div class="fcard" style="top:' + d.top + ';left:' + d.left + ';transform:rotate(' + d.rot + 'deg)">' +
        '<div class="fc-thumb" style="background:linear-gradient(135deg, ' + d.c1 + ', ' + d.c2 + ')">' +
          '<div class="fc-dots"></div>' +
          '<div class="fc-cat">' + d.cat + '</div>' +
          '<div class="fc-glyph">' + icon(d.glyph, { size: 34, stroke: 1.6 }) + '</div>' +
        '</div>' +
        '<div class="fc-body">' +
          '<div class="fc-title">' + d.title + '</div>' +
          '<div class="fc-foot">' +
            '<span class="fc-price">' + d.price + '</span>' +
            '<span class="fc-star">' + icon('star', { size: 11, fill: true, style: 'color:#f0a92b;' }) + ' ' + d.star + '</span>' +
          '</div>' +
        '</div>' +
      '</div>'
    );
  }

  // headline / sub are raw HTML strings
  window.brandPanelHTML = function (headline, sub) {
    const cards = FCARD.map(floatCard).join('');
    const shapes = SHAPES.map(function (s) {
      return '<div class="shape ' + s.cls + '" style="' + s.style + '"></div>';
    }).join('');
    return (
      '<aside class="brand-panel">' +
        '<div class="brand-content">' +
          '<div class="collage" aria-hidden="true">' + cards + shapes + '</div>' +
          '<div class="brand-lockup">' +
            '<span class="brand-mark">' + brandMark(20) + '</span>' +
            '<span class="brand-name">Open Jam</span>' +
          '</div>' +
          '<div class="brand-mid">' +
            '<span class="brand-eyebrow">' + icon('sparkle', { size: 13 }) + ' 創作者數位市集</span>' +
            '<h1 class="brand-headline">' + headline + '</h1>' +
            '<p class="brand-sub">' + sub + '</p>' +
          '</div>' +
          '<div class="brand-foot">' +
            '<span>12,400+ 創作者</span>' +
            '<span class="fdiv"></span>' +
            '<span>86,000+ 作品</span>' +
            '<span class="fdiv"></span>' +
            '<span>安全付款</span>' +
          '</div>' +
        '</div>' +
      '</aside>'
    );
  };
})();
