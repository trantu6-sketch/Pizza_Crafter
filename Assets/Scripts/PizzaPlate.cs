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
    
    // Danh sách các lát Pizza đang có trên đĩa này (Dùng cho logic duyệt)
    public List<PizzaSlice> slices = new List<PizzaSlice>();
    
    // Mảng 6 ô cứng để theo dõi vị trí góc của từng lát (tránh đè nhau)
    private PizzaSlice[] slotArray;

    [HideInInspector]
    public GridCell currentCell;
    
    [HideInInspector]
    public HoldSlot currentHoldSlot;
    
    [HideInInspector]
    public Vector3 originalPosition;

    void Start()
    {
        slotArray = new PizzaSlice[maxSlices];
        
        // Chỉ gán lại originalPosition nếu nó chưa được LobbyManager/HoldSlot set từ trước (ví dụ kéo thả thủ công)
        if (originalPosition == Vector3.zero)
        {
            originalPosition = transform.position;
        }
        
        // Nạp các lát pizza có sẵn ban đầu
        PizzaSlice[] initialSlices = GetComponentsInChildren<PizzaSlice>();
        foreach (var slice in initialSlices)
        {
            if (!slices.Contains(slice))
            {
                slice.currentPlate = this;
                slices.Add(slice);
            }
        }
        
        // Đồng bộ mảng slot
        for (int i = 0; i < slices.Count; i++)
        {
            slotArray[i] = slices[i];
        }

        ArrangeSlicesInstantly();
    }

    /// <summary>
    /// Lấy màu của lớp Pizza hiện tại
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
    /// Nhận 1 lát pizza mới, tìm ô trống và cho nó bay tới
    /// </summary>
    public bool AddSlice(PizzaSlice slice)
    {
        if (IsFull()) return false;

        // Tìm vị trí ô trống đầu tiên trên đĩa (từ 0 đến 5)
        int emptySlot = -1;
        if (slotArray == null) slotArray = new PizzaSlice[maxSlices];
        for (int i = 0; i < maxSlices; i++)
        {
            if (slotArray[i] == null)
            {
                emptySlot = i;
                break;
            }
        }
        
        if (emptySlot == -1) return false; // Hết chỗ

        // Xóa khỏi đĩa cũ
        if (slice.currentPlate != null)
        {
            slice.currentPlate.RemoveSlice(slice);
        }

        slices.Add(slice);
        slotArray[emptySlot] = slice;
        slice.currentPlate = this;

        // Tính toán góc dựa trên vị trí slot cố định (để lấp vào đúng lỗ hổng)
        float angle = emptySlot * (360f / maxSlices);
        
        Vector3 targetLocalPos = new Vector3(0, 1f, 0); 
        Quaternion targetLocalRot = Quaternion.Euler(0, angle, 0);

        // Ra lệnh bay
        slice.MoveToPlate(this, targetLocalPos, targetLocalRot, OnSliceArrived);
        
        return true;
    }

    public void RemoveSlice(PizzaSlice slice)
    {
        if (slices.Contains(slice))
        {
            slices.Remove(slice);
            
            // Xóa khỏi slotArray để tạo lỗ hổng
            if (slotArray != null)
            {
                for (int i = 0; i < maxSlices; i++)
                {
                    if (slotArray[i] == slice)
                    {
                        slotArray[i] = null;
                        break;
                    }
                }
            }

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

                // Kích hoạt âm thanh nổ với hiệu ứng Pitch Shift
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayExplosionSound();
                }

                // Hủy các lát cắt ngay lập tức
                foreach (var slice in slices)
                {
                    Destroy(slice.gameObject);
                }
                slices.Clear();

                // Tạo hiệu ứng thu nhỏ cho chính cái đĩa, SAU ĐÓ mới nổ Particle và hiện Text
                transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InBack).OnComplete(() => {
                    
                    // Sinh hiệu ứng Particle nổ từ Pool
                    if (ObjectPooler.Instance != null)
                    {
                        ObjectPooler.Instance.SpawnFromPool("ExplosionVFX", transform.position, Quaternion.identity);
                        
                        // Sinh Text cộng điểm bay lên (giữ nguyên góc xoay Prefab)
                        Vector3 textPos = transform.position + new Vector3(0, 1.5f, 0);
                        GameObject textObj = ObjectPooler.Instance.SpawnFromPool("FloatingText", textPos);
                        if (textObj != null)
                        {
                            var tmpro = textObj.GetComponentInChildren<TMPro.TextMeshPro>();
                            if (tmpro != null) tmpro.text = "+1";
                        }
                    }

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
                
                // Tính toán góc dựa vào số lượng lát đã có
                int currentCount = slices.Count;
                float angle = currentCount * (360f / maxSlices);
                newSlice.transform.localPosition = new Vector3(0, sliceYOffset, 0);
                newSlice.transform.localRotation = Quaternion.Euler(0, angle, 0);
                
                // DoTween Popup
                newSlice.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetDelay(currentCount * 0.1f);

                slices.Add(newSlice);
                if (slotArray == null) slotArray = new PizzaSlice[maxSlices];
                if (currentCount < maxSlices) slotArray[currentCount] = newSlice;
                
                newSlice.currentPlate = this;
                spawnedCount++;
            }
        }
    }

    public void PlaySpawnAnimation()
    {
        // Hiệu ứng cái đĩa xuất hiện
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one * 0.9f, 0.5f).SetEase(Ease.OutBack);
    }
}
