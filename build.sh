#!/usr/bin/bash

mkdir .build || true
cd .build || return
makepkg -Cf --noconfirm
cd ..
