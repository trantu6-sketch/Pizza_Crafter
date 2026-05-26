using UnityEngine;
using System.Collections.Generic;

public class DragDropManager : MonoBehaviour
{
    private PizzaPlate draggingPlate;
    private Vector3 dragOffset;
    
    // Mặt phẳng ảo dùng để Raycast khi đang kéo chậu hoa (tại độ cao Y nhất định)
    private Plane dragPlane;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseDown();
        }
        else if (Input.GetMouseButton(0) && draggingPlate != null)
        {
            HandleMouseDrag();
        }
        else if (Input.GetMouseButtonUp(0) && draggingPlate != null)
        {
            HandleMouseUp();
        }
    }

    private void HandleMouseDown()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            // Kiểm tra xem đối tượng có chứa component PizzaPlate không
            PizzaPlate plate = hit.collider.GetComponentInParent<PizzaPlate>();
            if (plate != null)
            {
                draggingPlate = plate;
                
                // Nếu plate đang nằm trên lưới, gỡ nó ra khỏi ô lưới hiện tại
                if (draggingPlate.currentCell != null)
                {
                    draggingPlate.currentCell.currentPlate = null;
                    draggingPlate.currentCell = null;
                }

                // Tạo một mặt phẳng nằm ngang ở độ cao Y của đĩa + một chút khoảng cách để nhìn có vẻ nhấc lên
                float dragHeight = draggingPlate.transform.position.y + 0.5f;
                dragPlane = new Plane(Vector3.up, new Vector3(0, dragHeight, 0));

                // Tính toán offset giữa điểm click chuột và tâm của đĩa pizza
                if (dragPlane.Raycast(ray, out float enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    dragOffset = draggingPlate.transform.position - hitPoint;
                }
            }
        }
    }

    private void HandleMouseDrag()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            draggingPlate.transform.position = hitPoint + dragOffset;
        }
    }

    private void HandleMouseUp()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        GridCell targetCell = null;

        // Bắn tia ray tìm xem con trỏ chuột có đang thả ở trên ô GridCell nào không
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        foreach (var hit in hits)
        {
            GridCell cell = hit.collider.GetComponent<GridCell>();
            if (cell != null)
            {
                targetCell = cell;
                break;
            }
        }

        if (targetCell != null && targetCell.IsEmpty)
        {
            // Nếu có ô trống, thực hiện bắt dính (Snapping)
            targetCell.SetPlate(draggingPlate);
            
            // Chạy thuật toán kiểm tra 4 hướng và In ra Console
            CheckNeighbors(targetCell);
        }
        else
        {
            // Nếu không tìm thấy ô, hoặc ô đã bị chiếm, trả về vị trí cũ ở khay chứa
            draggingPlate.ReturnToOriginalPosition();
        }

        draggingPlate = null;
    }

    private void CheckNeighbors(GridCell placedCell)
    {
        PizzaPlate placedPlate = placedCell.currentPlate;
        if (placedPlate == null || placedPlate.IsEmpty()) return;

        PizzaColor targetColor = placedPlate.GetTopColor();
        
        // 1. Lấy danh sách các đĩa lân cận có cùng màu (và đĩa vừa đặt)
        List<GridCell> matchingCells = GridManager.Instance.GetMatchingNeighbors(placedCell.row, placedCell.column, targetColor);
        
        // Nếu không có đĩa lân cận nào trùng màu, kết thúc
        if (matchingCells.Count == 0)
        {
            Debug.Log("[Logic Core] Không có đĩa pizza nào liền kề trùng màu.");
            return;
        }

        // Thêm chính đĩa vừa đặt vào danh sách để so sánh
        matchingCells.Add(placedCell);

        // 2. Tìm đĩa có chứa nhiều lát pizza màu targetColor nhất
        GridCell targetMergeCell = matchingCells[0];
        int maxSlices = 0;

        foreach (var cell in matchingCells)
        {
            int countColor = 0;
            // Đếm xem đĩa này có bao nhiêu lát cùng màu targetColor (tính từ trên xuống)
            for (int i = cell.currentPlate.slices.Count - 1; i >= 0; i--)
            {
                if (cell.currentPlate.slices[i].color == targetColor)
                    countColor++;
                else
                    break;
            }

            if (countColor > maxSlices)
            {
                maxSlices = countColor;
                targetMergeCell = cell;
            }
        }

        // 3. Thực hiện chuyển (bay) các lát cắt từ các đĩa kia sang đĩa nhiều nhất
        PizzaPlate targetPlate = targetMergeCell.currentPlate;

        foreach (var cell in matchingCells)
        {
            if (cell == targetMergeCell) continue; // Bỏ qua đĩa mục tiêu

            PizzaPlate sourcePlate = cell.currentPlate;

            // Rút dần các lát cắt từ trên cùng ra
            for (int i = sourcePlate.slices.Count - 1; i >= 0; i--)
            {
                if (sourcePlate.slices[i].color == targetColor && !targetPlate.IsFull())
                {
                    PizzaSlice sliceToMove = sourcePlate.slices[i];
                    // AddSlice sẽ lo logic rút khỏi đĩa cũ và bay sang đĩa mới
                    targetPlate.AddSlice(sliceToMove);
                }
                else
                {
                    break; // Đã hết lát màu này, hoặc đĩa đích đã đầy
                }
            }
        }

        Debug.Log($"[Logic Core] Đã gộp các lát Pizza về đĩa tại [{targetMergeCell.row}, {targetMergeCell.column}]");
    }
}
