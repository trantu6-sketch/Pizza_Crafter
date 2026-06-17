using UnityEngine;
using System.Collections;

public class ParticlePoolItem : MonoBehaviour
{
    [Tooltip("Tên Pool Tag để trả về. Ví dụ: ExplosionVFX")]
    public string poolTag = "ExplosionVFX";

    [Tooltip("Thời gian chờ trước khi tự động cất Effect đi (Giây)")]
    public float autoDisableTime = 2f;

    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void OnEnable()
    {
        if (ps != null)
        {
            ps.Play();
            StartCoroutine(WaitAndReturn());
        }
    }

    private IEnumerator WaitAndReturn()
    {
        // Chờ đúng thời gian ép buộc (mặc định 2 giây)
        yield return new WaitForSeconds(autoDisableTime);
        
        if (ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
