#!/usr/bin/bash

set -e

./clean.sh
dotnet publish -c Release --use-current-runtime || echo "Build failed" >&2
./exec.sh || echo "Execute failed" >&2
./clean.sh
