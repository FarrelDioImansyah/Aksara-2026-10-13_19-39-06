using UnityEngine;

public class BillboardCanvas : MonoBehaviour
{
    [Header("Pengaturan Kamera")]
    [Tooltip("Kosongkan jika ingin otomatis mengambil Camera Utama")]
    public Camera mainCamera;

    [Header("Pengaturan Rotasi")]
    [Tooltip("Centang jika ingin rotasi hanya berputar di sumbu Y (tegak lurus)")]
    public bool lockYAxis = true;

    void Start()
    {
        FindClosestCamera();
    }

    void LateUpdate()
    {
        UpdateRotation();
    }

    // Mencari kamera aktif jika variabel mainCamera belum diisi di Inspector
    public void FindClosestCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    // Memutar Canvas agar selalu menghadap ke arah kamera
    public void UpdateRotation()
    {
        if (mainCamera == null) return;

        Vector3 targetPosition = transform.position + mainCamera.transform.rotation * Vector3.forward;
        Vector3 targetOrientation = mainCamera.transform.rotation * Vector3.up;

        transform.LookAt(targetPosition, targetOrientation);

        // Jika hanya ingin berputar secara horizontal (sumbu Y)
        if (lockYAxis)
        {
            transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        }
    }
}