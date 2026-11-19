#!/usr/bin/env bash
set -euo pipefail

apphost=src/OpsBoard.AppHost/OpsBoard.AppHost.csproj
apphost_pid=

aspire() {
    local command=$1
    shift
    dotnet tool run aspire -- "$command" --apphost "$apphost" --non-interactive --nologo "$@"
}

cleanup() {
    local status=$?
    trap - EXIT

    if [[ -n "$apphost_pid" ]]; then
        if (( status != 0 )); then
            for resource in postgres data-generator api scenario-runner; do
                aspire logs "$resource" --tail 30 >&2 || true
            done
        fi
        aspire stop --pid "$apphost_pid" || true
    fi

    exit "$status"
}

trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

apphost_pid=$(aspire run --detach --format Json --isolated --no-build -- \
    --Scenarios:Run=true --Dataset:Persist=false | jq -er '.appHostPid')

deadline=$((SECONDS + 180))
while (( SECONDS < deadline )); do
    runner=$(aspire describe --format Json | jq -c \
        '.resources[] | select(.displayName == "scenario-runner") | {state, exitCode}')
    state=$(jq -r '.state' <<< "$runner")

    case "$state" in
        Finished)
            jq -e '.exitCode == 0' <<< "$runner" >/dev/null
            aspire logs scenario-runner --format Json | jq -r '.logs[].content'
            exit 0
            ;;
        Exited|FailedToStart|Failed|Stopped)
            echo "Scenario runner ended in state $state." >&2
            exit 1
            ;;
    esac

    sleep 2
done

echo 'Timed out waiting for the scenario runner.' >&2
exit 1
