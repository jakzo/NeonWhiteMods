set -eux
cd "$(dirname "$0")/.."

export PULUMI_CONFIG_PASSPHRASE=""

pulumi stack select local || pulumi stack init local
pulumi stack import --file ./scripts/empty-stack.json --force
docker-compose up --no-log-prefix
