// Cloud Monitoring → Discord 告警轉發（Cloud Functions gen2, Node.js 20）。
//
// GCP 的 webhook notification channel 送出的 JSON 與 Discord webhook 格式不相容，
// 本 function 作為中繼：驗證 token → 將 incident payload 轉成 Discord embed 發出。
// 部署與 channel 註冊見 ../monitoring/setup.sh。
const functions = require('@google-cloud/functions-framework');

const COLOR_FIRING = 0xe74c3c;   // 紅：告警觸發
const COLOR_RESOLVED = 0x2ecc71; // 綠：已恢復

/** epoch 秒 → 台北時間字串；缺值回 null */
function formatTaipei(epochSeconds) {
  if (!epochSeconds) return null;
  return new Date(epochSeconds * 1000).toLocaleString('zh-TW', {
    timeZone: 'Asia/Taipei',
    hour12: false,
  });
}

functions.http('alertRelay', async (req, res) => {
  if (req.method !== 'POST') {
    res.status(405).send('method not allowed');
    return;
  }
  const expected = process.env.RELAY_TOKEN;
  if (!expected || req.query.token !== expected) {
    res.status(401).send('unauthorized');
    return;
  }

  const incident = req.body && req.body.incident;
  if (!incident) {
    // 建立 channel 時 GCP 會先送驗證請求；非 incident 內容一律回 200 略過
    res.status(200).send('ignored');
    return;
  }

  const firing = incident.state === 'open';
  const fields = [];
  if (incident.condition_name) {
    fields.push({ name: '條件', value: incident.condition_name, inline: true });
  }
  if (incident.resource_display_name || incident.resource_name) {
    fields.push({
      name: '資源',
      value: incident.resource_display_name || incident.resource_name,
      inline: true,
    });
  }
  const startedAt = formatTaipei(incident.started_at);
  if (startedAt) fields.push({ name: '開始於', value: startedAt, inline: true });
  const endedAt = formatTaipei(incident.ended_at);
  if (!firing && endedAt) fields.push({ name: '恢復於', value: endedAt, inline: true });

  const payload = {
    username: 'Open Jam 監控',
    embeds: [
      {
        title: `${firing ? '🚨' : '✅'} ${incident.policy_name || '未知告警'}${firing ? '' : '（已恢復）'}`,
        description: (incident.summary || '').slice(0, 2000),
        url: incident.url || undefined,
        color: firing ? COLOR_FIRING : COLOR_RESOLVED,
        fields,
        timestamp: new Date().toISOString(),
      },
    ],
  };

  const resp = await fetch(process.env.DISCORD_WEBHOOK_URL, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify(payload),
  });
  if (!resp.ok) {
    console.error(`discord webhook failed: ${resp.status} ${await resp.text()}`);
    res.status(502).send('discord webhook failed');
    return;
  }
  res.status(200).send('ok');
});
