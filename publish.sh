#!/usr/bin/bash

set -e # exit on error

./clean.sh

# run tests first
dotnet test -c Test

# build the executable
makepkg -f --noconfirm

# update SRCINFO
makepkg --printsrcinfo > .SRCINFO
(git add .SRCINFO && git commit -m "SRCINFO" && git push) || (echo "Failed to commit .SRCINFO" >&2 | return)

# push to aur
if [ -z "$(git remote | grep aur)" ]; then
  git remote add aur ssh://aur@aur.archlinux.org/rgx-git.git
fi
git push aur
