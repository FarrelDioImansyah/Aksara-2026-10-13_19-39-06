using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Configuration Asset")]
    public GameConfig config;

    [Header("Card Prefab Reference")]
    public GameObject cardPrefab; // Masukkan Prefab Card_petangen ke sini
    public Transform spawnPoint;  // Tempat posisi kemunculan kartu (opsional)

    [Header("Audio References")]
    public AudioSource audioSource;
    public AudioClip[] narrationClips;

    [Header("Settings")]
    public float pollInterval = 3f;
    private string lastProcessedUserId = "";

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (config == null)
        {
            Debug.LogWarning("GameConfig belum dimasukkan ke GameManager Inspector!");
        }

        StartCoroutine(PollLatestUserData());
    }

    IEnumerator PollLatestUserData()
    {
        while (true)
        {
            yield return StartCoroutine(FetchLatestUserFromSupabase());
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
                string jsonResult = request.downloadHandler.text;
                ParseUserData(jsonResult);
            }
            else
            {
                Debug.LogError("Gagal mengambil data dari Supabase: " + request.error);
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

            if (latestUser.id != lastProcessedUserId)
            {
                lastProcessedUserId = latestUser.id;
                Debug.Log("Data baru ditemukan! Nama: " + latestUser.nama_latin + " | ID Kartu: " + latestUser.id_kartu);

                // 1. Spawn prefab kartu dan download gambarnya secara otomatis
                SpawnAndSetupCard(latestUser);

                // 2. Putar narasi audio berdasarkan id_kartu
                PlayNarration(latestUser.id_kartu);
            }
        }
    }

    void SpawnAndSetupCard(UserData user)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("Card Prefab belum dimasukkan ke GameManager Inspector!");
            return;
        }

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;

        // Membalikkan rotasi menghadap kamera atau membalikkan sumbu jika terbalik
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject newCard = Instantiate(cardPrefab, spawnPosition, spawnRotation);

        // --- SOLUSI JIKA TERBALIK SECARA VERTIKAL ---
        // Membalikkan skala Y prefab jika gambarnya terbalik atas-bawah
        Vector3 currentScale = newCard.transform.localScale;
        newCard.transform.localScale = new Vector3(currentScale.x, -currentScale.y, currentScale.z);
        // -------------------------------------------

        RawImage targetRawImage = newCard.GetComponentInChildren<RawImage>();

        if (targetRawImage != null && !string.IsNullOrEmpty(user.url_kartu))
        {
            StartCoroutine(LoadCardImageFromUrl(user.url_kartu, targetRawImage));
        }
    }

    IEnumerator LoadCardImageFromUrl(string imageUrl, RawImage targetImage)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
                targetImage.texture = tex;
                Debug.Log("Gambar kartu berhasil dimuat ke prefab yang di-generate!");
            }
            else
            {
                Debug.LogError("Gagal mendownload gambar kartu: " + uwr.error);
            }
        }
    }

    void PlayNarration(int idKartu)
    {
        string audioPath = "AudioNarasi/card_" + idKartu;
        AudioClip clipToPlay = Resources.Load<AudioClip>(audioPath);

        if (clipToPlay != null)
        {
            audioSource.clip = clipToPlay;
            audioSource.Play();
            Debug.Log("Berhasil memuat dan memutar narasi otomatis untuk ID Kartu: " + idKartu);
        }
        else
        {
            Debug.LogWarning("File audio dengan path 'Resources/" + audioPath + "' tidak ditemukan!");
        }
    }
}

[System.Serializable]
public class UserData
{
    public string id;
    public string created_at;
    public string nama_latin;
    public string nama_panggilan;
    public int id_kartu;
    public string url_kartu;
}

[System.Serializable]
public class UserDataList
{
    public UserData[] users;
}