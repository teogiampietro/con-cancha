#!/usr/bin/env bash
# Carga los valores que .railway/railway.ts declara como preserve(): secretos y URLs públicas.
# Correr después de `railway config apply` y de generar el dominio público de cada servicio.
# Requisitos: `railway login` y `railway link` al proyecto.
#
# Uso:
#   API_DOMAIN=concancha-api.up.railway.app \
#   WEB_DOMAIN=concancha-web.up.railway.app \
#   EMAIL_FROM='ConCancha <no-responder@tudominio.com.ar>' \
#   RESEND_API_KEY=re_xxx \
#   scripts/railway-setup.sh

set -euo pipefail

API_SERVICE="concancha-api"
WEB_SERVICE="concancha-web"

require_env() {
  local name="$1"
  if [[ -z "${!name:-}" ]]; then
    echo "Falta la variable de entorno $name" >&2
    exit 1
  fi
}

set_var() {
  local service="$1" key="$2" value="$3"
  railway variable set "$key=$value" --service "$service" --skip-deploys >/dev/null
  echo "  $service: $key"
}

has_var() {
  local service="$1" key="$2"
  railway variable list --service "$service" --kv | grep -q "^$key="
}

require_env API_DOMAIN
require_env WEB_DOMAIN
require_env EMAIL_FROM
require_env RESEND_API_KEY

railway whoami >/dev/null
railway status >/dev/null

echo "Configurando $API_SERVICE"
set_var "$API_SERVICE" Web__BaseUrl "https://$WEB_DOMAIN"
set_var "$API_SERVICE" Email__From "$EMAIL_FROM"
set_var "$API_SERVICE" Email__ResendApiKey "$RESEND_API_KEY"

if has_var "$API_SERVICE" Jwt__Key; then
  echo "  Jwt__Key ya existe, no se regenera (regenerarla cerraría todas las sesiones)"
else
  set_var "$API_SERVICE" Jwt__Key "$(openssl rand -base64 48)"
fi

echo "Configurando $WEB_SERVICE"
set_var "$WEB_SERVICE" Api__BaseUrl "https://$API_DOMAIN"

echo "Listo. Las variables están cargadas sin disparar deploys: redespliega cada servicio desde el dashboard."
