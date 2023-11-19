# Litty Youtube Player/Playlist

 This Unity project is a Youtube video player and playlist, controlled in Twitch chat. This project
 goes around all the obstacles of needing bloated 3rd party libraries.

The things you do need:
- A Youtube API Key. You can create one from the [Youtube Development Console](https://console.cloud.google.com/) under APIs & Services
- Twitch ClientID and Access Token. You can get those from [Twitch Token Generator](https://twitchtokengenerator.com/)

Replace Youtube API key in PlaylistManager.cs, and replace Twitch credentials in Twitch.cs
You can give twitch users access by adding the channel name to the Supers array on top of Twitch.cs

Twitch Commands:
- !addvid <youtubeID> - Adds the video data to a .sav for a local playlist ( Youtube ID only, eg: /watch?v={VIDEO_ID} )
- !playtunes - Starts the playlist from 0
- !pausetunes - Pauses the video & unpauses, same for !playtunes
- !skip - Skips to the next video in the local playlist
- !randomtune - Plays a random video from the local playlist
- !shuffle - Shuffles the local playlist in a random order
- !tunesreboot - Restarts the player incase of hang up

There is also a pause/play/skip button under the playlist, along with a toggle to hide the playlist.
Video shows on entire screen, default to 1280x720.

LittyPlaylist uses the iBicha Youtube Player package for playback https://github.com/iBicha/UnityYoutubePlayer

Support/Questions - https://www.littygames.net
