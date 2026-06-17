using UnityEngine;

public class AnchorToScreen3D : MonoBehaviour
{
    public enum ScreenAnchor { BottomCenter, TopCenter }
    public ScreenAnchor anchor = ScreenAnchor.BottomCenter;
    
    [Tooltip("Khoảng cách thụt vào so với mép màn hình. Tăng trục Z lên nếu mâm gỗ bị lẹm xuống dưới.")]
    public Vector3 offset = new Vector3(0, 0, 2f);

    void Start()
    {
        AnchorIt();
    }

    void AnchorIt()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Xác định mép dưới (0) hoặc mép trên (1) của màn hình
        Vector2 viewportPoint = anchor == ScreenAnchor.BottomCenter ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 1f);
        
        // Bắn tia từ Camera xuống mặt bàn Game (nơi Y = 0)
        Ray ray = cam.ViewportPointToRay(viewportPoint);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPos = ray.GetPoint(distance);
            
            // Cộng/Trừ khoảng cách an toàn (offset)
            if (anchor == ScreenAnchor.BottomCenter)
            {
                worldPos += offset;
            }
            else
            {
                worldPos -= offset;
            }
            
            // Giữ nguyên độ cao (Y) ban đầu của Lobby để không bị lún xuống bàn
            worldPos.y = transform.position.y;
            
            transform.position = worldPos;
        }
    }
}
