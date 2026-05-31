using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GridConfig
{
    public int rows;
    public int columns;
    public float cellSize;
    public float spacing;
}

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Tên file JSON trong thư mục Resources (không bao gồm đuôi .json)")]
    public string configFile = "GridConfig";
    
    [Header("Prefab")]
    [Tooltip("Prefab của ô lưới. Khuyên dùng một khối vuông phẳng có gắn GridCell và BoxCollider.")]
    public GameObject cellPrefab;
    
    [Header("Grid Parent")]
    [Tooltip("Object cha để chứa các cell được tạo ra.")]
    public Transform gridParent;

    [Header("Testing & Design")]
    [Tooltip("Bật tick này nếu bạn muốn thay đổi kích thước và số lượng trực tiếp trong Inspector ở dưới để dễ thiết kế (bỏ qua file JSON).")]
    public bool useInspectorConfig = false;

    [Header("Current Config")]
    public GridConfig config;
    
    // Ma trận 2 chiều lưu trữ trạng thái của bàn chơi
    private GridCell[,] gridMap;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        LoadConfig();
        GenerateGrid();
    }

    private void LoadConfig()
    {
        if (useInspectorConfig)
        {
            Debug.Log("Đang sử dụng cấu hình từ Inspector để test thiết kế.");
            return; // Bỏ qua việc đọc từ JSON, dùng luôn biến config ở Inspector
        }

        TextAsset jsonText = Resources.Load<TextAsset>(configFile);
        if (jsonText != null)
        {
            config = JsonUtility.FromJson<GridConfig>(jsonText.text);
            Debug.Log($"Loaded GridConfig from JSON: {config.columns}x{config.rows}");
        }
        else
        {
            config = new GridConfig { rows = 5, columns = 5, cellSize = 2f, spacing = 0.2f };
        }
    }

    private void GenerateGrid()
    {
        if (config == null) return;

        if (gridParent == null)
        {
            gridParent = new GameObject("GridParent").transform;
            gridParent.SetParent(this.transform);
        }

        gridMap = new GridCell[config.rows, config.columns];

        float totalWidth = config.columns * config.cellSize + (config.columns - 1) * config.spacing;
        float totalDepth = config.rows * config.cellSize + (config.rows - 1) * config.spacing;
        
        Vector3 startOffset = new Vector3(-totalWidth / 2f + config.cellSize / 2f, 0, -totalDepth / 2f + config.cellSize / 2f);

        for (int row = 0; row < config.rows; row++)
        {
            for (int col = 0; col < config.columns; col++)
            {
                float xPos = col * (config.cellSize + config.spacing);
                float zPos = row * (config.cellSize + config.spacing);
                
                Vector3 position = startOffset + new Vector3(xPos, 0, zPos);

                GameObject cellObj;
                if (cellPrefab != null)
                {
                    cellObj = Instantiate(cellPrefab, position, Quaternion.identity, gridParent);
                }
                else
                {
                    // Tự tạo một khối Cube phẳng làm ô lưới
                    cellObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cellObj.transform.position = position;
                    cellObj.transform.SetParent(gridParent);
                    // Dẹt khối cube thành mặt phẳng
                    cellObj.transform.localScale = new Vector3(config.cellSize, 0.1f, config.cellSize);
                }

                cellObj.name = $"GridCell_{row}_{col}";
                
                // Gắn component GridCell nếu chưa có
                GridCell cellComponent = cellObj.GetComponent<GridCell>();
                if (cellComponent == null)
                {
                    cellComponent = cellObj.AddComponent<GridCell>();
                }
                
                // Đảm bảo có BoxCollider cho Raycast
                if (cellObj.GetComponent<BoxCollider>() == null)
                {
                    cellObj.AddComponent<BoxCollider>();
                }

                cellComponent.row = row;
                cellComponent.column = col;
                
                gridMap[row, col] = cellComponent;
            }
        }
    }

    /// <summary>
    /// Thuật toán kiểm tra lân cận 4 hướng (Trên, Dưới, Trái, Phải)
    /// Trả về danh sách tất cả các GridCell có chứa đĩa Pizza.
    /// </summary>
    public List<GridCell> GetAllNeighbors(int row, int col)
    {
        List<GridCell> neighbors = new List<GridCell>();

        int[] dRow = { -1, 1, 0, 0 };
        int[] dCol = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int checkRow = row + dRow[i];
            int checkCol = col + dCol[i];

            if (checkRow >= 0 && checkRow < config.rows && checkCol >= 0 && checkCol < config.columns)
            {
                GridCell neighborCell = gridMap[checkRow, checkCol];
                if (!neighborCell.IsEmpty)
                {
                    neighbors.Add(neighborCell);
                }
            }
        }

        return neighbors;
    }

    /// <summary>
    /// Kiểm tra xem toàn bộ các ô lưới đã bị lấp đầy chưa (Game Over condition)
    /// </summary>
    public bool IsGridFull()
    {
        if (gridMap == null) return false;

        for (int row = 0; row < config.rows; row++)
        {
            for (int col = 0; col < config.columns; col++)
            {
                if (gridMap[row, col].IsEmpty)
                {
                    return false; // Còn ít nhất 1 ô trống
                }
                
                // [FIX LỖI GAME OVER NGẦM]
                // NẾU ô không trống, nhưng đĩa trên ô đó CHUẨN BỊ NỔ (đã đủ 6 miếng cùng màu)
                // Thì nó sẽ sớm biến mất sau 0.4s animation, nên Grid chưa thực sự Full!
                if (gridMap[row, col].currentPlate != null && gridMap[row, col].currentPlate.IsReadyToBloom())
                {
                    return false; 
                }
            }
        }
        return true; // Tất cả đã đầy
    }

    /// <summary>
    /// Xóa toàn bộ đĩa trên lưới để chơi lại
    /// </summary>
    public void ClearAllPlates()
    {
        if (gridMap == null) return;

        for (int row = 0; row < config.rows; row++)
        {
            for (int col = 0; col < config.columns; col++)
            {
                GridCell cell = gridMap[row, col];
                if (!cell.IsEmpty && cell.currentPlate != null)
                {
                    Destroy(cell.currentPlate.gameObject);
                    cell.currentPlate = null;
                }
            }
        }
    }

    // --- LƯU & TẢI TRẠNG THÁI BÀN CỜ ---
    
    public void SaveGridState()
    {
        if (DataManager.Instance == null) return;
        
        List<GridCellData> savedGrid = new List<GridCellData>();
        
        for (int row = 0; row < config.rows; row++)
        {
            for (int col = 0; col < config.columns; col++)
            {
                GridCell cell = gridMap[row, col];
                if (!cell.IsEmpty && cell.currentPlate != null)
                {
                    GridCellData cellData = new GridCellData();
                    cellData.row = row;
                    cellData.col = col;
                    
                    PlateData pData = new PlateData();
                    foreach (var slice in cell.currentPlate.slices)
                    {
                        pData.slices.Add(new SliceData(slice.color));
                    }
                    
                    cellData.plate = pData;
                    savedGrid.Add(cellData);
                }
            }
        }
        
        DataManager.Instance.playerData.savedGrid = savedGrid;
    }

    public void LoadGridState()
    {
        if (DataManager.Instance == null || LobbyManager.Instance == null) return;
        
        // Trước khi load, xóa sạch bàn cờ
        ClearAllPlates();

        List<GridCellData> savedGrid = DataManager.Instance.playerData.savedGrid;
        if (savedGrid == null || savedGrid.Count == 0) return;

        foreach (var cellData in savedGrid)
        {
            if (cellData.row >= 0 && cellData.row < config.rows && cellData.col >= 0 && cellData.col < config.columns)
            {
                GridCell targetCell = gridMap[cellData.row, cellData.col];
                
                // Sinh ra đĩa
                PizzaPlate newPlate = Instantiate(LobbyManager.Instance.platePrefab);
                targetCell.SetPlate(newPlate);
                
                // Bỏ hiệu ứng spawn ban đầu vì đây là đĩa đang nằm trên bàn
                newPlate.transform.localScale = Vector3.one * 0.8f;

                // Sinh các lát cắt dựa theo data
                if (cellData.plate != null && cellData.plate.slices != null)
                {
                    // Vì cơ chế AddSlice của chúng ta cần di chuyển từ ngoài vào, nhưng lúc Load Game thì cần chúng xuất hiện tức thì
                    // Ta sẽ dùng hàm LoadSlicesFromSave thay thế cho GenerateRandomSlices
                    newPlate.LoadSlicesFromSave(cellData.plate.slices, LobbyManager.Instance.slicePrefabs);
                }
            }
        }
    }
}
