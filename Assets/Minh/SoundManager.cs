using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        public float volume = 1f;
    }

    public List<Sound> sounds;
    private Dictionary<string, Sound> soundDict = new Dictionary<string, Sound>();

    private AudioSource sfxSource;
    private AudioSource musicSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);

        sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;

        foreach (var sound in sounds)
        {
            if (!soundDict.ContainsKey(sound.name))
                soundDict[sound.name] = sound;
        }
    }

    public void PlaySound(string name)
    {
        if (soundDict.TryGetValue(name, out Sound s))
        {
            sfxSource.PlayOneShot(s.clip, s.volume);
        }
        else
        {
            Debug.LogWarning("Sound not found: " + name);
        }
    }

    public void PlaySoundAt(string name, Vector3 position)
    {
        if (soundDict.TryGetValue(name, out Sound s))
        {
            AudioSource.PlayClipAtPoint(s.clip, position, s.volume);
        }
        else
        {
            Debug.LogWarning("Sound not found: " + name);
        }
    }

    public void PlayMusic(string name)
    {
        if (soundDict.TryGetValue(name, out Sound s))
        {
            musicSource.clip = s.clip;
            musicSource.volume = s.volume;
            musicSource.Play();
        }
        else
        {
            Debug.LogWarning("Music not found: " + name);
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }
}
