using UnityEngine;

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
        // Khởi tạo đĩa lần đầu tiên
        RefillLobby();
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
}
