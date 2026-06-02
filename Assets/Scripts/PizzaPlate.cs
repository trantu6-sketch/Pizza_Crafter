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
        ApplySkin(); // Cập nhật ngoại hình đĩa theo Skin hiện tại
    }

    private GameObject currentSkinObj;

    /// <summary>
    /// Áp dụng Skin mới cho đĩa bằng cách ẩn Mesh cũ và load Prefab mới từ Resources
    /// </summary>
    public void ApplySkin()
    {
        if (DataManager.Instance == null) return;
        
        string equippedSkinId = DataManager.Instance.playerData.EquippedPlateSkin;

        // BƯỚC 1: Xóa skin cũ (nếu có) để không bị đè lên nhau khi chuyển Skin liên tục
        if (currentSkinObj != null)
        {
            Destroy(currentSkinObj);
            currentSkinObj = null;
        }

        MeshRenderer rootRenderer = GetComponent<MeshRenderer>();
        if (rootRenderer == null) rootRenderer = GetComponentInChildren<MeshRenderer>();

        // BƯỚC 2: Tìm thông tin Skin trong JSON
        SkinData skinData = null;
        if (ShopManager.Instance != null)
        {
            skinData = ShopManager.Instance.GetSkinData(equippedSkinId);
        }

        // Nếu Skin không có prefabPath, nghĩa là nó là cái đĩa Gốc (Mặc định)
        if (skinData == null || string.IsNullOrEmpty(skinData.prefabPath))
        {
            if (rootRenderer != null) rootRenderer.enabled = true;
            return;
        }

        // BƯỚC 3: Tải Skin mới từ Shop
        GameObject skinPrefab = Resources.Load<GameObject>(skinData.prefabPath);
        if (skinPrefab != null)
        {
            // Tắt hiển thị của đĩa gốc
            if (rootRenderer != null) rootRenderer.enabled = false;
            
            // Sinh Prefab Skin mới
            currentSkinObj = Instantiate(skinPrefab, transform);
            currentSkinObj.transform.localPosition = Vector3.zero;
            currentSkinObj.transform.localRotation = Quaternion.identity;
            
            // Cảnh báo: Tắt BoxCollider của Skin mới để tránh trùng lặp
            Collider[] colliders = currentSkinObj.GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
            {
                c.enabled = false;
            }
        }
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
    /// Kiểm tra xem đĩa có đủ 6 miếng cùng màu và chuẩn bị nổ hay không
    /// </summary>
    public bool IsReadyToBloom()
    {
        if (!IsFull()) return false;
        PizzaColor firstColor = slices[0].color;
        for (int i = 1; i < slices.Count; i++)
        {
            if (slices[i].color != firstColor) return false;
        }
        return true;
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

            // Tách rời miếng pizza đang bay ra khỏi đĩa hiện tại (Không truyền false để giữ đúng tọa độ World)
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
                
                // [QUAN TRỌNG] Phải giải phóng ô lưới NGAY LẬP TỨC để hệ thống (IsGridFull) biết là ô này đã trống!
                // Nếu không, 0.4s sau đĩa mới bị Destroy, hàm CheckGameOver chạy luôn lúc này sẽ lầm tưởng Grid đã Full và chuyển sang Game Over ngầm gây đơ game.
                if (currentCell != null)
                {
                    currentCell.currentPlate = null;
                }
                
                // Cộng điểm trên UI và cộng Exp
                int finalScore = 1;
                if (LevelManager.Instance != null)
                {
                    finalScore = LevelManager.Instance.GetScoreMultiplier();
                    
                    int expEarned = Random.Range(8, 11);
                    LevelManager.Instance.AddExp(expEarned);
                }

                if (GameStateManager.Instance != null)
                {
                    GameStateManager.Instance.AddScore(finalScore);
                }

                // [SAVE/LOAD SYSTEM] Cộng vàng và cập nhật tiến trình nhiệm vụ
                if (DataManager.Instance != null)
                {
                    DataManager.Instance.AddGold(finalScore);
                    DataManager.Instance.UpdateQuestProgress("Quest_Match_50_Plates", 1);
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
                        
                        // Sinh Text Điểm (Vàng) lệch trái
                        Vector3 pointPos = transform.position + new Vector3(-0.5f, 1.5f, 0);
                        GameObject textObj = ObjectPooler.Instance.SpawnFromPool("FloatingText", pointPos);
                        if (textObj != null)
                        {
                            var tmpro = textObj.GetComponentInChildren<TMPro.TextMeshPro>();
                            if (tmpro != null) {
                                tmpro.text = $"+{finalScore}";
                                tmpro.color = Color.yellow;
                            }
                        }

                            // Sinh Text Exp (Xanh dương) lệch phải
                            if (LevelManager.Instance != null)
                            {
                                Vector3 expPos = transform.position + new Vector3(0.5f, 1.5f, 0);
                                GameObject expObj = ObjectPooler.Instance.SpawnFromPool("FloatingText", expPos);
                                if (expObj != null)
                                {
                                    var tmproExp = expObj.GetComponentInChildren<TMPro.TextMeshPro>();
                                    if (tmproExp != null) {
                                        tmproExp.text = $"+{Random.Range(8, 11)} XP";
                                        tmproExp.color = new Color(0.2f, 0.6f, 1f);
                                    }
                                }
                            }
                        }

                        Destroy(gameObject);
                    });
            }
            else
            {
                // Nếu đĩa chưa nổ (chưa đủ 6 miếng cùng màu), tiến hành dồn các miếng cùng màu lại gần nhau
                GroupSlicesByColor();
            }
        }
    }

    /// <summary>
    /// Tự động hoán đổi và dồn các lát Pizza cùng màu lại nằm sát nhau trên đĩa.
    /// Giúp giải quyết tình trạng kẹt/không liền mạch.
    /// </summary>
    public void GroupSlicesByColor()
    {
        if (slices.Count <= 1) return;

        // Sắp xếp lại danh sách: Các lát cùng màu sẽ nằm cạnh nhau
        slices.Sort((a, b) => a.color.CompareTo(b.color));

        // Xóa sạch slotArray cũ
        if (slotArray != null)
        {
            for (int i = 0; i < maxSlices; i++)
            {
                slotArray[i] = null;
            }
        }
        else
        {
            slotArray = new PizzaSlice[maxSlices];
        }

        // Cập nhật lại vị trí và góc xoay cho từng lát
        for (int i = 0; i < slices.Count; i++)
        {
            slotArray[i] = slices[i];
            float angle = i * (360f / maxSlices);
            
            // Xoay mượt mà về vị trí mới
            slices[i].transform.DOLocalRotate(new Vector3(0, angle, 0), 0.3f).SetEase(Ease.OutQuad);
        }
    }

    public void ReturnToOriginalPosition()
    {
        transform.position = originalPosition;
    }

    /// <summary>
    /// Thuật toán sinh ngẫu nhiên 1-4 lát cắt, chia theo cụm màu (Độ khó tăng theo Level)
    /// </summary>
    public void GenerateRandomSlices(PizzaSlice[] slicePrefabs)
    {
        slices.Clear();

        // Số lượng lát muốn sinh: 1 đến 4 như yêu cầu
        int totalSlicesToSpawn = Random.Range(1, 5); 
        
        int numChunks = 1;
        if (totalSlicesToSpawn > 1)
        {
            int currentLevel = 1;
            if (LevelManager.Instance != null) currentLevel = LevelManager.Instance.CurrentLevel;

            // Xác suất mặc định (Level 1-5): Dễ
            float chance1Color = 0.70f;
            float chance2Color = 0.30f;
            
            // Level trung bình
            if (currentLevel >= 6 && currentLevel <= 15)
            {
                chance1Color = 0.45f; // Giảm từ 70% -> 45% như bạn yêu cầu
                chance2Color = 0.40f;
            }
            // Level khó
            else if (currentLevel > 15)
            {
                chance1Color = 0.30f;
                chance2Color = 0.40f;
            }

            float randomVal = Random.value;
            if (randomVal <= chance1Color) numChunks = 1;
            else if (randomVal <= chance1Color + chance2Color) numChunks = 2;
            else numChunks = 3;

            // Đảm bảo số lượng chunk không lớn hơn tổng số lát
            if (numChunks > totalSlicesToSpawn) numChunks = totalSlicesToSpawn;
        }
        
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
                // Thêm tham số 'false' để Unity giữ nguyên LocalScale của Prefab (ví dụ 1.12), không bị bù trừ theo Scale của Đĩa mẹ
                PizzaSlice newSlice = Instantiate(prefab, transform, false);
                
                // Lưu lại scale gốc (ví dụ 1.12) để tí nữa phóng to đúng kích cỡ đó
                Vector3 originalScale = newSlice.transform.localScale;
                
                // Set scale về 0 để chuẩn bị làm hiệu ứng DOTween Popup
                newSlice.transform.localScale = Vector3.zero;
                
                // Tính toán góc dựa vào số lượng lát đã có
                int currentCount = slices.Count;
                float angle = currentCount * (360f / maxSlices);
                newSlice.transform.localPosition = new Vector3(0, sliceYOffset, 0);
                newSlice.transform.localRotation = Quaternion.Euler(0, angle, 0);
                
                // DoTween Popup (về đúng kích cỡ ban đầu thay vì Vector3.one)
                newSlice.transform.DOScale(originalScale, 0.4f).SetEase(Ease.OutBack).SetDelay(currentCount * 0.1f);

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

    /// <summary>
    /// Dùng riêng cho lúc Load Game: Tạo ngay các lát Pizza theo Data (không dùng DOtween)
    /// </summary>
    public void LoadSlicesFromSave(List<SliceData> savedSlices, PizzaSlice[] slicePrefabs)
    {
        slices.Clear();
        if (slotArray == null) slotArray = new PizzaSlice[maxSlices];
        else
        {
            for (int i = 0; i < maxSlices; i++) slotArray[i] = null;
        }

        for (int i = 0; i < savedSlices.Count; i++)
        {
            if (i >= maxSlices) break;

            // Tìm prefab tương ứng với màu này
            PizzaSlice prefabToSpawn = slicePrefabs[0]; // Mặc định cái đầu tiên
            foreach (var prefab in slicePrefabs)
            {
                if (prefab.color == savedSlices[i].color)
                {
                    prefabToSpawn = prefab;
                    break;
                }
            }

            // Sinh lát cắt, truyền 'false' để giữ nguyên LocalScale gốc
            PizzaSlice newSlice = Instantiate(prefabToSpawn, transform, false);
            
            // Xóa hiệu ứng Popup đang có (nếu Start/Awake của Slice có gọi DOTween)
            newSlice.transform.DOKill();
            
            // Đặt Scale về mặc định của Prefab
            newSlice.transform.localScale = new Vector3(1f, 1f, 1f); // Script PizzaSlice có gán originalPrefabScale = (1,1,1)

            // Đặt vào đúng vị trí slot
            float angle = i * (360f / maxSlices);
            newSlice.transform.localPosition = new Vector3(0, sliceYOffset, 0);
            newSlice.transform.localRotation = Quaternion.Euler(0, angle, 0);

            newSlice.currentPlate = this;
            slices.Add(newSlice);
            slotArray[i] = newSlice;
        }
    }
}
