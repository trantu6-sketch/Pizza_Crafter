using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class DragDropManager : MonoBehaviour
{
    private PizzaPlate draggingPlate;
    private Vector3 dragOffset;
    
    // Mặt phẳng ảo dùng để Raycast khi đang kéo chậu hoa (tại độ cao Y nhất định)
    private Plane dragPlane;
    
    // Lưu ô cờ đang được hover để làm hiệu ứng nổi
    private GridCell hoveredCell;

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
                // KHÓA CHIẾN THUẬT: Chặn không cho kéo đĩa đã được đặt lên bàn (Grid)
                if (plate.currentCell != null)
                {
                    Debug.Log("[Logic Core] Không thể nhấc đĩa đã đặt trên bàn chơi! Tính chiến thuật được kích hoạt.");
                    return; // Kết thúc hàm, không cho phép kéo
                }

                draggingPlate = plate;

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

        // --- Bắt đầu Logic Hover (Nổi ô cờ) ---
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        GridCell currentHover = null;
        
        foreach (var hit in hits)
        {
            GridCell cell = hit.collider.GetComponent<GridCell>();
            if (cell != null)
            {
                currentHover = cell;
                break;
            }
        }

        // Nếu người chơi lia chuột sang ô khác (hoặc lia ra ngoài)
        if (currentHover != hoveredCell)
        {
            // Hạ ô cũ xuống
            if (hoveredCell != null)
            {
                hoveredCell.ResetHoverEffect();
            }

            hoveredCell = currentHover;

            // Nâng ô mới lên (Chỉ nổi lên nếu ô đó trống, tránh nổi ô đã có đĩa)
            if (hoveredCell != null && hoveredCell.IsEmpty)
            {
                hoveredCell.HoverEffect();
            }
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
            // Hiệu ứng Squash & Stretch khi đặt thành công (Làm lố lên để dễ nhìn)
            PizzaPlate placedPlate = draggingPlate;
            
            // Dùng Sequence để tạo chuỗi hiệu ứng: Bẹp xuống -> Dãn lên -> Trở về bình thường
            DG.Tweening.Sequence seq = DG.Tweening.DOTween.Sequence();
            // 1. Ép bẹp (Squash) thật mạnh: Rộng ra (1.3) và Lùn đi (0.5)
            seq.Append(placedPlate.transform.DOScale(new Vector3(1.3f, 0.5f, 1.3f) * 0.8f, 0.08f));
            // 2. Kéo dãn (Stretch) nảy lên: Ốm lại (0.85) và Cao lên (1.2)
            seq.Append(placedPlate.transform.DOScale(new Vector3(0.85f, 1.2f, 0.85f) * 0.8f, 0.1f));
            // 3. Phục hồi về kích thước gốc 0.8
            seq.Append(placedPlate.transform.DOScale(Vector3.one * 0.8f, 0.15f).SetEase(DG.Tweening.Ease.OutQuad));

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
            
            // KÍCH HOẠT CHECK GAME OVER (Nếu không có animation nào chạy)
            if (GameStateManager.Instance != null && GameStateManager.Instance.IsState<PlayingState>())
            {
                GameStateManager.Instance.ChangeState(GameStateManager.Instance.CheckingState);
            }
        }
        else
        {
            // Hiệu ứng Rung lắc (Shake) báo hiệu đặt sai
            draggingPlate.transform.DOShakePosition(0.3f, new Vector3(0.2f, 0, 0.2f), 20, 90, false, true);
            
            // Nếu không tìm thấy ô, hoặc ô đã bị chiếm, trả về vị trí cũ ở khay chứa
            draggingPlate.ReturnToOriginalPosition();
        }

        // Hạ ô cờ đang nổi xuống (Dù đặt thành công hay thất bại)
        if (hoveredCell != null)
        {
            hoveredCell.ResetHoverEffect();
            hoveredCell = null;
        }

        draggingPlate = null;
    }

    private void CheckNeighbors(GridCell placedCell)
    {
        if (placedCell.currentPlate == null || placedCell.currentPlate.IsEmpty()) return;

        Queue<GridCell> activeQueue = new Queue<GridCell>();
        activeQueue.Enqueue(placedCell);

        int maxIterations = 100; // Tránh lặp vô tận do lỗi logic
        int iterations = 0;

        while (activeQueue.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            GridCell currentCell = activeQueue.Dequeue();
            PizzaPlate currentPlate = currentCell.currentPlate;

            if (currentPlate == null || currentPlate.IsEmpty()) continue;

            // Lấy các màu hiện có trên đĩa này
            HashSet<PizzaColor> colorsOnPlate = new HashSet<PizzaColor>();
            foreach (var slice in currentPlate.slices)
            {
                colorsOnPlate.Add(slice.color);
            }

            foreach (PizzaColor color in colorsOnPlate)
            {
                // Kiểm tra lại vì có thể đĩa đã bị full hoặc mất màu này trong quá trình lặp
                if (currentPlate == null || currentPlate.IsEmpty()) break;
                int currentCount = CountSlicesOfColor(currentPlate, color);
                if (currentCount == 0 || currentCount >= currentPlate.maxSlices) continue;

                // Tìm các lân cận CÓ MÀU NÀY
                List<GridCell> neighbors = GridManager.Instance.GetAllNeighbors(currentCell.row, currentCell.column);
                List<GridCell> validNeighbors = new List<GridCell>();
                
                foreach (var neighbor in neighbors)
                {
                    if (neighbor.currentPlate != null)
                    {
                        int c = CountSlicesOfColor(neighbor.currentPlate, color);
                        if (c > 0)
                        {
                            validNeighbors.Add(neighbor);
                        }
                    }
                }

                if (validNeighbors.Count == 0) continue;

                // TÌM ĐĨA ĐÍCH (Target) theo nguyên tắc chống Ping-Pong và chống Kẹt
                List<GridCell> group = new List<GridCell>();
                group.Add(currentCell);
                group.AddRange(validNeighbors);

                // Sort giảm dần theo số lượng lát màu này. Ưu tiên currentCell nếu bằng nhau.
                group.Sort((a, b) => 
                {
                    int cA = CountSlicesOfColor(a.currentPlate, color);
                    int cB = CountSlicesOfColor(b.currentPlate, color);
                    if (cA != cB) return cB.CompareTo(cA);
                    if (a == currentCell) return -1;
                    if (b == currentCell) return 1;
                    return 0;
                });

                GridCell targetMergeCell = null;
                foreach (var potentialTarget in group)
                {
                    // Nếu đĩa Đích bị Full 6 miếng, không thể nhét thêm được nữa! Bỏ qua.
                    if (potentialTarget.currentPlate.IsFull()) continue;

                    bool canMergeAnything = false;
                    foreach (var source in group)
                    {
                        if (source == potentialTarget) continue;
                        
                        int sCount = CountSlicesOfColor(source.currentPlate, color);
                        int tEmpty = potentialTarget.currentPlate.maxSlices - potentialTarget.currentPlate.slices.Count;
                        int tCount = CountSlicesOfColor(potentialTarget.currentPlate, color);

                        // ĐIỀU KIỆN MẤU CHỐT: Chỉ truyền Pizza nếu Đĩa đích đủ chỗ nhận TOÀN BỘ số Pizza của Đĩa nguồn,
                        // HOẶC nếu việc nhận này giúp Đĩa đích Đủ 6 miếng (Nổ luôn).
                        if (tEmpty >= sCount || (tCount + sCount) >= potentialTarget.currentPlate.maxSlices)
                        {
                            targetMergeCell = potentialTarget;
                            canMergeAnything = true;
                            break; // Tìm thấy 1 sự kết hợp hợp lệ là đủ để chốt Target
                        }
                    }

                    if (canMergeAnything) break;
                }

                if (targetMergeCell == null) continue;

                bool moved = false;

                // TH1: Target chính là currentCell (Nó sẽ Hút toàn bộ Pizza từ xung quanh)
                if (targetMergeCell == currentCell)
                {
                    foreach (var neighbor in validNeighbors)
                    {
                        PizzaPlate sourcePlate = neighbor.currentPlate;
                        int sCount = CountSlicesOfColor(sourcePlate, color);
                        int tEmpty = targetMergeCell.currentPlate.maxSlices - targetMergeCell.currentPlate.slices.Count;
                        int tCount = CountSlicesOfColor(targetMergeCell.currentPlate, color);
                        
                        // Phải thỏa mãn điều kiện chống Ping-Pong
                        if (tEmpty >= sCount || (tCount + sCount) >= targetMergeCell.currentPlate.maxSlices)
                        {
                            for (int i = sourcePlate.slices.Count - 1; i >= 0; i--)
                            {
                                if (sourcePlate.slices[i].color == color && !targetMergeCell.currentPlate.IsFull())
                                {
                                    if (targetMergeCell.currentPlate.AddSlice(sourcePlate.slices[i])) moved = true;
                                }
                            }
                        }
                    }
                }
                // TH2: Target là 1 đĩa xung quanh (currentCell sẽ Bơm Pizza ra ngoài)
                else
                {
                    PizzaPlate sourcePlate = currentPlate;
                    int sCount = CountSlicesOfColor(sourcePlate, color);
                    int tEmpty = targetMergeCell.currentPlate.maxSlices - targetMergeCell.currentPlate.slices.Count;
                    int tCount = CountSlicesOfColor(targetMergeCell.currentPlate, color);
                    
                    // Phải thỏa mãn điều kiện chống Ping-Pong
                    if (tEmpty >= sCount || (tCount + sCount) >= targetMergeCell.currentPlate.maxSlices)
                    {
                        for (int i = sourcePlate.slices.Count - 1; i >= 0; i--)
                        {
                            if (sourcePlate.slices[i].color == color && !targetMergeCell.currentPlate.IsFull())
                            {
                                if (targetMergeCell.currentPlate.AddSlice(sourcePlate.slices[i])) moved = true;
                            }
                        }
                    }
                }
                
                if (moved)
                {
                    if (!activeQueue.Contains(targetMergeCell)) activeQueue.Enqueue(targetMergeCell);
                    if (!activeQueue.Contains(currentCell)) activeQueue.Enqueue(currentCell);
                }
            }
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
