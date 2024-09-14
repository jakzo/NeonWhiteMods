import fs from "fs/promises";
import path from "path";

const projectName = context.payload.release.name;
const versionFilePath = `./mods/${projectName}/src/Mod.cs`;
const versionFileContent = await fs.readFile(versionFilePath, "utf8");

const currentVersion = versionFileContent.match(/VERSION\s*=\s*"(.*)";/)?.[1];
if (!currentVersion) throw new Error("Could not find version in the file.");

const releaseAssetsDir = `./mods/${projectName}/bin/Release`;
const filenames = await fs.readdir(releaseAssetsDir);

for (const filename of filenames) {
  await github.rest.repos.uploadReleaseAsset({
    owner: context.repo.owner,
    repo: context.repo.repo,
    release_id: context.payload.release.id,
    name: filename,
    data: await fs.readFile(path.join(releaseAssetsDir, filename)),
  });
}

await github.rest.git.createTag({
  owner: context.repo.owner,
  repo: context.repo.repo,
  tag: `${projectName}_${newVersion}`,
  message: `Release ${projectName} ${newVersion}`,
  object: context.sha,
  type: "commit",
});

await github.rest.git.createRef({
  owner: context.repo.owner,
  repo: context.repo.repo,
  ref: `refs/tags/${projectName}_${newVersion}`,
  sha: context.sha,
});

await github.rest.repos.updateRelease({
  owner: context.repo.owner,
  repo: context.repo.repo,
  release_id: context.payload.release.id,
  prerelease: false,
  name: `${projectName} ${newVersion}`,
  body: `[Link to README and code](https://github.com/jakzo/NeonWhiteMods/tree/main/mods/${projectName})\n\n${context.payload.release.body}`,
});
