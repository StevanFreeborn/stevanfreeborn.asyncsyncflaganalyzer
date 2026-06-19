#!/bin/bash

branch="${1:-main}"

tag=$(git describe --tags --abbrev=0 2>/dev/null)
base="${tag:-$branch}"

commits=$(git log "$base..HEAD" --format="%s")
major=0
minor=0
patch=0

while IFS= read -r message; do
  if [[ -n "$message" ]] && [[ "$message" =~ " BREAKING CHANGE|!:" ]]; then
    major=1
  elif [[ -n "$message" ]] && [[ "$message" =~ feat(\(.*\))?: ]]; then
    minor=1
  elif [[ -n "$message" ]] && [[ "$message" =~ fix(\(.*\))?: ]]; then
    patch=1
  fi
done <<< "$commits"

if [[ "$major" -eq 1 ]]; then
  minor=0
  patch=0
elif [[ "$minor" -eq 1 ]]; then
  patch=0
elif [[ "$patch" -eq 0 ]] && [[ -n "$commits" ]]; then
  patch=1
fi

currentVersion="0.0.0"
hasExistingTag=false

if [[ -n "$tag" ]]; then
  hasExistingTag=true
  currentVersion="${tag#v}"
fi

IFS='.' read -r majorCurrent minorCurrent patchCurrent <<< "$currentVersion"

majorNew=$((majorCurrent + major))

if [[ "$major" -eq 1 ]]; then
  minorNew=0
else
  minorNew=$((minorCurrent + minor))
fi

if [[ "$major" -eq 1 ]] || [[ "$minor" -eq 1 ]]; then
  patchNew=0
else
  patchNew=$((patchCurrent + patch))
fi

if [[ "$majorNew" -eq "$majorCurrent" ]] && [[ "$minorNew" -eq "$minorCurrent" ]] && [[ "$patchNew" -eq "$patchCurrent" ]]; then
  if [[ "$hasExistingTag" == "false" ]]; then
    version="0.0.0"
    [[ -n "$GITHUB_OUTPUT" ]] && echo "VERSION=$version" >> "$GITHUB_OUTPUT" || echo "VERSION=$version"
    echo "First release: publishing version 0.0.0"
  else
    exit 0
  fi
else
  version="$majorNew.$minorNew.$patchNew"
  [[ -n "$GITHUB_OUTPUT" ]] && echo "VERSION=$version" >> "$GITHUB_OUTPUT" || echo "VERSION=$version"
fi
