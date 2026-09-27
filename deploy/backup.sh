#!/bin/sh
# Nightly pg_dump, kept for 14 days. Upload the newest file to OCI Object Storage
# (20 GB is free on an Always Free account) with a cron job on the host:
#   oci os object put --bucket-name gla-backups --file /var/lib/docker/volumes/.../latest.dump
set -eu

BACKUP_DIR=/backups
RETENTION_DAYS=14

while true; do
	STAMP=$(date -u +%Y%m%dT%H%M%SZ)
	echo "[backup] starting $STAMP"

	if pg_dump -h db -U gla -d genericlearningapp -Fc -f "$BACKUP_DIR/gla-$STAMP.dump"; then
		ln -sf "gla-$STAMP.dump" "$BACKUP_DIR/latest.dump"
		echo "[backup] wrote gla-$STAMP.dump"
		find "$BACKUP_DIR" -name 'gla-*.dump' -mtime "+$RETENTION_DAYS" -delete
	else
		echo "[backup] FAILED for $STAMP" >&2
	fi

	# Once a day, starting one day after the container comes up.
	sleep 86400
done
