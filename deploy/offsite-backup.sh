#!/bin/sh
# Copies the nightly database dumps off this machine. A backup that lives only on the server it protects
# is lost with that server. Nothing to install: rclone runs through Docker.
#
# One-time setup (creates a "remote" for whichever storage you use: Backblaze B2, Google Drive,
# OCI Object Storage, any S3-compatible bucket, an SFTP server...):
#   docker run --rm -it -v ~/.config/rclone:/config/rclone rclone/rclone config
#
# Then run it by hand, or from cron (see deploy/DEPLOY.md):
#   RCLONE_REMOTE=gla-backups:my-bucket/genericlearningapp ./deploy/offsite-backup.sh
#
# Settings (environment variables):
#   RCLONE_REMOTE           required. <remote-name>:<bucket-or-folder>
#   BACKUP_VOLUME           the Docker volume holding the dumps      (default: genericlearningapp_backups)
#   RCLONE_CONFIG_DIR       where your rclone config lives           (default: ~/.config/rclone)
#   REMOTE_RETENTION_DAYS   delete copies older than this off-site   (default: 60; 0 = keep forever)
#   RCLONE_DOCKER_ARGS      extra `docker run` arguments             (default: none)
set -eu

REMOTE="${RCLONE_REMOTE:?set RCLONE_REMOTE, for example gla-backups:my-bucket/genericlearningapp}"
VOLUME="${BACKUP_VOLUME:-genericlearningapp_backups}"
CONFIG_DIR="${RCLONE_CONFIG_DIR:-$HOME/.config/rclone}"
KEEP_DAYS="${REMOTE_RETENTION_DAYS:-60}"

if ! docker volume inspect "$VOLUME" >/dev/null 2>&1; then
	echo "[offsite] no Docker volume called $VOLUME. Run 'docker volume ls' and set BACKUP_VOLUME." >&2
	exit 1
fi

# shellcheck disable=SC2086
RCLONE="docker run --rm ${RCLONE_DOCKER_ARGS:-} -v $CONFIG_DIR:/config/rclone -v $VOLUME:/backups:ro rclone/rclone:latest"

echo "[offsite] $(date -u +%Y-%m-%dT%H:%M:%SZ) copying dumps to $REMOTE"

# copy, not sync: a file deleted locally by the 14-day cleanup stays safe off-site.
# Only the dated dumps; latest.dump is just a link to one of them.
$RCLONE copy /backups "$REMOTE" --include 'gla-*.dump' --ignore-existing -v

if [ "$KEEP_DAYS" != "0" ]; then
	$RCLONE delete "$REMOTE" --include 'gla-*.dump' --min-age "${KEEP_DAYS}d" -v
fi

echo "[offsite] done. Now on $REMOTE:"
$RCLONE lsl "$REMOTE" --include 'gla-*.dump'
