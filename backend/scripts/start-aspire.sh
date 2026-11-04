#!/bin/zsh
set -euo pipefail

result=$("$HOME/.dotnet/tools/aspire" run --detach --format Json --nologo)
dashboard_url=$(printf '%s' "$result" | node -e '
let output = "";
process.stdin.on("data", chunk => output += chunk);
process.stdin.on("end", () => {
  const json = JSON.parse(output.slice(output.lastIndexOf("{")));
  process.stdout.write(json.dashboardUrl);
});
')

open -a "Google Chrome" "$dashboard_url"
printf 'Aspire dashboard: %s\n' "$dashboard_url"
