#!/bin/bash

# Usage: ./scripts/code-coverage.sh

# firsst argument of script
PATH_TO_PROJECT=$1
shift

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEST_RESULTS_PATH=./TestResults

cd "$SCRIPT_DIR/../.."

rm -rf $TEST_RESULTS_PATH

echo -e "\e[1;32mRunning tests...\e[0m"

dotnet test \
    $PATH_TO_PROJECT \
    -l:trx \
    --results-directory $TEST_RESULTS_PATH \
    --collect:"XPlat Code Coverage" \
    --settings coverlet.settings.xml \
    "$@"
