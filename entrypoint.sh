#!/bin/sh
export $(cat /secrets/devicestreammodule/.env | xargs)
exec "$@"