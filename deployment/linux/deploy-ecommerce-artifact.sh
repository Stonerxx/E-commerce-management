#!/usr/bin/env bash
set -Eeuo pipefail

ARTIFACT="${1:-}"

SERVICE_NAME="${SERVICE_NAME:-ecommerce}"
PUBLISH_CURRENT="${PUBLISH_CURRENT:-/var/www/ecommerce}"
PUBLISH_NEW="${PUBLISH_NEW:-/var/www/ecommerce-new}"
PUBLISH_OLD="${PUBLISH_OLD:-/var/www/ecommerce-old}"
PUBLISH_BAD="${PUBLISH_BAD:-/var/www/ecommerce-bad}"
APP_OWNER="${APP_OWNER:-www-data:www-data}"

HOME_URL="${HOME_URL:-http://127.0.0.1:5000/}"
HEALTH_URL="${HEALTH_URL:-http://127.0.0.1:5000/health/ready}"

usage() {
  cat <<'EOF'
Usage:
  deploy-ecommerce-artifact.sh /path/to/ecommerce-release.tar.gz

This script does not restore, build, or publish source code.
It only extracts a GitHub Actions publish artifact, swaps /var/www/ecommerce,
restarts systemd, and rolls back if the service or health check fails.
EOF
}

log() {
  echo
  echo "== $* =="
}

fail() {
  echo "ERROR: $*" >&2
  exit 1
}

require_root() {
  if [[ "${EUID}" -ne 0 ]]; then
    fail "Run this script as root, for example: sudo $0"
  fi
}

ensure_command() {
  command -v "$1" >/dev/null 2>&1 || fail "Required command not found: $1"
}

show_service_tail() {
  journalctl -u "$SERVICE_NAME" -n 80 --no-pager || true
}

wait_for_health() {
  local attempt body
  for attempt in {1..15}; do
    if body="$(curl -fsSL --max-time 10 "$HEALTH_URL")" \
      && grep -Eq '"success"[[:space:]]*:[[:space:]]*true' <<<"$body" \
      && grep -Eq '"connected"[[:space:]]*:[[:space:]]*true' <<<"$body"; then
      return 0
    fi
    sleep 2
  done
  return 1
}

rollback_after_failed_deploy() {
  echo "Deployment failed. Rolling back..."

  systemctl stop "$SERVICE_NAME" || true
  rm -rf "$PUBLISH_BAD"

  if [[ -d "$PUBLISH_CURRENT" ]]; then
    mv "$PUBLISH_CURRENT" "$PUBLISH_BAD"
  fi

  if [[ -d "$PUBLISH_OLD" ]]; then
    mv "$PUBLISH_OLD" "$PUBLISH_CURRENT"
    chown -R "$APP_OWNER" "$PUBLISH_CURRENT"
    systemctl start "$SERVICE_NAME" || true
  fi

  show_service_tail
  fail "Deployment failed and rollback was attempted."
}

validate_input() {
  if [[ "$ARTIFACT" == "-h" || "$ARTIFACT" == "--help" ]]; then
    usage
    exit 0
  fi

  [[ -n "$ARTIFACT" ]] || fail "Usage: $0 /path/to/ecommerce-release.tar.gz"
  [[ -f "$ARTIFACT" ]] || fail "Artifact not found: $ARTIFACT"
}

extract_artifact() {
  log "Validate artifact"
  tar -tzf "$ARTIFACT" >/dev/null

  log "Extract artifact to temporary publish directory"
  rm -rf "$PUBLISH_NEW"
  mkdir -p "$PUBLISH_NEW"
  tar -xzf "$ARTIFACT" -C "$PUBLISH_NEW"

  [[ -f "$PUBLISH_NEW/ECommerce.Web.dll" ]] || fail "ECommerce.Web.dll not found in artifact."
  chown -R "$APP_OWNER" "$PUBLISH_NEW"
}

replace_current() {
  log "Replace current published directory"
  mkdir -p "$(dirname "$PUBLISH_CURRENT")"
  systemctl stop "$SERVICE_NAME" || true

  rm -rf "$PUBLISH_OLD"

  if [[ -d "$PUBLISH_CURRENT" ]]; then
    mv "$PUBLISH_CURRENT" "$PUBLISH_OLD"
  fi

  mv "$PUBLISH_NEW" "$PUBLISH_CURRENT"
  chown -R "$APP_OWNER" "$PUBLISH_CURRENT"
}

start_and_verify() {
  log "Start service"
  systemctl daemon-reload
  systemctl start "$SERVICE_NAME"

  if ! systemctl is-active --quiet "$SERVICE_NAME"; then
    rollback_after_failed_deploy
  fi

  log "Verify health endpoint"
  if ! wait_for_health; then
    rollback_after_failed_deploy
  fi

  log "Service status"
  systemctl status "$SERVICE_NAME" --no-pager

  log "Local HTTP check"
  curl -I "$HOME_URL" || true

  echo
  echo "Artifact deployment completed successfully."
}

main() {
  require_root
  validate_input
  ensure_command tar
  ensure_command systemctl
  ensure_command curl

  extract_artifact
  replace_current
  start_and_verify
}

main "$@"
