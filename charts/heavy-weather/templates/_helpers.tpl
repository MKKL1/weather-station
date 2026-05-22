{{/*
Expand the name of the chart.
*/}}
{{- define "heavy-weather.name" -}}
{{- .Chart.Name | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "heavy-weather.fullname" -}}
{{- if contains .Chart.Name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name .Chart.Name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "heavy-weather.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "heavy-weather.labels" -}}
helm.sh/chart: {{ include "heavy-weather.chart" . }}
{{ include "heavy-weather.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "heavy-weather.selectorLabels" -}}
app.kubernetes.io/name: {{ include "heavy-weather.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Standard hardened container security context.
*/}}
{{- define "heavy-weather.containerSecurityContext" -}}
allowPrivilegeEscalation: false
readOnlyRootFilesystem: true
runAsNonRoot: true
runAsUser: {{ .securityContext.runAsUser | default 10000 }}
runAsGroup: {{ .securityContext.runAsGroup | default 10000 }}
capabilities:
  drop:
    - ALL
{{- end -}}


{{/*
Standard tmp volume
*/}}
{{- define "heavy-weather.tmpVolume" -}}
- name: tmp-volume
  emptyDir: {}
{{- end }}


{{/*
Selector labels for a specific component
*/}}
{{- define "heavy-weather.componentSelectorLabels" -}}
app.kubernetes.io/name: {{ include "heavy-weather.name" .ctx }}
app.kubernetes.io/instance: {{ .ctx.Release.Name }}
app.kubernetes.io/component: {{ .component }}
{{- end }}


{{/*
Standard DNS egress rule
*/}}
{{- define "heavy-weather.dnsEgress" -}}
- to:
    - namespaceSelector:
        matchLabels:
          kubernetes.io/metadata.name: kube-system
  ports:
    - protocol: UDP
      port: 53
    - protocol: TCP
      port: 53
{{- end }}


{{/*
Egress rule to a specific component
*/}}
{{- define "heavy-weather.egressToComponent" -}}
- to:
    - podSelector:
        matchLabels:
          {{- include "heavy-weather.componentSelectorLabels" (dict "ctx" .ctx "component" .component) | nindent 10 }}
  ports:
    - protocol: {{ .protocol | default "TCP" }}
      port: {{ .port }}
{{- end }}