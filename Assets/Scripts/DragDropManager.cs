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
        // Chặn người chơi không cho kéo thả nếu FSM KHÔNG phải là PlayingState
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsState<PlayingState>())
        {
            return;
        }

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
            
            // Nếu đĩa này được kéo từ khay chờ (Lobby), giải phóng khay đó
            if (draggingPlate.currentHoldSlot != null)
            {
                draggingPlate.currentHoldSlot.ClearSlot();
                
                // Báo cho Lobby kiểm tra xem 3 khay đã trống hết chưa để bơm đĩa mới
                if (LobbyManager.Instance != null)
                {
                    LobbyManager.Instance.CheckAndRefill();
                }
            }

            // Chạy thuật toán kiểm tra 4 hướng
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

        // 1. Lấy danh sách TẤT CẢ các đĩa lân cận (không phân biệt màu)
        List<GridCell> neighbors = GridManager.Instance.GetAllNeighbors(placedCell.row, placedCell.column);
        
        if (neighbors.Count == 0) return;

        // 2. Tìm tất cả các màu đang tồn tại trên đĩa vừa đặt VÀ các đĩa lân cận
        HashSet<PizzaColor> colorsToCheck = new HashSet<PizzaColor>();
        foreach (var slice in placedPlate.slices)
            colorsToCheck.Add(slice.color);
        foreach (var neighbor in neighbors)
        {
            foreach (var slice in neighbor.currentPlate.slices)
                colorsToCheck.Add(slice.color);
        }

        // 3. Với mỗi màu, gom tất cả các lát cắt về cái đĩa đang chứa NHIỀU lát màu đó nhất
        foreach (PizzaColor color in colorsToCheck)
        {
            List<GridCell> cellsWithColor = new List<GridCell>();
            
            if (CountSlicesOfColor(placedPlate, color) > 0)
                cellsWithColor.Add(placedCell);
                
            foreach (var neighbor in neighbors)
            {
                if (CountSlicesOfColor(neighbor.currentPlate, color) > 0)
                    cellsWithColor.Add(neighbor);
            }

            // Nếu chỉ có 1 đĩa có màu này thì không có gì để gom
            if (cellsWithColor.Count <= 1) continue;

            // Tìm đĩa đích (đĩa chứa nhiều màu này nhất)
            GridCell targetMergeCell = cellsWithColor[0];
            int maxSlices = CountSlicesOfColor(targetMergeCell.currentPlate, color);

            for (int i = 1; i < cellsWithColor.Count; i++)
            {
                int count = CountSlicesOfColor(cellsWithColor[i].currentPlate, color);
                if (count > maxSlices)
                {
                    maxSlices = count;
                    targetMergeCell = cellsWithColor[i];
                }
            }

            // Tiến hành chuyển các lát cắt màu này từ các đĩa khác về đĩa đích
            PizzaPlate targetPlate = targetMergeCell.currentPlate;
            foreach (var cell in cellsWithColor)
            {
                if (cell == targetMergeCell) continue; // Bỏ qua đĩa đích

                PizzaPlate sourcePlate = cell.currentPlate;
                // Rút ngược từ trên xuống để tránh lỗi index khi Remove
                for (int i = sourcePlate.slices.Count - 1; i >= 0; i--)
                {
                    if (sourcePlate.slices[i].color == color)
                    {
                        if (!targetPlate.IsFull())
                        {
                            PizzaSlice sliceToMove = sourcePlate.slices[i];
                            targetPlate.AddSlice(sliceToMove);
                        }
                    }
                }
            }
            
            Debug.Log($"[Logic Core] Đã gộp các lát Pizza màu {color} về đĩa tại [{targetMergeCell.row}, {targetMergeCell.column}]");
        }
    }

    private int CountSlicesOfColor(PizzaPlate plate, PizzaColor color)
    {
        if (plate == null) return 0;
        
        int count = 0;
        foreach (var slice in plate.slices)
        {
            if (slice != null && slice.color == color) count++;
        }
        return count;
    }
}
