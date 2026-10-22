#!/usr/bin/env bash
# Copia de seguridad lógica de la base. Uso: DATABASE_URL=postgres://... scripts/backup-db.sh [carpeta]
set -euo pipefail

: "${DATABASE_URL:?Definí DATABASE_URL con la URL de conexión de PostgreSQL}"

target_dir="${1:-backups}"
mkdir -p "$target_dir"
file="$target_dir/concancha-$(date -u +%Y%m%dT%H%M%SZ).dump"

pg_dump --format=custom --no-owner --file="$file" "$DATABASE_URL"
echo "Copia creada: $file"
