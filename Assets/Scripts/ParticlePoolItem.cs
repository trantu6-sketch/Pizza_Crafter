using UnityEngine;
using System.Collections;

public class ParticlePoolItem : MonoBehaviour
{
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
        // Đợi cho đến khi Particle chạy xong (hoặc đợi 1 thời gian cố định)
        yield return new WaitForSeconds(ps.main.duration + ps.main.startLifetime.constantMax);
        
        if (ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.ReturnToPool("ExplosionVFX", gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
