#!/usr/bin/env bash
#
# Create a new Sorolla game project from UnityGITTemplate.
#
#   ./scripts/new-project.sh puzzlequest "PuzzleQuest" com.sorolla.puzzlequest
#
# Clones the template with its Core submodule, renames the project, creates a
# private GitHub repo, pushes, and verifies with a cold Unity import.
#
# See the "Starting a new project" section of CLAUDE.md.

set -euo pipefail

TEMPLATE_URL="https://github.com/rodsorolla/UnityGitTemplate.git"
GITHUB_ORG="sorolla-studio"
PARENT_DIR="$HOME/Documents/Git"

die() { printf '\033[31merror:\033[0m %s\n' "$1" >&2; exit 1; }
step() { printf '\n\033[1m==> %s\033[0m\n' "$1"; }

# ---------------------------------------------------------------- arguments
if [ $# -lt 2 ]; then
  cat >&2 <<EOF
usage: $0 <repo-name> <ProductName> [bundle-id]

  repo-name     lowercase, used for the directory and GitHub repo (e.g. puzzlequest)
  ProductName   Unity productName, shown to players (e.g. "Puzzle Quest")
  bundle-id     defaults to com.sorolla.<repo-name>

example:
  $0 puzzlequest "Puzzle Quest"
EOF
  exit 64
fi

NAME="$1"
PROD="$2"
BUNDLE_ID="${3:-com.sorolla.$NAME}"
DEST="$PARENT_DIR/$NAME"

[[ "$NAME" =~ ^[a-z0-9][a-z0-9-]*$ ]] \
  || die "repo name must be lowercase alphanumeric with dashes, got '$NAME'"
[ -e "$DEST" ] && die "$DEST already exists"
command -v gh >/dev/null || die "gh CLI not found"
gh auth status >/dev/null 2>&1 || die "gh not authenticated — run: gh auth login"
gh repo view "$GITHUB_ORG/$NAME" >/dev/null 2>&1 \
  && die "$GITHUB_ORG/$NAME already exists on GitHub"

# ---------------------------------------------------------------- clone
step "Cloning template into $DEST"
git clone --recurse-submodules "$TEMPLATE_URL" "$DEST"
cd "$DEST"

# The submodule lands on a detached HEAD; put it on main so Core commits
# made inside this project belong to a branch.
git -C Packages/com.sorolla.core checkout main
git config submodule.recurse true

[ -f Packages/com.sorolla.core/package.json ] \
  || die "Core submodule is empty — clone did not recurse"

# ---------------------------------------------------------------- rename
step "Renaming to $PROD ($BUNDLE_ID)"
python3 - "$PROD" "$BUNDLE_ID" <<'PY'
import re, sys
prod, bundle = sys.argv[1], sys.argv[2]
p = "ProjectSettings/ProjectSettings.asset"
s = open(p).read()

s, n = re.subn(r"^  productName: .*$", f"  productName: {prod}", s, count=1, flags=re.M)
assert n == 1, "productName not found"

# applicationIdentifier is a nested block; rewrite every platform under it.
def platforms(m):
    body = re.sub(r"^(    \w+): .*$", rf"\1: {bundle}", m.group(2), flags=re.M)
    return m.group(1) + body
s, n = re.subn(r"(^  applicationIdentifier:\n)((?:    \w+: .*\n)+)",
               platforms, s, count=1, flags=re.M)
assert n == 1, "applicationIdentifier block not found"

open(p, "w").write(s)
print(f"  productName -> {prod}")
print(f"  applicationIdentifier -> {bundle} (all platforms)")
PY

sed -i '' "1s|^# .*|# $PROD — Unity Game Project|" CLAUDE.md
cat > tasks/todo.md <<EOF
# Tasks

_No active task. Plans go here before work starts (see CLAUDE.md rule 1)._
EOF

# ---------------------------------------------------------------- github
step "Creating $GITHUB_ORG/$NAME (private)"
gh repo create "$GITHUB_ORG/$NAME" --private -d "$PROD — Unity game built on the Sorolla template"
git remote set-url origin "https://github.com/$GITHUB_ORG/$NAME.git"
git remote add template "$TEMPLATE_URL"

git add -A
git commit -q -m "chore: rebrand template clone as $PROD

productName -> $PROD, applicationIdentifier -> $BUNDLE_ID on all platforms,
reset tasks/todo.md, CLAUDE.md title."
git push -q -u origin main

# ---------------------------------------------------------------- verify
step "Verifying with a cold Unity import (this takes a few minutes)"
UNITY_VERSION=$(awk '/^m_EditorVersion:/ {print $2}' ProjectSettings/ProjectVersion.txt)
UNITY="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity"

if [ ! -x "$UNITY" ]; then
  printf '\033[33mskipped:\033[0m Unity %s not installed at %s\n' "$UNITY_VERSION" "$UNITY"
  printf 'Open the project manually and check the Console.\n'
  exit 0
fi

LOG="$DEST/Logs/new-project-import.log"
mkdir -p "$(dirname "$LOG")"
"$UNITY" -batchmode -nographics -quit -projectPath "$DEST" -logFile "$LOG" || true

ERRORS=$(grep -cE "error CS[0-9]+" "$LOG" || true)
CONFLICTS=$(grep -c "conflicts with" "$LOG" || true)
ASSEMBLIES=$(ls "$DEST"/Library/ScriptAssemblies/Sorolla*.dll 2>/dev/null | wc -l | tr -d ' ')

step "Result"
printf '  compile errors:      %s\n' "$ERRORS"
printf '  GUID conflicts:      %s\n' "$CONFLICTS"
printf '  Sorolla assemblies:  %s\n' "$ASSEMBLIES"

if [ "$ERRORS" != "0" ] || [ "$CONFLICTS" != "0" ]; then
  printf '\n\033[31mImport reported problems.\033[0m First few:\n'
  grep -E "error CS[0-9]+|conflicts with" "$LOG" | sort -u | head -5
  printf '\nFull log: %s\n' "$LOG"
  exit 1
fi

printf '\n\033[32mReady:\033[0m %s -> https://github.com/%s/%s\n' "$DEST" "$GITHUB_ORG" "$NAME"
