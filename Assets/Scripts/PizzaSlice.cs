using UnityEngine;
using System.Collections;

public class PizzaSlice : MonoBehaviour
{
    [Header("Slice Properties")]
    public PizzaColor color = PizzaColor.Red;
    
    [Header("Animation Settings")]
    [Tooltip("Thời gian bay từ đĩa này sang đĩa khác (giây)")]
    public float flightDuration = 0.5f;
    [Tooltip("Độ cao của đường vòng cung Bezier")]
    public float arcHeight = 2.0f;

    // Đĩa hiện tại đang chứa lát pizza này
    [HideInInspector]
    public PizzaPlate currentPlate;

    private bool isFlying = false;

    /// <summary>
    /// Hàm gọi để di chuyển lát Pizza bay sang một đĩa khác
    /// </summary>
    public void MoveToPlate(PizzaPlate targetPlate, Vector3 targetLocalPosition, Quaternion targetLocalRotation, System.Action onComplete = null)
    {
        if (isFlying) return;
        StartCoroutine(FlyBezierRoutine(targetPlate, targetLocalPosition, targetLocalRotation, onComplete));
    }

    private IEnumerator FlyBezierRoutine(PizzaPlate targetPlate, Vector3 targetLocalPosition, Quaternion targetLocalRotation, System.Action onComplete)
    {
        isFlying = true;
        
        // Báo cho FSM biết có 1 animation vừa bắt đầu
        if (GameStateManager.Instance != null)
        {
            if (GameStateManager.Instance.IsState<PlayingState>())
            {
                GameStateManager.Instance.ChangeState(GameStateManager.Instance.AnimatingState);
            }
            GameStateManager.Instance.AddActiveAnimation();
        }

        Vector3 startPos = transform.position;
        // Điểm đích tính theo World Space dựa trên vị trí Local mong muốn trên đĩa mới
        Vector3 endPos = targetPlate.transform.TransformPoint(targetLocalPosition);

        // Tính điểm điều khiển (Control Point P1) cho đường cong Bezier
        Vector3 midPoint = (startPos + endPos) / 2f;
        midPoint.y += arcHeight; // Đẩy lên cao để tạo vòng cung

        Quaternion startRot = transform.rotation;
        // Góc đích (World Space)
        Quaternion endRot = targetPlate.transform.rotation * targetLocalRotation;

        float timePassed = 0f;

        while (timePassed < flightDuration)
        {
            timePassed += Time.deltaTime;
            float t = timePassed / flightDuration;

            // Làm mượt t (tùy chọn, để bay mượt hơn ở điểm đầu và cuối)
            float smoothT = Mathf.SmoothStep(0, 1, t);

            // Công thức Quadratic Bezier: B(t) = (1-t)^2*P0 + 2*(1-t)*t*P1 + t^2*P2
            Vector3 m1 = Vector3.Lerp(startPos, midPoint, smoothT);
            Vector3 m2 = Vector3.Lerp(midPoint, endPos, smoothT);
            transform.position = Vector3.Lerp(m1, m2, smoothT);

            // Nội suy góc xoay (Slerp)
            transform.rotation = Quaternion.Slerp(startRot, endRot, smoothT);

            yield return null;
        }

        // Đảm bảo đến đúng vị trí
        transform.position = targetPlate.transform.TransformPoint(targetLocalPosition);
        transform.rotation = targetPlate.transform.rotation * targetLocalRotation;
        
        // Cập nhật quan hệ cha - con
        transform.SetParent(targetPlate.transform);
        currentPlate = targetPlate;

        isFlying = false;
        
        // Báo cho FSM biết animation này đã xong
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.RemoveActiveAnimation();
        }

        // Gọi callback khi hoàn thành
        onComplete?.Invoke();
    }
}
