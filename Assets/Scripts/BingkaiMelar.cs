using UnityEngine;
using TMPro;

[ExecuteAlways] // Agar efek melar bisa langsung terlihat di dalam scene Unity (meski belum di-play)
public class BingkaiMelar : MonoBehaviour
{
    public SpriteRenderer bingkai;
    public TextMeshPro teksNama; // Menggunakan versi 3D

    [Header("Pengaturan Ukuran")]
    public float lebarBingkai = 5f;
    public float paddingAtasBawah = 1f; // Jarak ekstra agar teks tidak menabrak batas atas/bawah bingkai

    void Update()
    {
        if (bingkai != null && teksNama != null)
        {
            // Membaca tinggi teks murni secara real-time
            float tinggiTeks = teksNama.preferredHeight;

            // Memanjangkan ukuran bingkai ke bawah (lebar tetap, tinggi berubah)
            bingkai.size = new Vector2(lebarBingkai, tinggiTeks + paddingAtasBawah);
        }
    }
}