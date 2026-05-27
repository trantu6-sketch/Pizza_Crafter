using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

[RequireComponent(typeof(BoxCollider))]
public class PizzaPlate : MonoBehaviour
{
    [Header("Pizza Slices Configuration")]
    public int maxSlices = 6;
    
    [Tooltip("Độ cao để lát pizza nổi lên trên mặt đĩa thay vì lún xuống đáy")]
    public float sliceYOffset = 1.0f;
    
    // Danh sách các lát Pizza đang có trên đĩa này
    public List<PizzaSlice> slices = new List<PizzaSlice>();

    [HideInInspector]
    public GridCell currentCell;
    
    [HideInInspector]
    public HoldSlot currentHoldSlot;
    
    [HideInInspector]
    public Vector3 originalPosition;

    void Start()
    {
        // Chỉ gán lại originalPosition nếu nó chưa được LobbyManager/HoldSlot set từ trước (ví dụ kéo thả thủ công)
        if (originalPosition == Vector3.zero)
        {
            originalPosition = transform.position;
        }
        
        // Nạp các lát pizza có sẵn ban đầu (nếu người dùng kéo vào prefab làm child)
        PizzaSlice[] initialSlices = GetComponentsInChildren<PizzaSlice>();
        foreach (var slice in initialSlices)
        {
            if (!slices.Contains(slice))
            {
                slice.currentPlate = this;
                slices.Add(slice);
            }
        }
        ArrangeSlicesInstantly();
    }

    /// <summary>
    /// Lấy màu của lớp Pizza hiện tại (Màu của lát mới nhất, hoặc None nếu trống)
    /// Lưu ý: Trò chơi Bloom Sort thường các cánh hoa cùng tầng sẽ cùng màu, 
    /// ở đây ta lấy màu của lát trên cùng.
    /// </summary>
    public PizzaColor GetTopColor()
    {
        if (slices.Count == 0) return PizzaColor.None;
        return slices[slices.Count - 1].color;
    }

    public bool IsFull()
    {
        return slices.Count >= maxSlices;
    }

    public bool IsEmpty()
    {
        return slices.Count == 0;
    }

    /// <summary>
    /// Nhận 1 lát pizza mới, tính toán vị trí và góc rồi cho nó bay tới
    /// </summary>
    public void AddSlice(PizzaSlice slice)
    {
        if (IsFull()) return;

        // Xóa khỏi đĩa cũ
        if (slice.currentPlate != null)
        {
            slice.currentPlate.RemoveSlice(slice);
        }

        slices.Add(slice);
        slice.currentPlate = this;

        // Tính toán vị trí và góc xoay dựa trên index hiện tại (tạo thành vòng tròn 6 miếng)
        int index = slices.Count - 1;
        float angle = index * (360f / maxSlices);
        
        // Đẩy lát cắt lên theo trục Y = 1 cứng như yêu cầu
        Vector3 targetLocalPos = new Vector3(0, 1f, 0); 
        Quaternion targetLocalRot = Quaternion.Euler(0, angle, 0);

        // Ra lệnh bay
        slice.MoveToPlate(this, targetLocalPos, targetLocalRot, OnSliceArrived);
    }

    public void RemoveSlice(PizzaSlice slice)
    {
        if (slices.Contains(slice))
        {
            slices.Remove(slice);
            // Tách rời miếng pizza đang bay ra khỏi đĩa hiện tại để đĩa không kéo theo nó nếu bị hủy
            slice.transform.SetParent(null);
            
            // Nếu đĩa này nằm trên Grid (có GridCell) và vừa bị lấy đi miếng cuối cùng
            if (slices.Count == 0 && currentCell != null)
            {
                currentCell.currentPlate = null;
                // Thu nhỏ rồi biến mất
                transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InBack).OnComplete(() => {
                    Destroy(gameObject);
                });
            }
        }
    }

    /// <summary>
    /// Sắp xếp tức thì không có animation (Dùng khi khởi tạo lúc đầu)
    /// </summary>
    private void ArrangeSlicesInstantly()
    {
        for (int i = 0; i < slices.Count; i++)
        {
            float angle = i * (360f / maxSlices);
            slices[i].transform.localPosition = new Vector3(0, 1f, 0);
            slices[i].transform.localRotation = Quaternion.Euler(0, angle, 0);
        }
    }

    private void OnSliceArrived()
    {
        // Khi một miếng vừa bay tới xong, ta kiểm tra xem đĩa đã đủ 6 miếng chưa
        CheckBloom();
    }

    private void CheckBloom()
    {
        if (IsFull())
        {
            // Kiểm tra xem tất cả 6 miếng có cùng 1 màu không
            PizzaColor firstColor = slices[0].color;
            bool allSame = true;
            for (int i = 1; i < slices.Count; i++)
            {
                if (slices[i].color != firstColor)
                {
                    allSame = false;
                    break;
                }
            }

            if (allSame)
            {
                Debug.Log($"[Logic Core] BÙM! Đĩa ở {currentCell.row}, {currentCell.column} đã đủ 6 lát màu {firstColor}. CỘNG 1 ĐIỂM!");
                
                // Cộng điểm trên UI
                if (GameStateManager.Instance != null)
                {
                    GameStateManager.Instance.AddScore(1);
                }

                // Giải phóng ô lưới
                if (currentCell != null)
                {
                    currentCell.currentPlate = null;
                }

                // Hủy các lát cắt (Tạm thời dùng Destroy, tuần sau sẽ dùng Object Pooling)
                foreach (var slice in slices)
                {
                    Destroy(slice.gameObject);
                }
                slices.Clear();

                // Tạo hiệu ứng thu nhỏ cho chính cái đĩa rồi biến mất
                transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InBack).OnComplete(() => {
                    Destroy(gameObject);
                });
            }
        }
    }

    public void ReturnToOriginalPosition()
    {
        transform.position = originalPosition;
    }

    /// <summary>
    /// Thuật toán sinh ngẫu nhiên 1-4 lát cắt, chia theo cụm màu
    /// </summary>
    public void GenerateRandomSlices(PizzaSlice[] slicePrefabs)
    {
        slices.Clear();

        // Số lượng lát muốn sinh: 1 đến 4 như yêu cầu
        int totalSlicesToSpawn = Random.Range(1, 5); 
        
        // Chia làm 1 hoặc 2 cụm (Chunk) để các lát liền kề có màu giống nhau
        int numChunks = (totalSlicesToSpawn > 1) ? Random.Range(1, 3) : 1;
        
        int spawnedCount = 0;

        for (int c = 0; c < numChunks; c++)
        {
            // Cụm cuối cùng lấy nốt số lát còn lại, ngược lại thì random 1 phần
            int slicesInChunk = (c == numChunks - 1) ? (totalSlicesToSpawn - spawnedCount) : Random.Range(1, totalSlicesToSpawn - spawnedCount);
            
            // Chọn ngẫu nhiên 1 màu (1 prefab)
            int randomPrefabIndex = Random.Range(0, slicePrefabs.Length);
            PizzaSlice prefab = slicePrefabs[randomPrefabIndex];

            for (int i = 0; i < slicesInChunk; i++)
            {
                PizzaSlice newSlice = Instantiate(prefab, transform);
                
                // Set scale về 0 để chuẩn bị làm hiệu ứng DOTween Popup
                newSlice.transform.localScale = Vector3.zero;
                
                float angle = slices.Count * (360f / maxSlices);
                newSlice.transform.localPosition = new Vector3(0, sliceYOffset, 0);
                newSlice.transform.localRotation = Quaternion.Euler(0, angle, 0);
                
                // DoTween Popup, delay một chút để sinh ra từ từ nối đuôi nhau
                newSlice.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetDelay(slices.Count * 0.1f);

                slices.Add(newSlice);
                newSlice.currentPlate = this;
                spawnedCount++;
            }
        }
    }

    public void PlaySpawnAnimation()
    {
        // Hiệu ứng cái đĩa xuất hiện
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
    }
}
