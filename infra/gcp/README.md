# GCP 監控告警（Discord）

正式站告警分三層，全部匯入同一個 Discord 頻道：

| 層級 | 偵測 | 元件 |
|------|------|------|
| 網站掛了 | openjam.co / auth / hydra / api / workspace 外部探測失敗（叢集、LB、DNS、憑證任一層掛都涵蓋） | Cloud Monitoring uptime check（GCP 全球節點，叢集全掛仍能告警） |
| 應用程式錯誤 | 服務 stdout 出現 `fail:` / `crit:` 前綴或未捕捉例外 | Cloud Logging log-based alert |
| pod / node 異常 | CrashLoopBackOff、OOM、ImagePull 失敗、PVC 快滿、node 異常（含失敗原因與 log 片段） | 叢集內 kwatch（helm chart `kwatch` 區塊），直接發 Discord |

前兩層的告警經 `alert-relay/`（Cloud Functions gen2）轉發：GCP webhook
notification channel 的 JSON 與 Discord webhook 格式不相容，relay 驗證
`?token=` 後轉成 Discord embed（紅＝觸發、綠＝恢復，台北時間）。

## 建置 / 更新

```bash
DISCORD_WEBHOOK_URL="https://discord.com/api/webhooks/..." \
RELAY_TOKEN="<與現有 function env 相同的 token；首次可 openssl rand -hex 24>" \
./monitoring/setup.sh
```

腳本冪等：function 原地更新，channel / uptime check / alert policy 已存在即略過
（調整既有 policy 請直接在 Cloud Console 或先刪除再重跑）。

## 注意事項

- **log 告警 filter 勿改用 `severity>=ERROR`**：.NET console log 進 Cloud Logging
  一律 severity=INFO，錯誤只認得出行首 `fail:` / `crit:`；而 stderr 全被標成
  ERROR（nginx `[notice]`、Hydra 警告都中），噪音太大。
- kwatch 的 Discord webhook 走 helm 機密 overlay `secrets.discord.webhookUrl`
  （GCP Secret Manager `open-jam-helm-secrets`），與 relay 的 `DISCORD_WEBHOOK_URL`
  可以是同一個頻道 webhook。
- uptime check 5 條 × 3 region × 每分鐘 ≈ 65 萬次/月，在免費額度（100 萬次）內；
  新增 check 時留意勿超量。
- relay function 為 `--allow-unauthenticated`（Cloud Monitoring webhook channel
  不支援 OIDC），僅靠 `RELAY_TOKEN` 防護；token 洩漏時重跑 setup.sh 換新值即可。
