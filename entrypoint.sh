#!/bin/bash

cd /usr/share/elasticsearch/ && ./main &

exec /usr/local/bin/docker-entrypoint.sh $@
