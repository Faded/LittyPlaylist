using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class PlaylistData
{
    // This class if for saving and loading the .sav
    public List<VideoData> videoDataList;
    private readonly string savePath;

    public PlaylistData()
    {
        savePath = Application.persistentDataPath + "/playlist.sav";
        videoDataList = new List<VideoData>();
    }

    public void Save()
    {
        string jsonData = JsonConvert.SerializeObject(this);
        File.WriteAllText(savePath, jsonData);
    }

    public void Load()
    {
        if (File.Exists(savePath))
        {
            string jsonData = File.ReadAllText(savePath);
            PlaylistData loadedData = JsonConvert.DeserializeObject<PlaylistData>(jsonData);
            this.videoDataList = loadedData.videoDataList;
        }
    }
}