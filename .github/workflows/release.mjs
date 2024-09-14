import fs from "fs/promises";
import path from "path";
import { execSync } from "child_process";

const { modName, versionBumpType, changelogDescription } = process.env;
const versionFilePath = `./mods/${modName}/src/Mod.cs`;
const versionFileContent = await fs.readFile(versionFilePath, "utf8");

const versionRegex = /(VERSION\s*=\s*")(.+)(";)/;
const currentVersionMatch = versionFileContent.match(versionRegex);
if (!currentVersionMatch) throw new Error("Could not find version in Mod.cs");

const [, beforeVersion, currentVersion, afterVersion] = currentVersionMatch;
const [major, minor, patch] = currentVersion.split(".").map(Number);

const newVersion = {
  patch: `${major}.${minor}.${patch + 1}`,
  minor: `${major}.${minor + 1}.0`,
  major: `${major + 1}.0.0`,
}[versionBumpType.toLowerCase()];
if (!newVersion) throw new Error(`Unknown bump type: ${versionBumpType}`);

const newVersionContent = versionFileContent.replace(
  versionRegex,
  `${beforeVersion}${newVersion}${afterVersion}`
);
await fs.writeFile(versionFilePath, newVersionContent);

execSync(`git add "${versionFilePath}"`, { stdio: "inherit" });
execSync(`git commit -m "Release ${modName} v${newVersion}" --no-verify`, {
  stdio: "inherit",
});
execSync(`git push origin "${context.ref}" --no-verify`, { stdio: "inherit" });

const releaseAssetsDir = `./mods/${modName}/bin/Release`;
const filenames = await fs.readdir(releaseAssetsDir);

const tagName = `${modName}_v${newVersion}`;
await github.rest.git.createRef({
  owner: context.repo.owner,
  repo: context.repo.repo,
  ref: `refs/tags/${tagName}`,
  sha: context.sha,
});

const release = await github.repos.createRelease({
  owner: context.repo.owner,
  repo: context.repo.repo,
  tag_name: tagName,
  name: `${modName} v${newVersion}`,
  body:
    `[Link to README and code](https://github.com/jakzo/NeonWhiteMods/tree/main/mods/${modName})` +
    (changelogDescription ? `\n\n${changelogDescription}` : ""),
  draft: false,
  prerelease: false,
});

for (const filename of filenames) {
  await github.rest.repos.uploadReleaseAsset({
    owner: context.repo.owner,
    repo: context.repo.repo,
    release_id: release.id,
    name: filename,
    data: await fs.readFile(path.join(releaseAssetsDir, filename)),
  });
}
