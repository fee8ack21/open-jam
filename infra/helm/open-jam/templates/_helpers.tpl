{{/*
Expand the name of the chart.
*/}}
{{- define "open-jam.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "open-jam.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Chart label.
*/}}
{{- define "open-jam.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels.
*/}}
{{- define "open-jam.labels" -}}
helm.sh/chart: {{ include "open-jam.chart" . }}
{{ include "open-jam.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels.
*/}}
{{- define "open-jam.selectorLabels" -}}
app.kubernetes.io/name: {{ include "open-jam.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
PostgreSQL Service hostname.
*/}}
{{- define "open-jam.postgresHost" -}}
{{- printf "%s-postgres" (include "open-jam.fullname" .) }}
{{- end }}

{{/*
RabbitMQ Service hostname.
*/}}
{{- define "open-jam.rabbitmqHost" -}}
{{- printf "%s-rabbitmq" (include "open-jam.fullname" .) }}
{{- end }}

{{/*
Hydra Admin URL (cluster-internal).
*/}}
{{- define "open-jam.hydraAdminUrl" -}}
{{- printf "http://%s-hydra:4445" (include "open-jam.fullname" .) }}
{{- end }}

{{/*
Hydra Public API URL (cluster-internal)，for 服務間 client_credentials 取 token 使用。
*/}}
{{- define "open-jam.hydraInternalPublicUrl" -}}
{{- printf "http://%s-hydra:4444" (include "open-jam.fullname" .) }}
{{- end }}

{{/*
SMTP host: mailpit service name when mailpit is enabled, otherwise smtp.host.
*/}}
{{- define "open-jam.smtpHost" -}}
{{- if .Values.mailpit.enabled }}
{{- printf "%s-mailpit" (include "open-jam.fullname" .) }}
{{- else }}
{{- .Values.smtp.host }}
{{- end }}
{{- end }}

{{/*
Hydra Public URL (cluster-external).
*/}}
{{- define "open-jam.hydraPublicUrl" -}}
{{- .Values.config.hydraPublicUrl }}
{{- end }}

{{/*Hydra OIDC discovery URL (cluster-external)，for JWKS 驗證使用。
*/}}
{{- define "open-jam.hydraMetadataAddress" -}}
{{- printf "%s.well-known/openid-configuration" (include "open-jam.hydraPublicUrl" .) }}
{{- end }}

{{/*
StorageService Service URL (cluster-internal).
*/}}
{{- define "open-jam.storageServiceUrl" -}}
{{- printf "http://%s-storage-service:8080" (include "open-jam.fullname" .) }}
{{- end }}

{{/*
StoreService Service URL (cluster-internal).
*/}}
{{- define "open-jam.storeServiceUrl" -}}
{{- printf "http://%s-store-service:8080" (include "open-jam.fullname" .) }}
{{- end }}

{{/*
CatalogService Service URL (cluster-internal).
*/}}
{{- define "open-jam.catalogServiceUrl" -}}
{{- printf "http://%s-catalog-service:8080" (include "open-jam.fullname" .) }}
{{- end }}

{{/*
QuotaService Service URL (cluster-internal).
*/}}
{{- define "open-jam.quotaServiceUrl" -}}
{{- printf "http://%s-quota-service:8080" (include "open-jam.fullname" .) }}
{{- end }}

{{/*
PaymentService Service URL (cluster-internal).
*/}}
{{- define "open-jam.paymentServiceUrl" -}}
{{- printf "http://%s-payment-service:8080" (include "open-jam.fullname" .) }}
{{- end }}

{{/*
OrderService Service URL (cluster-internal).
*/}}
{{- define "open-jam.orderServiceUrl" -}}
{{- printf "http://%s-order-service:8080" (include "open-jam.fullname" .) }}
{{- end }}

{{/*
ContentService Service URL (cluster-internal).
*/}}
{{- define "open-jam.contentServiceUrl" -}}
{{- printf "http://%s-content-service:8080" (include "open-jam.fullname" .) }}
{{- end }}

{{/*
Secret name.
*/}}
{{- define "open-jam.secretName" -}}
{{- printf "%s-secret" (include "open-jam.fullname" .) }}
{{- end }}
