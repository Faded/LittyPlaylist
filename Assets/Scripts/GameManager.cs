using System;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    /* A thread dispatcher for twitch's websocket. This uses NO 3rd party twitch libs
     * It's needed to keep all coroutines in the same main thread */
    private MainThreadDispatcher mainThreadDispatcher;

    public Twitch twitch;
    public PlaylistManager playlistManager;

    // Playlist Objects for show/hide toggle
    public CanvasGroup playlistCanvas;
    public Button playlistToggle;

    private void Awake()
    {
        mainThreadDispatcher = FindAnyObjectByType<MainThreadDispatcher>();
        if (mainThreadDispatcher == null)
        {
            Debug.LogError("MainThreadDispatcher not found in the scene.");
        }
    }

    void Start()
    {
        EnqueueOnMainThread(() =>
        {
            playlistToggle.onClick.AddListener(TogglePlaylist);
            // We enqueue everything twitch related on the main thread, because of the websocket
            //twitch.AuthenticateWithTwitch();
        });
    }

    /// <summary>
    /// Toggles the Playlist's CanvasGroup
    /// </summary>
    /// <param></param>
    /// <returns></returns>
    void TogglePlaylist()
    {
        if (playlistCanvas != null)
        {
            if (playlistCanvas.interactable)
            {
                playlistCanvas.alpha = 0f;
                playlistCanvas.interactable = false;
                playlistCanvas.blocksRaycasts = false;
            }
            else
            {
                playlistCanvas.alpha = 1f;
                playlistCanvas.interactable = true;
                playlistCanvas.blocksRaycasts = true;
            }
        }
    }

    /// <summary>
    /// Adds the Youtube Video information to the playlist
    /// </summary>
    /// <param name="videoId"></param>
    /// <param name="videoTitle"></param>
    /// <param name="userName"></param>
    /// <returns></returns>
    public bool AddNewVideo(string videoId, string videoTitle, string userName)
    {
        if (IsPlayerAlreadyExists(videoId))
            return false;

        int newVideoID = playlistManager.playlistData.videoDataList.Count + 1;
        VideoData newVideo = new()
        {
            SlotID = newVideoID,
            VideoID = videoId,
            VideoTitle = videoTitle,
            AddedBy = userName
        };

        playlistManager.playlistData.videoDataList.Add(newVideo);
        playlistManager.playlistData.Save();
        playlistManager.ReloadPlaylist();
        playlistManager.RefreshPlaylist();
        return true;
    }

    private bool IsPlayerAlreadyExists(string videoId)
    {
        return playlistManager.playlistData.videoDataList.Exists(video => video.VideoID == videoId);
    }


    private void EnqueueOnMainThread(Action action)
    {
        mainThreadDispatcher.Enqueue(action);
    }
}
