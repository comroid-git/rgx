#!/usr/bin/bash

set -e

./clean.sh
./build.sh || echo "Build failed" >&2
./exec.sh || echo "Execute failed" >&2
./clean.sh
