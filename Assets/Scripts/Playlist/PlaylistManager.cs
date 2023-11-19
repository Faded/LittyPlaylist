using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;
using YoutubePlayer;

public class PlaylistManager : MonoBehaviour
{
    // Replace this with your Youtube API Key
    private string apiKey = "YOUR_YOUTUBE_API_KEY";

    public PlaylistData playlistData;
    public Twitch twitch;
    public GameManager gameManager;
    public delegate void TitleCallback(string title);
    public TextMeshProUGUI titlePrefab;
    public Transform scrollViewContent;

    public VideoPlayer videoPlayer;
    private int currentVideoIndex = 0;

    public Button playButton;
    public Button skipButton;
    public Button pauseButton;

    private void Start()
    {
        // Button Listeners
        playButton.onClick.AddListener(PlayVideo);
        pauseButton.onClick.AddListener(PauseVideo);
        skipButton.onClick.AddListener(SkipVideo);

        // Load Playlist and Initialize the class
        Debug.Log("Loading Playlist...");
        playlistData = new PlaylistData();
        playlistData.Load();

        videoPlayer.loopPointReached += OnVideoLoopPointReached;
        LoadPlaylist();
    }

    /// <summary>
    /// Starts the playlist from the first video
    /// </summary>
    /// <returns></returns>
    public async void PlayVideo()
    {
        if (!videoPlayer.isPrepared)
        {
            if (playlistData.videoDataList.Count > 0)
            {
                VideoData currentVideo = playlistData.videoDataList[currentVideoIndex];
                string videoUrl = $"https://www.youtube.com/watch?v={currentVideo.VideoID}";

                if (!videoPlayer.isPrepared)
                    await PlayVideoAsync(videoUrl);

                videoPlayer.Play();
                StartCoroutine(FetchTitle(currentVideo.VideoID, title =>
                {
                    twitch.SendChatMessage(Utils.Format("Now Playing: {0}", title));
                }));

                currentVideoIndex++;
            }
            else
            {
                Debug.LogWarning("Playlist is empty.");
            }
        }
        videoPlayer.Play();
    }

    /// <summary>
    /// Skips to next video in the playlist
    /// </summary>
    /// <returns></returns>
    public void SkipVideo()
    {
        videoPlayer.Stop();

        if (currentVideoIndex >= playlistData.videoDataList.Count)
            currentVideoIndex = 0;

        PlayVideo();
    }

    /// <summary>
    /// Pauses & Unpauses the video, pressing Play while paused also works.
    /// </summary>
    /// <returns></returns>
    void PauseVideo()
    {
        if (videoPlayer.isPaused)
            videoPlayer.Play();
        else
            videoPlayer.Pause();
    }

    /// <summary>
    /// Refreshes the playlist after a new video is added
    /// </summary>
    /// <returns></returns>
    public void RefreshPlaylist()
    {
        foreach (Transform child in scrollViewContent)
            Destroy(child.gameObject);

        LoadPlaylist();
    }

    /// <summary>
    /// Reloads the playlist from the json save file
    /// </summary>
    /// <returns></returns>
    public void ReloadPlaylist()
    {
        playlistData.Load();
    }

    /// <summary>
    /// Plays a random video from the playlist
    /// </summary>
    /// <returns></returns>
    public void PlayRandomVideo()
    {
        videoPlayer.Stop();
        currentVideoIndex = Random.Range(0, playlistData.videoDataList.Count);
        PlayVideo();
    }

    /// <summary>
    /// Shuffles the playlist. (List reordered when reloaded from save)
    /// </summary>
    /// <returns></returns>
    public void ShufflePlaylist()
    {
        int n = playlistData.videoDataList.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            VideoData value = playlistData.videoDataList[k];
            playlistData.videoDataList[k] = playlistData.videoDataList[n];
            playlistData.videoDataList[n] = value;
        }
        RefreshPlaylist();
    }

    /// <summary>
    /// Restarts the video playlist if it gets hungup
    /// </summary>
    /// <returns></returns>
    public void RestartApplication()
    {
        twitch.SendChatMessage("Reloading Litty Player...");
        videoPlayer.Stop();
        ReloadPlaylist();
        RefreshPlaylist();
        PlayVideo();
    }

    private void OnVideoLoopPointReached(VideoPlayer source)
    {
        videoPlayer.Stop();

        if (currentVideoIndex >= playlistData.videoDataList.Count)
            currentVideoIndex = 0;

        PlayVideo();
    }

    /// <summary>
    /// Fetches the title from twitch command from the given youtube ID
    /// </summary>
    /// <param name="videoId"></param>
    /// <returns></returns>
    public IEnumerator FetchTitle(string videoId, TitleCallback callback)
    {
        string url = $"https://www.googleapis.com/youtube/v3/videos?id={videoId}&part=snippet&key={apiKey}";

        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + www.error);
            twitch.SendChatMessage(Utils.Format("That video id doesn't exist."));
        }
        else
        {
            string jsonContent = www.downloadHandler.text;
            string title = ParseTitleFromJson(jsonContent);
            if (title == "d")
                twitch.SendChatMessage(Utils.Format("That video id doesn't exist."));
            else
                callback(title);
        }
    }

    string ParseTitleFromJson(string jsonContent)
    {
        int titleStart = jsonContent.IndexOf("\"title\":") + 9;
        int titleEnd = jsonContent.IndexOf("\",", titleStart);

        if (titleStart >= 0 && titleEnd >= 0)
        {
            return jsonContent.Substring(titleStart, titleEnd - titleStart);
        }
        else
        {
            return "Title not found";
        }
    }

    private async Task PlayVideoAsync(string videoUrl)
    {
        Debug.Log("Loading video...");
        var videoPlayer = GetComponent<VideoPlayer>();
        await videoPlayer.PlayYoutubeVideoAsync(videoUrl);
    }

    void LoadPlaylist()
    {
        foreach (VideoData videoData in playlistData.videoDataList)
        {
            TextMeshProUGUI titleText = Instantiate(titlePrefab, scrollViewContent);
            titleText.text = videoData.VideoTitle;
        }
    }

    [System.Serializable]
    public class YouTubeVideoData
    {
        public Snippet snippet;
    }

    [System.Serializable]
    public class Snippet
    {
        public string title;
    }

    [System.Serializable]
    public class ResourceId
    {
        public string kind;
        public string videoId;
    }
}
