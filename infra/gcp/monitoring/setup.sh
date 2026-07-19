#!/usr/bin/env bash
# Open Jam 正式站告警：GCP Cloud Monitoring 資源建置（冪等，可重跑）
#
# 建立三類資源：
#   1. alert relay Cloud Function（../alert-relay，Cloud Monitoring webhook → Discord）
#   2. webhook_tokenauth notification channel（指向 relay）
#   3. uptime checks（外部探測各站台）＋ alert policies（uptime 失敗、應用錯誤 log）
#
# 用法：
#   DISCORD_WEBHOOK_URL="https://discord.com/api/webhooks/..." \
#   RELAY_TOKEN="$(openssl rand -hex 24)" \
#   ./setup.sh
#
# RELAY_TOKEN 為 relay 的共享密鑰（channel URL 與 function env 兩側一致即可）；
# 重跑腳本時帶同一組值，function 會原地更新、既有 channel / check / policy 略過。
# 需要 gcloud alpha / beta 元件（gcloud components install alpha beta）。
set -euo pipefail

PROJECT="open-jam-498418"
REGION="asia-east1"
FUNCTION_NAME="alert-discord-relay"
CHANNEL_DISPLAY="Discord 告警頻道（alert relay）"
NAMESPACE="open-jam"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

: "${DISCORD_WEBHOOK_URL:?請以環境變數提供 Discord webhook URL}"
: "${RELAY_TOKEN:?請以環境變數提供 relay 驗證 token（隨機字串）}"

# ── 1. Alert relay Cloud Function ─────────────────────────────────────────────
echo "==> 部署 ${FUNCTION_NAME}"
gcloud functions deploy "$FUNCTION_NAME" --gen2 \
  --project="$PROJECT" --region="$REGION" \
  --runtime=nodejs20 --entry-point=alertRelay \
  --source="$SCRIPT_DIR/../alert-relay" \
  --trigger-http --allow-unauthenticated \
  --memory=256Mi --max-instances=3 \
  --set-env-vars="DISCORD_WEBHOOK_URL=${DISCORD_WEBHOOK_URL},RELAY_TOKEN=${RELAY_TOKEN}" \
  --quiet

FUNCTION_URL="$(gcloud functions describe "$FUNCTION_NAME" --gen2 \
  --project="$PROJECT" --region="$REGION" --format='value(url)')"
echo "    relay URL: ${FUNCTION_URL}"

# ── 2. Notification channel ───────────────────────────────────────────────────
# channels / policies 的 --filter 走 Monitoring API 原生語法（snake_case + 雙引號字串），
# 非 gcloud 標準 filter；用 displayName='...' 會被解析器拒絕
CHANNEL_ID="$(gcloud beta monitoring channels list --project="$PROJECT" \
  --filter="display_name=\"${CHANNEL_DISPLAY}\"" --format='value(name)')"
if [ -z "$CHANNEL_ID" ]; then
  echo "==> 建立 notification channel"
  gcloud beta monitoring channels create --project="$PROJECT" \
    --display-name="$CHANNEL_DISPLAY" \
    --type=webhook_tokenauth \
    --channel-labels="url=${FUNCTION_URL}?token=${RELAY_TOKEN}"
  CHANNEL_ID="$(gcloud beta monitoring channels list --project="$PROJECT" \
    --filter="display_name=\"${CHANNEL_DISPLAY}\"" --format='value(name)')"
else
  echo "==> notification channel 已存在，略過"
fi
echo "    channel: ${CHANNEL_ID}"

# ── 3. Uptime checks ──────────────────────────────────────────────────────────
# key|host|path（healthz 端點不經 PathBase 前綴剝除也可直達，見 Shared/Web/PathBaseExtensions）
CHECKS=(
  "portal|openjam.co|/"
  "auth|auth.openjam.co|/healthz"
  "hydra|hydra.openjam.co|/health/ready"
  "api-order|api.openjam.co|/order-service/healthz"
  "workspace|workspace.openjam.co|/"
)

uptime_check_id() {
  gcloud monitoring uptime list-configs --project="$PROJECT" \
    --filter="displayName='$1'" --format='value(name)' | awk -F/ '{print $NF}'
}

for entry in "${CHECKS[@]}"; do
  IFS='|' read -r key host path <<<"$entry"
  display="uptime-${key}"
  if [ -z "$(uptime_check_id "$display")" ]; then
    echo "==> 建立 uptime check ${display}（https://${host}${path}）"
    gcloud monitoring uptime create "$display" --project="$PROJECT" \
      --resource-type=uptime-url \
      --resource-labels="host=${host},project_id=${PROJECT}" \
      --protocol=https --port=443 --path="$path" \
      --period=1 --timeout=10 \
      --regions=asia-pacific,europe,usa-iowa
  else
    echo "==> uptime check ${display} 已存在，略過"
  fi
done

# ── 4. Alert policies ─────────────────────────────────────────────────────────
TMP_POLICY="$(mktemp)"
trap 'rm -f "$TMP_POLICY"' EXIT

create_policy_if_absent() {
  local display="$1"
  local existing
  existing="$(gcloud alpha monitoring policies list --project="$PROJECT" \
    --filter="display_name=\"${display}\"" --format='value(name)')"
  if [ -z "$existing" ]; then
    echo "==> 建立 alert policy「${display}」"
    gcloud alpha monitoring policies create --project="$PROJECT" \
      --policy-from-file="$TMP_POLICY"
  else
    echo "==> alert policy「${display}」已存在，略過"
  fi
}

# 4a. 每個 uptime check 一條「網站掛了」policy：
#     300s 視窗內（3 個 region、每分鐘一測）失敗次數 > 2 即觸發
for entry in "${CHECKS[@]}"; do
  IFS='|' read -r key host path <<<"$entry"
  check_id="$(uptime_check_id "uptime-${key}")"
  display="網站掛了：${host}${path}"
  cat > "$TMP_POLICY" <<EOF
{
  "displayName": "${display}",
  "combiner": "OR",
  "conditions": [
    {
      "displayName": "uptime check 失敗（${host}${path}）",
      "conditionThreshold": {
        "filter": "resource.type = \"uptime_url\" AND metric.type = \"monitoring.googleapis.com/uptime_check/check_passed\" AND metric.labels.check_id = \"${check_id}\"",
        "aggregations": [
          {
            "alignmentPeriod": "300s",
            "perSeriesAligner": "ALIGN_NEXT_OLDER",
            "crossSeriesReducer": "REDUCE_COUNT_FALSE",
            "groupByFields": ["resource.label.*"]
          }
        ],
        "comparison": "COMPARISON_GT",
        "thresholdValue": 2,
        "duration": "0s",
        "trigger": { "count": 1 }
      }
    }
  ],
  "alertStrategy": { "autoClose": "1800s" },
  "notificationChannels": ["${CHANNEL_ID}"]
}
EOF
  create_policy_if_absent "$display"
done

# 4b. 應用程式錯誤 log：.NET console log 進 Cloud Logging 後 severity 一律 INFO，
#     錯誤要靠行首的「fail: / crit:」辨識（stderr 的 severity>=ERROR 噪音太大，
#     nginx [notice] / Hydra 警告都會中，刻意不用）；另補 runtime 未捕捉例外的字樣。
display="應用程式錯誤 log（${NAMESPACE}）"
cat > "$TMP_POLICY" <<EOF
{
  "displayName": "${display}",
  "combiner": "OR",
  "conditions": [
    {
      "displayName": "服務輸出 fail:/crit: 或未捕捉例外",
      "conditionMatchedLog": {
        "filter": "resource.type=\"k8s_container\" AND resource.labels.namespace_name=\"${NAMESPACE}\" AND (textPayload:\"fail:\" OR textPayload:\"crit:\" OR textPayload:\"Unhandled exception\")"
      }
    }
  ],
  "alertStrategy": {
    "notificationRateLimit": { "period": "300s" },
    "autoClose": "1800s"
  },
  "notificationChannels": ["${CHANNEL_ID}"]
}
EOF
create_policy_if_absent "$display"

echo "==> 完成"
