using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("Kéo AudioSource dùng để phát âm thanh nổ đĩa vào đây")]
    public AudioSource explosionSource;

    [Header("Pitch Shift Settings")]
    [Tooltip("Độ tăng cao độ (Pitch) mỗi lần nổ liên tiếp")]
    public float pitchStep = 0.1f;
    [Tooltip("Giới hạn cao độ tối đa")]
    public float maxPitch = 2.0f;
    [Tooltip("Thời gian chờ để reset combo (nếu không có tiếng nổ nào trong thời gian này, pitch sẽ về 1.0)")]
    public float comboResetTime = 1.5f;

    private float currentPitch = 1.0f;
    private float lastExplosionTime = -1f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        // Kiểm tra xem đã hết thời gian combo chưa để reset Pitch về bình thường
        if (Time.time - lastExplosionTime > comboResetTime && currentPitch > 1.0f)
        {
            currentPitch = 1.0f;
        }
    }

    public void PlayExplosionSound()
    {
        if (explosionSource == null) return;

        // Cập nhật cao độ
        explosionSource.pitch = currentPitch;
        explosionSource.PlayOneShot(explosionSource.clip);

        // Tăng cao độ cho lần nổ tiếp theo (nếu nổ liên tục)
        currentPitch += pitchStep;
        if (currentPitch > maxPitch)
        {
            currentPitch = maxPitch;
        }

        // Ghi nhận thời điểm nổ để tính thời gian chờ reset combo
        lastExplosionTime = Time.time;
    }
}
