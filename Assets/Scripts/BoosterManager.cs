using UnityEngine;
using DG.Tweening;
using System.Collections;

public enum BoosterType
{
    None,
    Hammer,
    Swap,
    Trash,
    Reroll
}

public class BoosterManager : MonoBehaviour
{
    public static BoosterManager Instance { get; private set; }

    [Header("Pricing")]
    public int rerollCost = 20;
    public int trashCost = 30;
    public int swapCost = 50;
    public int hammerCost = 100;

    private BoosterType activeBooster = BoosterType.None;
    private GridCell firstSwapCell = null;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        // Chờ Click chuột cho thao tác Đĩa 2 của vật phẩm SWAP
        if (activeBooster == BoosterType.Swap && GameStateManager.Instance != null && GameStateManager.Instance.IsState<BoosterState>())
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleSecondClickForSwap();
            }
            
            // Hủy nếu bấm chuột phải hoặc ESC
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelBoosterDrop();
            }
        }
    }

    // ==========================================
    // LẤY SỐ LƯỢNG (Cho UI hiển thị)
    // ==========================================
    public int GetBoosterCount(BoosterType type)
    {
        if (DataManager.Instance == null) return 0;
        
        switch (type)
        {
            case BoosterType.Hammer: return DataManager.Instance.playerData.hammerCount;
            case BoosterType.Swap: return DataManager.Instance.playerData.swapCount;
            case BoosterType.Trash: return DataManager.Instance.playerData.trashCount;
            case BoosterType.Reroll: return DataManager.Instance.playerData.rerollCount;
        }
        return 0;
    }

    private void ConsumeBooster(BoosterType type)
    {
        if (DataManager.Instance == null) return;

        switch (type)
        {
            case BoosterType.Hammer: DataManager.Instance.playerData.hammerCount--; break;
            case BoosterType.Swap: DataManager.Instance.playerData.swapCount--; break;
            case BoosterType.Trash: DataManager.Instance.playerData.trashCount--; break;
            case BoosterType.Reroll: DataManager.Instance.playerData.rerollCount--; break;
        }
        
        DataManager.Instance.SaveData();
    }

    // ==========================================
    // NHẬN TÍN HIỆU KHI THẢ (DROP) ICON
    // ==========================================
    public void ProcessBoosterDrop(BoosterType type, RaycastHit hit)
    {
        PizzaPlate targetPlate = hit.collider != null ? hit.collider.GetComponentInParent<PizzaPlate>() : null;

        switch (type)
        {
            case BoosterType.Reroll:
                // Reroll có thể thả ở bất kỳ đâu trên màn hình, không cần target
                ConsumeBooster(BoosterType.Reroll);
                if (LobbyManager.Instance != null) LobbyManager.Instance.ForceRerollAll();
                ExitBoosterModeAndSave();
                break;

            case BoosterType.Hammer:
                if (targetPlate != null && targetPlate.currentCell != null)
                {
                    ExecuteHammer(targetPlate.currentCell);
                }
                else CancelBoosterDrop(); // Trượt
                break;

            case BoosterType.Trash:
                if (targetPlate != null && targetPlate.currentHoldSlot != null)
                {
                    ExecuteTrash(targetPlate.currentHoldSlot);
                }
                else CancelBoosterDrop(); // Trượt
                break;

            case BoosterType.Swap:
                if (targetPlate != null && targetPlate.currentCell != null)
                {
                    // Đánh dấu đĩa thứ nhất và chờ click đĩa thứ 2
                    activeBooster = BoosterType.Swap;
                    firstSwapCell = targetPlate.currentCell;
                    
                    // Hiệu ứng nảy lên để dễ nhìn
                    targetPlate.transform.DOMoveY(targetPlate.transform.position.y + 0.5f, 0.2f);
                    Debug.Log("[Booster] Đã Drop Swap lên Đĩa 1. Hãy Click chọn Đĩa 2!");
                }
                else CancelBoosterDrop(); // Trượt
                break;
        }
    }

    public void CancelBoosterDrop()
    {
        if (activeBooster == BoosterType.Swap && firstSwapCell != null && firstSwapCell.currentPlate != null)
        {
            // Trả Đĩa 1 về độ cao cũ
            firstSwapCell.currentPlate.transform.DOMoveY(firstSwapCell.currentPlate.transform.position.y - 0.5f, 0.2f);
        }

        activeBooster = BoosterType.None;
        firstSwapCell = null;
        
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsState<BoosterState>())
        {
            GameStateManager.Instance.ChangeState(GameStateManager.Instance.PlayingState);
        }
        
        Debug.Log("[Booster] Hủy dùng kỹ năng (Trả vật phẩm lại túi).");
    }

    // ==========================================
    // LOGIC ĐẶC BIỆT CHO SWAP (Cú click thứ 2)
    // ==========================================
    private void HandleSecondClickForSwap()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            PizzaPlate plate2 = hit.collider.GetComponentInParent<PizzaPlate>();
            
            if (plate2 != null && plate2.currentCell != null)
            {
                if (plate2.currentCell == firstSwapCell)
                {
                    // Click lại vào đĩa cũ -> Coi như Hủy
                    CancelBoosterDrop();
                }
                else
                {
                    ExecuteSwap(firstSwapCell, plate2.currentCell);
                }
            }
        }
    }

    // ==========================================
    // THỰC THI HIỆU ỨNG VÀ TRỪ SỐ LƯỢNG
    // ==========================================

    private void ExecuteHammer(GridCell targetCell)
    {
        ConsumeBooster(BoosterType.Hammer);
        
        PizzaPlate plateToDestroy = targetCell.currentPlate;
        targetCell.currentPlate = null; // Giải phóng ô cờ

        plateToDestroy.transform.DOShakeScale(0.3f, 0.5f, 10, 90, true).OnComplete(() => {
            plateToDestroy.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => {
                Destroy(plateToDestroy.gameObject);
                ExitBoosterModeAndSave();
            });
        });
    }

    private void ExecuteTrash(HoldSlot targetSlot)
    {
        ConsumeBooster(BoosterType.Trash);
        
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.TrashSlot(targetSlot);
        }
        
        ExitBoosterModeAndSave();
    }

    private void ExecuteSwap(GridCell cell1, GridCell cell2)
    {
        ConsumeBooster(BoosterType.Swap);

        PizzaPlate plate1 = cell1.currentPlate;
        PizzaPlate plate2 = cell2.currentPlate;

        // Tráo tham chiếu
        cell1.currentPlate = plate2;
        cell2.currentPlate = plate1;

        if (plate1 != null) plate1.currentCell = cell2;
        if (plate2 != null) plate2.currentCell = cell1;

        Vector3 pos1 = cell1.transform.position + new Vector3(0, 0.2f, 0); // Vị trí Đĩa 1 (đang nhô lên)
        Vector3 pos2 = cell2.transform.position; // Vị trí Đĩa 2 (thấp)
        
        // Tính toán tọa độ hạ cánh (Về đúng chuẩn)
        Vector3 landPos1 = cell1.transform.position;
        Vector3 landPos2 = cell2.transform.position;

        Sequence seq = DOTween.Sequence();
        
        if (plate1 != null)
            seq.Join(plate1.transform.DOJump(landPos2, 2f, 1, 0.5f));
            
        if (plate2 != null)
            seq.Join(plate2.transform.DOJump(landPos1, 2f, 1, 0.5f));

        seq.OnComplete(() => {
            ExitBoosterModeAndSave();
        });
    }

    private void ExitBoosterModeAndSave()
    {
        activeBooster = BoosterType.None;
        firstSwapCell = null;
        
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ChangeState(GameStateManager.Instance.PlayingState);
            GameStateManager.Instance.TriggerAutoSave();
        }
    }
}
