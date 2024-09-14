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

const script = `

dotnet build -c Release

git config --global user.name "github-actions[bot]"
git config --global user.email "github-actions[bot]@users.noreply.github.com"
git add "${versionFilePath}"
git commit -m "Release ${modName} v${newVersion}" --no-verify
git push origin "${context.ref}" --no-verify

`;

for (const line of script.split("\n")) {
  if (!line.trim()) continue;
  console.log("Running command:", line);
  execSync(line, { stdio: "inherit" });
}

const modDir = path.join("mods", modName);
const releaseAssetsDir = path.join(modDir, "bin/Release");
const filenames = await fs.readdir(releaseAssetsDir);

const tagName = `${modName}_v${newVersion}`;
await github.rest.git.createRef({
  owner: context.repo.owner,
  repo: context.repo.repo,
  ref: `refs/tags/${tagName}`,
  sha: execSync("git rev-parse HEAD").toString().trim(),
});

const readme = await fs.readFile(path.join(modDir, "README.md"), "utf8");
const [readmeFirstChar] = readme;
const emoji = readmeFirstChar.length > 0 ? readmeFirstChar : "";

const installation = await fs.readFile("./support/INSTALLATION.md", "utf8");

const releaseName = `${emoji}${emoji && " "}${modName} v${newVersion}`;
const release = await github.rest.repos.createRelease({
  owner: context.repo.owner,
  repo: context.repo.repo,
  tag_name: tagName,
  draft: false,
  prerelease: false,
  name: releaseName,
  body: [
    "# Changelog",
    `[💻 CODE](https://github.com/jakzo/NeonWhiteMods/tree/${tagName}/mods/${modName})`,
    changelogDescription,
    "# Readme",
    readme,
    "# Installation",
    installation,
  ].join("\n\n"),
});

core.summary.addLink(`Release ${releaseName}`, release.data.html_url);
await core.summary.write();

for (const filename of filenames) {
  await github.rest.repos.uploadReleaseAsset({
    owner: context.repo.owner,
    repo: context.repo.repo,
    release_id: release.data.id,
    name: filename,
    data: await fs.readFile(path.join(releaseAssetsDir, filename)),
  });
}
