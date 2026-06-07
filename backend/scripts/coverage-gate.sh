#!/usr/bin/env bash
# T136 — coverage gate for Domain + Application assemblies.
# Requires: dotnet 10 SDK, dotnet-reportgenerator-globaltool installed and on PATH.
#
# Usage:
#   ./scripts/coverage-gate.sh                       # uses default threshold (70)
#   COVERAGE_THRESHOLD=80 ./scripts/coverage-gate.sh # custom threshold
#
# CI behaviour:
#   - Cleans previous TestResults / coverage-report folders.
#   - Runs `dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings`.
#   - Aggregates cobertura reports with ReportGenerator.
#   - Fails (exit 1) if combined line coverage < $COVERAGE_THRESHOLD.

set -euo pipefail

THRESHOLD="${COVERAGE_THRESHOLD:-70}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
BACKEND_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$BACKEND_DIR"

# Ensure ReportGenerator is on PATH (default install path for global tools).
if ! command -v reportgenerator >/dev/null 2>&1; then
  export PATH="$PATH:$HOME/.dotnet/tools"
fi

if ! command -v reportgenerator >/dev/null 2>&1; then
  echo "ERROR: reportgenerator not found. Install with:" >&2
  echo "  dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.4.4" >&2
  exit 2
fi

echo "==> Cleaning previous coverage artifacts"
rm -rf tests/ExpenseTracker.Domain.Tests/TestResults \
       tests/ExpenseTracker.Application.Tests/TestResults \
       coverage-report

echo "==> Running Domain + Application tests with coverage"
dotnet test tests/ExpenseTracker.Domain.Tests \
  --nologo \
  --collect:"XPlat Code Coverage" \
  --settings coverlet.runsettings \
  /clp:ErrorsOnly
dotnet test tests/ExpenseTracker.Application.Tests \
  --nologo \
  --collect:"XPlat Code Coverage" \
  --settings coverlet.runsettings \
  /clp:ErrorsOnly

echo "==> Aggregating coverage"
reportgenerator \
  -reports:"tests/ExpenseTracker.Domain.Tests/TestResults/**/coverage.cobertura.xml;tests/ExpenseTracker.Application.Tests/TestResults/**/coverage.cobertura.xml" \
  -targetdir:coverage-report \
  -reporttypes:"TextSummary;Cobertura;Html" \
  -assemblyfilters:"+ExpenseTracker.Domain;+ExpenseTracker.Application" \
  >/dev/null

SUMMARY="coverage-report/Summary.txt"
cat "$SUMMARY"

# Parse "Line coverage: 73.5%" from the TextSummary.
LINE_COV=$(grep -E "^[[:space:]]+Line coverage:" "$SUMMARY" | head -1 | sed -E 's/.*: *([0-9]+\.[0-9]+)%.*/\1/')

if [[ -z "$LINE_COV" ]]; then
  echo "ERROR: could not parse line coverage from $SUMMARY" >&2
  exit 3
fi

echo ""
echo "Combined Domain+Application line coverage: ${LINE_COV}% (threshold: ${THRESHOLD}%)"

# Compare using awk (portable across macOS/Linux without bc).
if awk "BEGIN { exit !(${LINE_COV} < ${THRESHOLD}) }"; then
  echo "FAIL: line coverage ${LINE_COV}% is below the required ${THRESHOLD}% gate." >&2
  exit 1
fi

echo "PASS: coverage gate satisfied."
