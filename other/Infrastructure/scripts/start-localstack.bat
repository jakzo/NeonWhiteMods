@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0\.."

set PULUMI_CONFIG_PASSPHRASE=

pulumi stack select local
if errorlevel 1 ( pulumi stack init local )
pulumi stack import --file .\scripts\empty-stack.json --force
docker-compose up --no-log-prefix
