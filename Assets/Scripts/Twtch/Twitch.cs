using UnityEngine;
using WebSocketSharp;
using System.Text.RegularExpressions;
using System.Collections;
using UnityEngine.Networking;
using System;
using System.Threading;
using System.Linq;

public class Twitch : MonoBehaviour
{
    private WebSocket twitchSocket;
    public bool IsSocketNull => twitchSocket == null;

    // Replace these with your twitch credentials
    private string channel = "YOUR_TWITCH_CHANNEL";
    private string token = "YOUR_CHANNEL_OR_BOT_TOKEN";
    private string clientid = "YOUR_CLIENT_ID";

    public delegate void TitleCallback(string title);

    private bool reconnecting = false;
    private bool doNotReconnnect = false;

    private MainThreadDispatcher mainThreadDispatcher;
    public PlaylistData playlistData;
    public PlaylistManager playlistManager;
    public GameManager gameManager;

    // Super Users that are able to use the commands (Twitch names only)
    private string[] supers = {"HypeFawx", "OfficialEaselm"};

    private void Awake()
    {
        mainThreadDispatcher = FindAnyObjectByType<MainThreadDispatcher>();
        if (mainThreadDispatcher == null)
        {
            Debug.LogError("MainThreadDispatcher not found in the scene.");
        }
    }

    /// <summary>
    /// Opens the websocket for Twitch and connects automatically
    /// </summary>
    /// <returns></returns>
    public void ConnectToTwitchChat()
    {
        string wsUri = "wss://irc-ws.chat.twitch.tv:443";
        twitchSocket = new WebSocket(wsUri);
        twitchSocket.OnOpen += (sender, e) => EnqueueOnMainThread(() => OnTwitchChatConnect());
        twitchSocket.OnMessage += (sender, e) => EnqueueOnMainThread(() => OnTwitchMessage(e.Data));
        twitchSocket.OnError += (sender, e) => EnqueueOnMainThread(() => OnTwitchError(e.Message));
        twitchSocket.OnClose += (sender, e) => EnqueueOnMainThread(() => OnTwitchClose());
        twitchSocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
        EnqueueOnMainThread(() => { twitchSocket.ConnectAsync(); });
    }

    private void OnTwitchChatConnect()
    {
        Debug.Log("Twitch chat socket Open.");
        JoinTwitchChannel();
    }

    private void JoinTwitchChannel()
    {
        twitchSocket.Send("CAP REQ :twitch.tv/tags twitch.tv/commands");
        twitchSocket.Send("PASS oauth:" + token);
        twitchSocket.Send("NICK " + channel);
        twitchSocket.Send("JOIN #" + channel);
    }

    private void OnTwitchMessage(string message)
    {
        EnqueueOnMainThread(() =>
        {
            // Extracts the twitch chat message pattern and returns specific values
            string userPattern = "display-name=(.*?);";
            string channelPattern = "PRIVMSG #(.*?):";
            string chatMessagePattern = "PRIVMSG #.*? :(.*?)$";
            string userIDPattern = "user-id=(.*?);";
            string moderatorPattern = "mod=(.*?);";
            string vipPattern = "vip=(.*?);";
            string subscriberPattern = "subscriber=(.*?);";

            string username = Regex.Match(message, userPattern).Groups[1].Value; // 0
            string channel = Regex.Match(message, channelPattern).Groups[1].Value; // 1
            string chatMessage = Regex.Match(message, chatMessagePattern).Groups[1].Value; // 2
            string userID = Regex.Match(message, userIDPattern).Groups[1].Value; // 3
            string moderator = Regex.Match(message, moderatorPattern).Groups[1].Value; // 4
            string vip = Regex.Match(message, vipPattern).Groups[1].Value; // 5
            string subscriber = Regex.Match(message, subscriberPattern).Groups[1].Value; // 6

            bool isSuper = supers.Contains(username);

            try
            {
                Debug.Log(message);
                if (message.Contains("PING :tmi.twitch.tv"))
                {
                    twitchSocket.Send("PONG :tmi.twitch.tv");
                    Debug.Log("Got PING, sent back PONG.");
                }
                else if (message.Contains("PRIVMSG"))
                {
                    // Twitch Commands
                    if (chatMessage.Contains("!addvid"))
                    {
                        if (isSuper)
                        {
                            string[] words = chatMessage.Split(' ');
                            if (words.Length >= 2)
                            {
                                string videoName = words[1];
                                string cleanedJson = videoName.Replace("\r", "");

                                StartCoroutine(FetchTitleAndHandleResult(cleanedJson, (title) =>
                                {
                                    Debug.Log("Received Title: " + title);

                                    SendChatMessage(Utils.Format("{0}, checking video...", username));
                                    if (gameManager.AddNewVideo(cleanedJson, title, username))
                                        SendChatMessage(Utils.Format("{0}, video {1} has been added. => {2}", username, cleanedJson, title));
                                    else
                                        SendChatMessage(Utils.Format("{0}, That video already exists in the list, or not at all.", username));
                                }));
                            }
                            else
                            {
                                SendChatMessage(Utils.Format("{0}, not enough arguments after !addvid.", username));
                            }
                        }
                    }
                    else if (chatMessage.Contains("!playtunes"))
                    {
                        if (isSuper)
                        {
                            if (playlistManager.videoPlayer.isPlaying)
                                SendChatMessage(Utils.Format("{0}, tunes already playing bruh!", username));
                            else
                            {
                                playlistManager.PlayVideo();
                                SendChatMessage(Utils.Format("We're playing tunes again o/"));
                            }
                        }
                    }
                    else if (chatMessage.Contains("!pausetunes"))
                    {
                        if (isSuper)
                        {
                            if (playlistManager.videoPlayer.isPaused)
                                SendChatMessage(Utils.Format("{0}, tunes already paused bruh!", username));
                            else
                            {
                                playlistManager.videoPlayer.Pause();
                                SendChatMessage(Utils.Format("{0} paused the tunes.. better be for a good reason...", username));
                            }
                        }
                    }
                    else if (chatMessage.Contains("!skip"))
                    {
                        if (isSuper)
                            playlistManager.SkipVideo();
                    }
                    else if (chatMessage.Contains("!randomtune"))
                    {
                        if (isSuper)
                            playlistManager.PlayRandomVideo();
                    }
                    else if (chatMessage.Contains("!shuffle"))
                    {
                        if (isSuper)
                            playlistManager.ShufflePlaylist();
                    }
                    else if (chatMessage.Contains("!tunesreboot"))
                    {
                        if (isSuper)
                            playlistManager.RestartApplication();
                    }
                    else if (chatMessage.Contains("!tuneshelp"))
                    {
                        if (isSuper)
                            SendChatMessage("Litty Playlist Help: !addvid <youtubeID>, !playtunes, !pausetunes, !skip, !randomtune, !shuffle, !tunesreboot");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("WebSocket Exception: " + ex.Message);
                Debug.LogException(ex);
            }
        });
    }

    IEnumerator FetchTitleAndHandleResult(string videoId, PlaylistManager.TitleCallback callback)
    {
        // Call FetchTitle from the PlaylistManager with the specified videoId
        yield return playlistManager.FetchTitle(videoId, (title) => { callback(title); });
    }

    public void AuthenticateWithTwitch()
    {
        string authUrl = "https://id.twitch.tv/oauth2/validate";
        UnityWebRequest request = UnityWebRequest.Get(authUrl);
        request.SetRequestHeader("Client-ID", clientid);
        request.SetRequestHeader("Authorization", "Bearer " + token);
        StartCoroutine(SendRequest(request));
    }

    private IEnumerator SendRequest(UnityWebRequest request)
    {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseFromServer = request.downloadHandler.text;
            Debug.Log(responseFromServer);
            ConnectToTwitchChat();
        }
        else
        {
            Debug.LogError("Error authenticating with Twitch: " + request.error);
        }
    }

    private void OnTwitchError(string errorMessage)
    {
        Debug.LogError("WebSocket error: " + errorMessage);
        if (!reconnecting)
        {
            reconnecting = true;
            EnqueueOnMainThread(() => { Reconnect(); });
        }
    }

    private void OnTwitchClose()
    {
        Debug.Log("WebSocket connection to Twitch chat server closed");
        if (!reconnecting)
        {
            if (!doNotReconnnect)
            {
                reconnecting = true;
                EnqueueOnMainThread(() => { Reconnect(); });
            }
        }
    }

    public void Reconnect()
    {
        Debug.Log("Sleeping for 5 seconds before reconnecting...");
        Thread.Sleep(5000);
        ConnectToTwitchChat();
        reconnecting = false;
    }

    public void DisconnectFromTwitchChat()
    {
        if (twitchSocket != null && twitchSocket.IsAlive)
        {
            doNotReconnnect = true;
            twitchSocket.Close();
        }
    }

    public void SendChatMessage(string message)
    {
        if (twitchSocket != null && twitchSocket.IsAlive)
        {
            string formattedMessage = $"PRIVMSG #{channel} :{message}";
            twitchSocket.Send(formattedMessage);
        }
        else
            Debug.LogWarning("Twitch socket is not alive. Message not sent.");
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Disconnecting...");
        DisconnectFromTwitchChat();
    }

    private void EnqueueOnMainThread(Action action)
    {
        mainThreadDispatcher.Enqueue(action);
    }
}