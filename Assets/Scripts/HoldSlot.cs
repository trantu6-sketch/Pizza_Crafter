using UnityEngine;

public class HoldSlot : MonoBehaviour
{
    [Tooltip("Đĩa Pizza đang được đặt ở vị trí này. Null nếu ô trống.")]
    public PizzaPlate currentPlate;

    public bool IsEmpty => currentPlate == null;

    /// <summary>
    /// Gắn đĩa vào khay chứa, cập nhật vị trí
    /// </summary>
    public void SetPlate(PizzaPlate plate)
    {
        currentPlate = plate;
        plate.currentHoldSlot = this;
        // Đặt vị trí đĩa trùng với tâm khay chứa
        plate.transform.position = transform.position;
        // Lưu lại vị trí ban đầu để DragDropManager biết đường trả về nếu kéo lỗi
        plate.originalPosition = transform.position;
    }

    /// <summary>
    /// Xóa đĩa khỏi khay chứa (khi người chơi kéo thả thành công lên Grid)
    /// </summary>
    public void ClearSlot()
    {
        if (currentPlate != null)
        {
            currentPlate.currentHoldSlot = null;
            currentPlate = null;
        }
    }
}
