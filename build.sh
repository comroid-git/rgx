#!/usr/bin/bash

set -e

mkdir .build
cd build
makepkg -Cf --noconfirm
cd ..
