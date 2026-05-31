using UnityEngine;
using DG.Tweening;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Lobby Settings")]
    [Tooltip("3 khay chứa đĩa chờ kéo")]
    public HoldSlot[] holdSlots = new HoldSlot[3];

    [Header("Prefabs")]
    public PizzaPlate platePrefab;
    public PizzaSlice[] slicePrefabs;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (DataManager.Instance != null && DataManager.Instance.playerData.hasSavedGame)
        {
            // Nếu có dữ liệu save, tải lại trạng thái bàn cờ và sảnh
            GridManager.Instance.LoadGridState();
            LoadLobbyState();
        }
        else
        {
            // Trò chơi mới hoàn toàn
            RefillLobby();
        }
    }

    /// <summary>
    /// Kiểm tra xem cả 3 khay chứa có đang trống không, nếu có thì bơm đĩa mới.
    /// Hàm này được gọi từ DragDropManager mỗi khi người chơi đặt thành công 1 đĩa lên Grid.
    /// </summary>
    public void CheckAndRefill()
    {
        bool allEmpty = true;
        foreach (var slot in holdSlots)
        {
            if (!slot.IsEmpty)
            {
                allEmpty = false;
                break;
            }
        }

        if (allEmpty)
        {
            Debug.Log("[Lobby] Khay chứa đã trống hoàn toàn. Đang sinh 3 đĩa mới...");
            RefillLobby();
        }
    }

    private void RefillLobby()
    {
        foreach (var slot in holdSlots)
        {
            if (slot.IsEmpty)
            {
                // Tạo cái đĩa
                PizzaPlate newPlate = Instantiate(platePrefab);
                slot.SetPlate(newPlate);
                
                // Chạy hiệu ứng mọc cái đĩa
                newPlate.PlaySpawnAnimation();

                // Sinh lát pizza ngẫu nhiên
                newPlate.GenerateRandomSlices(slicePrefabs);
            }
        }
    }

    /// <summary>
    /// Dùng cho vật phẩm Re-roll: Vứt hết đĩa cũ và tạo 3 đĩa mới.
    /// </summary>
    public void ForceRerollAll()
    {
        foreach (var slot in holdSlots)
        {
            if (!slot.IsEmpty && slot.currentPlate != null)
            {
                Destroy(slot.currentPlate.gameObject);
                slot.ClearSlot();
            }
        }
        // Gọi RefillLobby để nó lấp đầy 3 khay vừa xóa
        RefillLobby();
    }

    /// <summary>
    /// Dùng cho vật phẩm Trash: Vứt 1 đĩa cụ thể và tạo đĩa mới thay thế.
    /// </summary>
    public void TrashSlot(HoldSlot slot)
    {
        if (!slot.IsEmpty && slot.currentPlate != null)
        {
            // Hiệu ứng thu nhỏ trước khi xóa
            PizzaPlate plateToDestroy = slot.currentPlate;
            slot.ClearSlot();
            
            plateToDestroy.transform.DOScale(Vector3.zero, 0.3f).SetEase(DG.Tweening.Ease.InBack).OnComplete(() => {
                Destroy(plateToDestroy.gameObject);
                // Sau khi xóa xong thì refill ngay ô đó
                RefillLobby();
            });
        }
    }

    // --- LƯU & TẢI TRẠNG THÁI LOBBY ---

    public void SaveLobbyState()
    {
        if (DataManager.Instance == null) return;
        
        System.Collections.Generic.List<PlateData> savedLobby = new System.Collections.Generic.List<PlateData>();
        
        foreach (var slot in holdSlots)
        {
            if (!slot.IsEmpty && slot.currentPlate != null)
            {
                PlateData pData = new PlateData();
                foreach (var slice in slot.currentPlate.slices)
                {
                    pData.slices.Add(new SliceData(slice.color));
                }
                savedLobby.Add(pData);
            }
            else
            {
                // Nếu khay trống, ta vẫn thêm 1 mảng null để giữ đúng vị trí index (có 3 khay)
                savedLobby.Add(null);
            }
        }
        
        DataManager.Instance.playerData.savedLobby = savedLobby;
    }

    public void LoadLobbyState()
    {
        if (DataManager.Instance == null) return;

        System.Collections.Generic.List<PlateData> savedLobby = DataManager.Instance.playerData.savedLobby;
        if (savedLobby == null || savedLobby.Count == 0) return;

        for (int i = 0; i < holdSlots.Length; i++)
        {
            if (i >= savedLobby.Count) break;

            HoldSlot slot = holdSlots[i];
            PlateData pData = savedLobby[i];

            if (pData != null && pData.slices != null && pData.slices.Count > 0)
            {
                // Sinh ra đĩa
                PizzaPlate newPlate = Instantiate(platePrefab);
                slot.SetPlate(newPlate);
                
                // Bỏ hiệu ứng spawn ban đầu vì đây là đĩa đã có sẵn
                newPlate.transform.localScale = Vector3.one * 0.9f;

                // Load các lát Pizza
                newPlate.LoadSlicesFromSave(pData.slices, slicePrefabs);
            }
        }
    }
}
