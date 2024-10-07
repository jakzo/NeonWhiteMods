import { basename, join } from "https://deno.land/std/path/mod.ts";
import { exists } from "https://deno.land/std/fs/mod.ts";
import { createReadStream } from "node:fs";
import { google } from "npm:googleapis@144.0.0";
import open from "npm:open@10.1.0";

const PLAYLIST_TITLE = "Neon White";

const LEVELS = `
Movement
Pummel
Gunner
Cascade
Elevate
Bounce
Purify
Climb
Fasttrack
Glass Port
Take flight
Godspeed
Dasher
Thrasher
Outstretched
Smackdown
Catwalk
Fastlane
Distinguish
Dancer
Guardian
Stomp
Jumper
Dash Tower
Descent
Driller
Canals
Sprint
Mountain
Superkinetic
Arrival
Forgotten City
The Clocktower
Fireball
Ringer
Cleaner
Warehouse
Boom
Streets
Steps
Demolition
Arcs
Apartment
Hanging Gardens
Tangled
Waterworks
Killswitch
Falling
Shocker
Bouquet
Prepare
Triptrack
Race
Bubble
Shield
Overlook
Pop
Minefield
Mimic
Trigger
Greenhouse
Sweep
Fuse
Heaven's Edge
Zipline
Swing
Chute
Crash
Ascent
Straightaway
Firecracker
Streak
Mirror
Escalation
Bolt
Godstreak
Plunge
Mayhem
Barrage
Estate
Trapwire
Ricochet
Fortress
Holy Ground
The Third Temple
Spree
Breakthrough
Glide
Closer
Hike
Switch
Access
Congregation
Sequence
Marathon
Sacrifice
Absolution
Elevate Traversal I
Elevate Traversal II
Purify Traversal
Godspeed Traversal
Stomp Traversal
Fireball Traversal
Dominion Traversal
Book of Life Traversal
Doghouse
Choker
Chain
Hellevator
Razor
All Seeing Eye
Resident Saw I
Resident Saw II
Sunset Flip Powerbomb
Balloon Mountain
Climbing Gym
Fisherman Suplex
STF
Arena
Attitude Adjustment
Rocket
`.trim().split("\n");

const tokenPath = join(import.meta.dirname!, "generated-tokens.json");

const [videoFolder = "C:\\Users\\jakzo\\Videos\\Neon White"] = Deno.args;
if (!videoFolder) {
    console.error(
        "Usage: deno --allow-all ./scripts/upload-to-youtube.ts [path_to_neon_white_videos_folder]",
    );
    Deno.exit(1);
}

let oauthCodeResolve: (oauthCode: string) => void;
let oauthCodeReject: (err: unknown) => void;
const oauthCode = new Promise<string>((resolve, reject) => {
    oauthCodeResolve = resolve;
    oauthCodeReject = reject;
});

const server = Deno.serve({ port: 0 }, (req) => {
    try {
        const url = new URL(req.url, "http://localhost");
        const code = url.searchParams.get("code");
        if (!code) {
            return new Response("No OAuth2 code provided", { status: 400 });
        }

        oauthCodeResolve(code);

        return new Response(
            "<html> <body> You can now close this window. <script>window.close();</script> </body> </html>",
            {
                status: 200,
                headers: { "Content-Type": "text/html" },
            },
        );
    } catch (err) {
        console.error("Error handling request:", err);
        oauthCodeReject(err);
        return new Response("Internal Server Error", { status: 500 });
    }
});

const credentials = JSON.parse(
    await Deno.readTextFile(join(import.meta.dirname!, "credentials.json")),
);
const { client_secret, client_id } = credentials.installed;
const oauth2Client = new google.auth.OAuth2(
    client_id,
    client_secret,
    `http://localhost:${server.addr.port}`,
);

if (await exists(tokenPath)) {
    oauth2Client.setCredentials(
        JSON.parse(await Deno.readTextFile(tokenPath)),
    );
} else {
    const authorizationUrl = oauth2Client.generateAuthUrl({
        access_type: "offline",
        scope: [
            "https://www.googleapis.com/auth/youtube",
            "https://www.googleapis.com/auth/youtube.upload",
        ],
    });

    open(authorizationUrl);

    const { tokens } = await oauth2Client.getToken(await oauthCode);
    await Deno.writeTextFile(
        tokenPath,
        JSON.stringify(tokens),
    );

    oauth2Client.setCredentials(tokens);

    await server.shutdown();
}

const youtube = google.youtube({ version: "v3", auth: oauth2Client });

console.log("Finding playlist...");
const { data: existingPlaylists } = await youtube.playlists.list({
    part: ["snippet"],
    mine: true,
});

let playlist = existingPlaylists.items?.find((item) =>
    item?.snippet?.title === PLAYLIST_TITLE
);

if (playlist) {
    console.log("Playlist found");
} else {
    console.log("Playlist not found, creating...");
    ({ data: playlist } = await youtube.playlists.insert({
        part: ["snippet", "status"],
        requestBody: {
            snippet: {
                title: PLAYLIST_TITLE,
                description: "Neon White speedruns",
            },
            status: {
                privacyStatus: "unlisted",
            },
        },
    }));
    console.log("Created new playlist with ID:", playlist.id);
}

const existingPlaylistItems: {
    id: string;
    title: string;
    levelName: string | undefined;
}[] = [];

let nextPageToken: string | undefined = undefined;
do {
    const { data: playlistItems } = await youtube.playlistItems.list({
        part: ["snippet"],
        playlistId: playlist.id,
        pageToken: nextPageToken,
    });

    for (const item of playlistItems.items ?? []) {
        existingPlaylistItems.push({
            id: item.id,
            title: item.snippet.title,
            levelName: item.snippet.title.match(/\] (.+?) - /)?.[1],
        });
    }

    nextPageToken = playlistItems.nextPageToken;
} while (nextPageToken);

console.log("Existing playlist levels:", existingPlaylistItems);

for await (const levelFolder of Deno.readDir(videoFolder)) {
    if (!levelFolder.isDirectory) continue;

    const levelName = levelFolder.name;
    const pbFolder = join(videoFolder, levelName, "PBs");
    if (!await exists(pbFolder)) continue;

    const levelIndex = LEVELS.indexOf(levelName);
    if (levelIndex === -1) {
        console.warn("Skipping unknown level:", levelName);
        continue;
    }

    let bestTime = Infinity;
    let bestTimestamp = "";
    let bestVideoPath = "";
    for await (const videoFile of Deno.readDir(pbFolder)) {
        const videoName = basename(videoFile.name, ".mkv");
        const [timestamp, timeString] = videoName.split(" - ");
        const time = parseFloat(timeString);

        if (time < bestTime) {
            bestTime = time;
            bestTimestamp = timestamp;
            bestVideoPath = join(pbFolder, videoFile.name);
        }
    }

    if (bestVideoPath) {
        const title = `[Neon White] ${levelName} - ${bestTime.toFixed(3)}`;
        const formattedTimestamp = `${bestTimestamp.slice(0, 4)}-${
            bestTimestamp.slice(4, 6)
        }-${bestTimestamp.slice(6, 8)} ${bestTimestamp.slice(8, 10)}:${
            bestTimestamp.slice(10, 12)
        }:${bestTimestamp.slice(12, 14)}`;
        const description = `Run completed on: ${
            new Date(formattedTimestamp).toLocaleString()
        }`;

        if (existingPlaylistItems.find((item) => item.title === title)) {
            console.log(`Video "${title}" already exists, skipping upload`);
            continue;
        }

        console.log(`Uploading: ${title}`);
        const { data: uploadedVideo } = await youtube.videos.insert({
            part: ["snippet", "status"],
            requestBody: {
                snippet: {
                    title: title,
                    description: description,
                },
                status: {
                    privacyStatus: "unlisted",
                },
            },
            media: {
                body: createReadStream(bestVideoPath),
            },
        });
        console.log(`Uploaded video with ID: ${uploadedVideo.id}`);

        let position = existingPlaylistItems.length;
        for (let i = levelIndex; i < LEVELS.length; i++) {
            const index = existingPlaylistItems.findIndex((x) =>
                x.levelName === LEVELS[i]
            );
            if (index !== -1) {
                position = index;
                if (i === levelIndex) {
                    console.log("Removing old video from playlist...");
                    await youtube.playlistItems.delete({
                        id: existingPlaylistItems[position].id,
                    });
                    existingPlaylistItems.splice(position, 1);
                }
                break;
            }
        }

        console.log("Adding video to playlist at position:", position);
        const { data: addedPlaylistItem } = await youtube.playlistItems.insert({
            part: ["snippet"],
            requestBody: {
                snippet: {
                    playlistId: playlist.id,
                    position,
                    resourceId: {
                        kind: "youtube#video",
                        videoId: uploadedVideo.id,
                    },
                },
            },
        });

        existingPlaylistItems.splice(position, 0, {
            id: addedPlaylistItem.id,
            title,
            levelName,
        });
    }
}
