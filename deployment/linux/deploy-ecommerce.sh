#!/usr/bin/env bash
set -Eeuo pipefail

PROJECT_DIR="${PROJECT_DIR:-/root/E-commerce-management}"
SLN="${SLN:-ECommerce.sln}"
WEB_CSPROJ="${WEB_CSPROJ:-src/ECommerce.Web/ECommerce.Web.csproj}"
SERVICE_NAME="${SERVICE_NAME:-ecommerce}"

PUBLISH_CURRENT="${PUBLISH_CURRENT:-/var/www/ecommerce}"
PUBLISH_NEW="${PUBLISH_NEW:-/var/www/ecommerce-new}"
PUBLISH_OLD="${PUBLISH_OLD:-/var/www/ecommerce-old}"
PUBLISH_BAD="${PUBLISH_BAD:-/var/www/ecommerce-bad}"
APP_OWNER="${APP_OWNER:-www-data:www-data}"

HOME_URL="${HOME_URL:-http://127.0.0.1:5000/}"
HEALTH_URL="${HEALTH_URL:-http://127.0.0.1:5000/health/ready}"
RUN_TESTS="${RUN_TESTS:-0}"
PULL_CODE="${PULL_CODE:-1}"
ALLOW_DIRTY="${ALLOW_DIRTY:-0}"

ROLLBACK_ONLY=0
TARGET_BRANCH=""

usage() {
  cat <<'EOF'
Usage:
  deploy-ecommerce.sh [branch]
  deploy-ecommerce.sh --rollback

Examples:
  /root/E-commerce-management/deployment/linux/deploy-ecommerce.sh
  /root/E-commerce-management/deployment/linux/deploy-ecommerce.sh feat-member1-foundation-oracle-deploy
  RUN_TESTS=1 /root/E-commerce-management/deployment/linux/deploy-ecommerce.sh
  /root/E-commerce-management/deployment/linux/deploy-ecommerce.sh --rollback

Environment overrides:
  PROJECT_DIR=/root/E-commerce-management
  SERVICE_NAME=ecommerce
  PUBLISH_CURRENT=/var/www/ecommerce
  HOME_URL=http://127.0.0.1:5000/
  HEALTH_URL=http://127.0.0.1:5000/health/ready
  RUN_TESTS=0|1
  PULL_CODE=0|1
  ALLOW_DIRTY=0|1
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

parse_args() {
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --rollback)
        ROLLBACK_ONLY=1
        shift
        ;;
      --no-pull)
        PULL_CODE=0
        shift
        ;;
      --run-tests)
        RUN_TESTS=1
        shift
        ;;
      --skip-tests)
        RUN_TESTS=0
        shift
        ;;
      -h|--help)
        usage
        exit 0
        ;;
      *)
        if [[ -n "$TARGET_BRANCH" ]]; then
          fail "Only one branch name is allowed."
        fi
        TARGET_BRANCH="$1"
        shift
        ;;
    esac
  done
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

rollback() {
  log "Rollback to previous published version"

  if [[ ! -d "$PUBLISH_OLD" ]]; then
    fail "Rollback directory does not exist: $PUBLISH_OLD"
  fi

  systemctl stop "$SERVICE_NAME" || true
  rm -rf "$PUBLISH_BAD"

  if [[ -d "$PUBLISH_CURRENT" ]]; then
    mv "$PUBLISH_CURRENT" "$PUBLISH_BAD"
  fi

  mv "$PUBLISH_OLD" "$PUBLISH_CURRENT"
  chown -R "$APP_OWNER" "$PUBLISH_CURRENT"
  systemctl start "$SERVICE_NAME"

  if ! systemctl is-active --quiet "$SERVICE_NAME"; then
    show_service_tail
    fail "Service failed after rollback."
  fi

  systemctl status "$SERVICE_NAME" --no-pager
  curl -I "$HOME_URL" || true
  echo "Rollback completed."
}

prepare_git() {
  log "Prepare source repository"
  cd "$PROJECT_DIR"

  if [[ "$ALLOW_DIRTY" != "1" ]] && [[ -n "$(git status --porcelain)" ]]; then
    git status --short
    fail "Working tree is dirty. Commit/stash changes or set ALLOW_DIRTY=1."
  fi

  if [[ "$PULL_CODE" == "1" ]]; then
    git fetch --all --prune
  fi

  if [[ -n "$TARGET_BRANCH" ]]; then
    if git show-ref --verify --quiet "refs/heads/$TARGET_BRANCH"; then
      git switch "$TARGET_BRANCH"
    elif git show-ref --verify --quiet "refs/remotes/origin/$TARGET_BRANCH"; then
      git switch --track "origin/$TARGET_BRANCH"
    else
      fail "Branch not found locally or on origin: $TARGET_BRANCH"
    fi
  fi

  if [[ "$PULL_CODE" == "1" ]]; then
    git pull --ff-only
  fi

  echo "Branch: $(git branch --show-current)"
  echo "Commit: $(git rev-parse --short HEAD)"
}

build_and_publish() {
  log "Restore dependencies"
  cd "$PROJECT_DIR"
  dotnet restore "$SLN"

  log "Build Release"
  dotnet build "$SLN" -c Release --no-restore

  if [[ "$RUN_TESTS" == "1" ]]; then
    log "Run tests"
    dotnet test "$SLN" -c Release --no-build
  fi

  log "Publish to temporary directory"
  rm -rf "$PUBLISH_NEW"
  mkdir -p "$PUBLISH_NEW"
  dotnet publish "$WEB_CSPROJ" -c Release -o "$PUBLISH_NEW" --no-build --no-restore
}

replace_current() {
  log "Replace published directory"
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
    echo "Service failed to start. Rolling back..."
    show_service_tail
    rollback
    exit 1
  fi

  log "Verify health endpoint"
  if ! wait_for_health; then
    echo "Health check failed: $HEALTH_URL"
    show_service_tail
    rollback
    exit 1
  fi

  log "Service status"
  systemctl status "$SERVICE_NAME" --no-pager

  log "Local HTTP check"
  curl -I "$HOME_URL" || true

  echo
  echo "Deployment completed successfully."
}

main() {
  parse_args "$@"
  require_root
  ensure_command git
  ensure_command dotnet
  ensure_command systemctl
  ensure_command curl

  if [[ "$ROLLBACK_ONLY" == "1" ]]; then
    rollback
    exit 0
  fi

  prepare_git
  build_and_publish
  replace_current
  start_and_verify
}

main "$@"
