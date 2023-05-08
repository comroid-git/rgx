#!/usr/bin/bash

set -e # exit on error

./clean.sh

# run tests first
dotnet test -c Test

# update SRCINFO
makepkg --printsrcinfo > .SRCINFO

# build the executable
makepkg -Cf --noconfirm

# push to aur
if [ -z "$(git remote | grep aur)" ]; then
  git remote add aur ssh://aur@aur.archlinux.org/rgx-git.git
fi
git push aur
