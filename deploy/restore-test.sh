#!/bin/sh
# Proves a backup is usable WITHOUT touching live data: restores a dump into a scratch database, prints
# what is inside, then deletes the scratch database. An untested backup is only a hope, so run this
# after setting up backups and every month or so.
#
# Run from the repository folder on the server:
#   ./deploy/restore-test.sh                      # newest dump
#   ./deploy/restore-test.sh gla-20260920T030000Z.dump
#
# To actually restore a live site from a backup, follow "Recovering from a disaster" in deploy/DEPLOY.md.
set -eu

DUMP="${1:-latest.dump}"
SCRATCH=gla_restore_test
RUN="docker compose exec -T backup"

$RUN test -e "/backups/$DUMP" || { echo "No such dump in the backups volume: $DUMP" >&2; $RUN ls /backups >&2; exit 1; }

echo "Restoring $DUMP into scratch database $SCRATCH ..."
$RUN sh -c "dropdb -h db -U gla --if-exists $SCRATCH && createdb -h db -U gla $SCRATCH && pg_restore -h db -U gla -d $SCRATCH --no-owner /backups/$DUMP"

echo
echo "What is in the backup:"
$RUN psql -h db -U gla -d "$SCRATCH" -c "
SELECT 'people'                AS what, count(*) AS how_many FROM \"AspNetUsers\" WHERE \"UserName\" NOT LIKE 'guest-%'
UNION ALL SELECT 'guest accounts',       count(*) FROM \"AspNetUsers\" WHERE \"UserName\" LIKE 'guest-%'
UNION ALL SELECT 'super admin accounts', count(*) FROM \"AspNetUsers\" WHERE \"IsSuperAdmin\"
UNION ALL SELECT 'subjects',             count(*) FROM subjects
UNION ALL SELECT 'notes',                count(*) FROM notes
UNION ALL SELECT 'menus',                count(*) FROM menu_items
UNION ALL SELECT 'login keys',           count(*) FROM \"DataProtectionKeys\";"

$RUN dropdb -h db -U gla "$SCRATCH"
echo "Scratch database removed. Live data was not touched."
