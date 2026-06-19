#!/bin/bash

version="${1:-}"
csprojPath="${2:-src/StevanFreeborn.AsyncSyncFlagAnalyzer/StevanFreeborn.AsyncSyncFlagAnalyzer.csproj}"

if [[ -z "$version" ]]; then
  echo "Error: Version is required"
  exit 1
fi

content=$(cat "$csprojPath")

if [[ "$content" =~ \<Version\>.*?\</Version\> ]]; then
  content=$(echo "$content" | sed "s|<Version>.*</Version>|<Version>$version</Version>|")
elif [[ "$content" =~ \<PropertyGroup\> ]]; then
  content=$(echo "$content" | sed "s|<PropertyGroup>|<PropertyGroup>\\n    <Version>$version</Version>|")
fi

echo "$content" > "$csprojPath"
