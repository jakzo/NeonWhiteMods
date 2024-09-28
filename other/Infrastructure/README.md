This project contains the tooling for deploying server infrastructure.

## Local testing

Prerequisites:

- Install [Docker](https://www.docker.com/)
- Install [Pulumi](https://www.pulumi.com/docs/iac/download-install/)

Reset deployment state and start LocalStack (AWS but on your machine):

```
./other/Infrastructure/scripts/start-localstack.sh
 OR
.\other\Infrastructure\scripts\start-localstack.bat
```

Keep that running in the background then build and deploy to LocalStack:

```sh
dotnet build /p:PulumiStack=local
```

Test the deployed service:

```sh
curl http://URL_FROM_PULUMI_UP_OUTPUT/upload -H 'Content-Type: application/json' -d '{"LevelId":"TUT_GHOST_1","TimeMicroseconds":1234567,"FinishTimestamp":"2024-09-17T01:23:45.123456Z","Platform":"STEAM","UserId":"user123"}'
```

View your local database at: https://app.localstack.cloud
