using UnityEngine;
using DG.Tweening;

public class GridCell : MonoBehaviour
{
    public int row;
    public int column;
    
    // Đĩa pizza đang nằm trên ô này
    public PizzaPlate currentPlate;

    // Kiểm tra xem ô lưới này có trống hay không
    public bool IsEmpty => currentPlate == null;

    private float originalY;

    void Start()
    {
        originalY = transform.position.y;
    }

    public void HoverEffect()
    {
        // Nổi ô cờ lên 0.2f
        transform.DOMoveY(originalY + 0.2f, 0.1f).SetEase(DG.Tweening.Ease.OutQuad);
    }

    public void ResetHoverEffect()
    {
        // Hạ ô cờ xuống vị trí cũ
        transform.DOMoveY(originalY, 0.15f).SetEase(DG.Tweening.Ease.InQuad);
    }

    /// <summary>
    /// Gán đĩa pizza vào ô lưới này
    /// </summary>
    public void SetPlate(PizzaPlate plate)
    {
        currentPlate = plate;
        if (plate != null)
        {
            plate.currentCell = this;
            
            // Tính toán độ cao (Y) để đĩa nằm trên mặt lưới thay vì bị lún xuống giữa
            float yOffset = 0f;
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                // Lấy khoảng cách từ tâm đến đỉnh của BoxCollider
                yOffset = col.bounds.extents.y;
            }
            else
            {
                // Mặc định nâng lên Y = 1 nếu không có Collider để tính toán
                yOffset = 1f;
            }

            // Snap (Hút) vị trí của chậu/đĩa lên trên bề mặt ô lưới
            plate.transform.position = transform.position + new Vector3(0, yOffset, 0);
        }
    }
}
