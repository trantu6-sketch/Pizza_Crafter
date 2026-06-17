using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAutoFit : MonoBehaviour
{
    [Tooltip("Tỷ lệ khung hình gốc bạn dùng lúc test ở Unity Editor (Ví dụ: 1080 x 1920 thì tỷ lệ là 9/16)")]
    public Vector2 targetResolution = new Vector2(1080, 1920);

    private Camera cam;
    private float initialFovOrSize;

    void Awake()
    {
        cam = GetComponent<Camera>();
        
        // Lưu lại thông số chuẩn lúc bạn thiết kế
        if (cam.orthographic)
            initialFovOrSize = cam.orthographicSize;
        else
            initialFovOrSize = cam.fieldOfView;

        FitScreen();
    }

    void FitScreen()
    {
        // Tỷ lệ chuẩn (Ví dụ 9/16 = 0.5625)
        float targetAspect = targetResolution.x / targetResolution.y;
        
        // Tỷ lệ thực tế của thiết bị người chơi (Ví dụ màn hình dài 9/20 = 0.45)
        float currentAspect = (float)Screen.width / (float)Screen.height;
        
        // So sánh
        float scaleHeight = currentAspect / targetAspect;

        // Nếu tỷ lệ thiết bị HẸP ngang hơn tỷ lệ chuẩn (scaleHeight < 1)
        if (scaleHeight < 1.0f)
        {
            // Bắt buộc Camera phải lùi ra xa (tăng Size/FOV) để bù đắp chiều ngang bị mất
            if (cam.orthographic)
            {
                cam.orthographicSize = initialFovOrSize / scaleHeight;
            }
            else
            {
                // Công thức quy đổi góc nhìn (FOV) cho Camera 3D Perspective
                float radHFOV = 2 * Mathf.Atan(Mathf.Tan(initialFovOrSize * Mathf.Deg2Rad / 2) * targetAspect);
                cam.fieldOfView = 2 * Mathf.Atan(Mathf.Tan(radHFOV / 2) / currentAspect) * Mathf.Rad2Deg;
            }
        }
        else
        {
            // Thiết bị RỘNG ngang hơn tỷ lệ chuẩn (Ví dụ iPad, WebGL) -> Giữ nguyên chiều dọc, để chiều ngang tự dưng dư ra 2 bên
            if (cam.orthographic)
                cam.orthographicSize = initialFovOrSize;
            else
                cam.fieldOfView = initialFovOrSize;
        }
    }
}
