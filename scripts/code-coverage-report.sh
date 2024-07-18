#!/bin/bash

# Usage: ./scripts/code-coverage-report.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEST_RESULTS_PATH=./TestResults

cd "$SCRIPT_DIR/../.."

echo -e "\e[1;32m Generating code coverage report...\e[0m"

dotnet reportgenerator \
    -reports:${TEST_RESULTS_PATH}/**/coverage.cobertura.xml \
    -targetdir:CoverageReports \
    -reporttypes:'Cobertura;TextSummary;Html'

cat CoverageReports/Summary.txt | grep -v '0.0%' | grep -v '^$' # code -

echo -e "\e[1;32m Code coverage report generated successfully.\e[0m"

echo -e "\n\n\e[1;30mOpening code coverage report in editor...\e[0m"

code -r CoverageReports/index.html
