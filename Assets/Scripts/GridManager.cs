using UnityEngine;

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
    [Header("Configuration")]
    [Tooltip("Tên file JSON trong thư mục Resources (không bao gồm đuôi .json)")]
    public string configFile = "GridConfig";
    
    [Header("Prefab")]
    [Tooltip("Prefab của ô lưới (ví dụ: Cube). Nếu để trống, sẽ tự tạo Cube mặc định.")]
    public GameObject cellPrefab;
    
    [Header("Grid Parent")]
    [Tooltip("Object cha để chứa các cell được tạo ra.")]
    public Transform gridParent;

    private GridConfig config;

    void Start()
    {
        LoadConfig();
        GenerateGrid();
    }

    private void LoadConfig()
    {
        // Đọc nội dung file JSON từ thư mục Resources
        TextAsset jsonText = Resources.Load<TextAsset>(configFile);
        if (jsonText != null)
        {
            config = JsonUtility.FromJson<GridConfig>(jsonText.text);
            Debug.Log($"Loaded GridConfig: {config.columns}x{config.rows}, CellSize: {config.cellSize}");
        }
        else
        {
            Debug.LogError($"Không tìm thấy file cấu hình {configFile} trong thư mục Resources! Đang sử dụng cấu hình mặc định.");
            // Cấu hình mặc định nếu không tìm thấy file
            config = new GridConfig { rows = 5, columns = 5, cellSize = 1f, spacing = 0.1f };
        }
    }

    private void GenerateGrid()
    {
        if (config == null) return;

        // Nếu chưa gán gridParent, tự tạo một object mới làm cha
        if (gridParent == null)
        {
            gridParent = new GameObject("GridParent").transform;
            gridParent.SetParent(this.transform);
        }

        // Tính toán chiều rộng và chiều sâu tổng thể của lưới để căn giữa
        float totalWidth = config.columns * config.cellSize + (config.columns - 1) * config.spacing;
        float totalDepth = config.rows * config.cellSize + (config.rows - 1) * config.spacing;
        
        Vector3 startOffset = new Vector3(-totalWidth / 2f + config.cellSize / 2f, 0, -totalDepth / 2f + config.cellSize / 2f);

        for (int row = 0; row < config.rows; row++)
        {
            for (int col = 0; col < config.columns; col++)
            {
                // Tính toán vị trí X và Z (trải trên mặt phẳng XZ)
                float xPos = col * (config.cellSize + config.spacing);
                float zPos = row * (config.cellSize + config.spacing);
                
                Vector3 position = startOffset + new Vector3(xPos, 0, zPos);

                GameObject cell;
                if (cellPrefab != null)
                {
                    cell = Instantiate(cellPrefab, position, Quaternion.identity, gridParent);
                }
                else
                {
                    // Tự tạo một khối Cube cơ bản nếu không có prefab được gán
                    cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cell.transform.position = position;
                    cell.transform.SetParent(gridParent);
                }

                cell.name = $"Cell_{row}_{col}";
                
                // Thay đổi kích thước (Scale) cho phù hợp với cellSize
                cell.transform.localScale = new Vector3(config.cellSize, config.cellSize, config.cellSize);
            }
        }
    }
}
