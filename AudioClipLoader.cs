using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using IPA.Utilities;
using UnityEngine;

namespace CrouchAlert;

internal class AudioClipLoader
{
    private static readonly string RESOURCE_PATH = Path.Combine(UnityGame.UserDataPath, "CrouchAlert");
    private static readonly string DEFAULT_AUDIO_CLIP_PATH = Path.Combine(RESOURCE_PATH, "default.wav");

    private Dictionary<string, AudioClip> _audioClips;
    private AudioClipAsyncLoader _audioClipAsyncLoader;

    public string[] AudioClipNames => _audioClips.Keys.ToArray();

    public AudioClipLoader(AudioClipAsyncLoader audioClipAsyncLoader)
    {
        _audioClipAsyncLoader = audioClipAsyncLoader;
        _audioClips = new Dictionary<string, AudioClip>();
        InitializeAudioClips();
    }

    private async void InitializeAudioClips()
    {
        await ExtractResources();
        await LoadAudioClips();
    }

    private async Task ExtractResources()
    {
        try
        {
            if (!Directory.Exists(RESOURCE_PATH))
            {
                Directory.CreateDirectory(RESOURCE_PATH);
            }

            if (!File.Exists(DEFAULT_AUDIO_CLIP_PATH))
            {
                Plugin.Log.Info("Default audio clip file not found. Creating default audio clip.");
                Stream defaultAudioClipStream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("CrouchAlert.Resources.default.wav");
                await defaultAudioClipStream.CopyToAsync(File.Create(DEFAULT_AUDIO_CLIP_PATH));
            }
        }
        catch (Exception e)
        {
            Plugin.Log.Error("Failed to extract resources.");
            Plugin.Log.Error(e);
        }
    }

    private async Task LoadAudioClips()
    {
        Plugin.Log.Info($"Loading audio clips from {RESOURCE_PATH}");
        string[] filePaths = Directory.GetFiles(RESOURCE_PATH).Where(path =>
        {
            string extension = Path.GetExtension(path);
            return extension == ".wav" || extension == ".ogg";
        }).ToArray();
        Plugin.Log.Info($"Found {filePaths.Length} wav/ogg files from {RESOURCE_PATH}");
        foreach (string filePath in filePaths)
        {
            try
            {
                AudioClip audioClip = await _audioClipAsyncLoader.Load(filePath);
                _audioClips.Add(Path.GetFileName(filePath), audioClip);
                Plugin.Log.Info($"Loaded {filePath}");
            }
            catch (Exception e)
            {
                Plugin.Log.Error($"Failed to load audio clip file: '{filePath}', skip to next");
                Plugin.Log.Error(e);
            }
        }
        Plugin.Log.Info($"Loaded {_audioClips.Count} audio clip(s)");
    }

    public AudioClip? GetAudioClipByName(string name)
    {
        if (!_audioClips.ContainsKey(name))
        {
            return null;
        }

        return _audioClips[name];
    }
}