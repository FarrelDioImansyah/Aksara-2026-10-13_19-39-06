using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Configuration Asset")]
    public GameConfig config;

    [Header("Video References")]
    public VideoPlayer videoPlayer;
    public VideoClip loopVideo;          // loop mallpract
    public VideoClip transitionVideo;    // send transisi contoh
    public VideoClip giriVideo;          // JARAK GIRI   (id_kartu = 1)
    public VideoClip lengkaraVideo;      // JARAK LENGKARA (id_kartu = 2)
    public VideoClip srigatiVideo;       // JARAK SRIGATI  (id_kartu = 3)

    [Header("UI Nama & Bingkai (3D Object)")]
    public Transform bingkaiNama;        // Objek BingkaiNama (Empty Object)
    public TextMeshPro userNameText;     // Komponen TextMeshPro 3D
    public Transform startPoint;         // Empty object posisi bawah
    public Transform targetPoint;        // Empty object posisi tengah

    [Header("Audio References")]
    public AudioSource audioSource;

    [Header("Settings")]
    public float pollInterval = 3f;
    public float animationDuration = 1f;

    private string lastProcessedUserId = "";
    private bool isProcessingSequence = false;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // Sembunyikan bingkai di awal
        if (bingkaiNama != null) bingkaiNama.gameObject.SetActive(false);

        // Mulai Standby
        PlayLoopVideo();

        // Jalankan pengecekan Supabase terus menerus
        StartCoroutine(PollLatestUserData());
    }

    void PlayLoopVideo()
    {
        if (videoPlayer != null && loopVideo != null)
        {
            videoPlayer.clip = loopVideo;
            videoPlayer.isLooping = true;
            videoPlayer.Play();
        }

        if (audioSource != null)
        {
            AudioClip loopAudio = Resources.Load<AudioClip>("AudioNarasi/Loop");
            if (loopAudio != null)
            {
                audioSource.clip = loopAudio;
                audioSource.loop = true; // Audio latar belakang diatur looping
                audioSource.Play();
            }
            else
            {
                Debug.LogWarning("Audio 'Loop' tidak ditemukan di folder Resources/AudioNarasi/!");
            }
        }
    }

    void PlayNarration(int idKartu)
    {
        string audioPath = "AudioNarasi/card_" + idKartu;
        AudioClip clip = Resources.Load<AudioClip>(audioPath);
        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.loop = false; // Narasi kartu diputar sekali saja
            audioSource.Play();
        }
    }

    IEnumerator PollLatestUserData()
    {
        while (true)
        {
            if (!isProcessingSequence)
            {
                yield return StartCoroutine(FetchLatestUserFromSupabase());
            }
            yield return new WaitForSeconds(pollInterval);
        }
    }

    IEnumerator FetchLatestUserFromSupabase()
    {
        string queryUrl = config.supabaseRestUrl + "?select=*&order=created_at.desc&limit=1";

        using (UnityWebRequest request = UnityWebRequest.Get(queryUrl))
        {
            request.SetRequestHeader("apikey", config.supabaseApiKey);
            request.SetRequestHeader("Authorization", "Bearer " + config.supabaseApiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ParseUserData(request.downloadHandler.text);
            }
        }
    }

    void ParseUserData(string json)
    {
        string wrappedJson = "{\"users\":" + json + "}";
        UserDataList dataList = JsonUtility.FromJson<UserDataList>(wrappedJson);

        if (dataList != null && dataList.users != null && dataList.users.Length > 0)
        {
            UserData latestUser = dataList.users[0];

            if (latestUser.id != lastProcessedUserId && !isProcessingSequence)
            {
                lastProcessedUserId = latestUser.id;
                StartCoroutine(ExecutePhotoboothSequence(latestUser));
            }
        }
    }

    IEnumerator ExecutePhotoboothSequence(UserData user)
    {
        isProcessingSequence = true;

        // ==========================================
        // FASE 2: TRANSISI, AUDIO TRANSISI, & ANIMASI NAMA NAIK
        // ==========================================
        videoPlayer.isLooping = false;
        videoPlayer.clip = transitionVideo;
        videoPlayer.Play();

        string transitionAudioPath = "AudioNarasi/Send Doa 13 sec";
        AudioClip transAudio = Resources.Load<AudioClip>(transitionAudioPath);
        if (transAudio != null)
        {
            audioSource.clip = transAudio;
            audioSource.loop = false;
            audioSource.Play();
        }

        if (bingkaiNama != null && userNameText != null)
        {
            userNameText.text = user.nama_latin;
            bingkaiNama.gameObject.SetActive(true);
            StartCoroutine(AnimateBingkaiMoveUp(animationDuration));
        }

        yield return null;
        yield return new WaitUntil(() => videoPlayer.isPlaying);
        yield return new WaitUntil(() => !videoPlayer.isPlaying);

        // ==========================================
        // FASE 3: VIDEO HASIL & AUDIO NARASI KARTU
        // ==========================================
        videoPlayer.clip = GetVideoClipByCardId(user.id_kartu);
        videoPlayer.isLooping = false;
        videoPlayer.Play();

        PlayNarration(user.id_kartu);

        yield return new WaitUntil(() => !videoPlayer.isPlaying && !audioSource.isPlaying);

        // ==========================================
        // FASE 4: RESET
        // ==========================================
        if (bingkaiNama != null) bingkaiNama.gameObject.SetActive(false);
        PlayLoopVideo();
        isProcessingSequence = false;
    }

    IEnumerator AnimateBingkaiMoveUp(float duration)
    {
        float elapsedTime = 0f;
        Vector3 startPos = startPoint.position;
        Vector3 targetPos = targetPoint.position;

        bingkaiNama.position = startPos;

        while (elapsedTime < duration)
        {
            bingkaiNama.position = Vector3.Lerp(startPos, targetPos, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        bingkaiNama.position = targetPos;
    }

    VideoClip GetVideoClipByCardId(int idKartu)
    {
        switch (idKartu)
        {
            case 1: return giriVideo;
            case 2: return lengkaraVideo;
            case 3: return srigatiVideo;
            default: return giriVideo;
        }
    }
}

[System.Serializable]
public class UserData { public string id, created_at, nama_latin, nama_panggilan, url_kartu; public int id_kartu; }
[System.Serializable]
public class UserDataList { public UserData[] users; }