using UnityEngine;
using DG.Tweening;

public class FloatingText : MonoBehaviour
{
    [Tooltip("Text Mesh Pro component")]
    public TMPro.TextMeshPro textMesh;

    [Tooltip("Tốc độ bay lên")]
    public float floatDuration = 1.0f;
    [Tooltip("Khoảng cách bay lên (Y)")]
    public float floatDistance = 2.0f;

    void OnEnable()
    {
        // Setup ban đầu
        // Setup ban đầu
        transform.localScale = Vector3.zero;

        if (textMesh != null)
        {
            Color c = textMesh.color;
            c.a = 1f;
            textMesh.color = c;
        }

        // Animation
        Sequence seq = DOTween.Sequence();
        
        // Hiện ra
        seq.Append(transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack));
        
        // Bay lên đồng thời mờ dần
        seq.Append(transform.DOMoveY(transform.position.y + floatDistance, floatDuration).SetEase(Ease.OutQuad));
        if (textMesh != null)
        {
            seq.Join(DOTween.To(() => textMesh.color, x => textMesh.color = x, new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, 0), floatDuration).SetEase(Ease.InQuad));
        }

        // Trả về Pool sau khi xong
        seq.OnComplete(() =>
        {
            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnToPool("FloatingText", gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        });
    }
}
