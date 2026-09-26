#!/bin/bash
set -e

# Keep the at job queue on the /data volume so scheduled recordings survive redeploys
ATJOBS=/var/spool/cron/atjobs
PERSISTED=/data/atjobs
if [ ! -L "$ATJOBS" ]; then
    mkdir -p "$PERSISTED"
    # seed with the image's spool (it contains the .SEQ file that at needs)
    cp -an "$ATJOBS/." "$PERSISTED/"
    rm -rf "$ATJOBS"
    ln -s "$PERSISTED" "$ATJOBS"
fi
[ -e "$PERSISTED/.SEQ" ] || touch "$PERSISTED/.SEQ"
chown -R daemon:daemon "$PERSISTED"
chmod 1770 "$PERSISTED"

/usr/sbin/atd
exec dotnet ipvcr.Web.dll
