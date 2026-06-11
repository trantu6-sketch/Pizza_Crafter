using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[System.Serializable]
public class SkinData
{
    public string id;
    public string name;
    public int price;
    public string prefabPath;
    public string iconPath; // Đường dẫn ảnh 2D trong Resources
}

[System.Serializable]
public class SkinDatabase
{
    public List<SkinData> skins;
}

public class BoosterData
{
    public string name;
    public BoosterType type;
    public int price;
    public string iconPath;
}

public enum ShopTab
{
    Skins,
    Boosters
}

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Shop Config")]
    public string databaseFileName = "SkinDatabase";

    [Header("UI Panels")]
    public GameObject shopPanel;
    public TextMeshProUGUI goldText;
    
    [Header("Shop Tabs Content")]
    [Tooltip("Gom tất cả UI của vòng quay Skin/Booster (Ảnh, Giá, Nút Mua, Chấm) vào 1 Panel và kéo vào đây")]
    public GameObject carouselTabContent;

    [Header("Carousel Display")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPriceText;
    public Image itemIcon;
    
    [Header("Buttons")]
    public Button leftButton;
    public Button rightButton;
    public Button buyButton;
    public TextMeshProUGUI buyButtonText;

    [Header("Pagination Dots")]
    public Transform dotsContainer;
    public GameObject dotPrefab;
    public Color dotActiveColor = Color.yellow;
    public Color dotInactiveColor = Color.gray;

    private SkinDatabase skinDatabase;
    private List<BoosterData> boosterDatabase;
    
    private int currentIndex = 0;
    private List<Image> paginationDots = new List<Image>();
    
    private ShopTab currentTab = ShopTab.Skins;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        // Khởi tạo Database cho Booster
        boosterDatabase = new List<BoosterData>() {
            new BoosterData { name = "Làm Mới (Re-roll)", type = BoosterType.Reroll, price = BoosterManager.Instance != null ? BoosterManager.Instance.rerollCost : 20, iconPath = "Boosters/Icon_Reroll" },
            new BoosterData { name = "Thùng Rác (Trash)", type = BoosterType.Trash, price = BoosterManager.Instance != null ? BoosterManager.Instance.trashCost : 30, iconPath = "Boosters/Icon_Trash" },
            new BoosterData { name = "Đổi Chỗ (Swap)", type = BoosterType.Swap, price = BoosterManager.Instance != null ? BoosterManager.Instance.swapCost : 50, iconPath = "Boosters/Icon_Swap" },
            new BoosterData { name = "Búa Tạ (Hammer)", type = BoosterType.Hammer, price = BoosterManager.Instance != null ? BoosterManager.Instance.hammerCost : 100, iconPath = "Boosters/Icon_Hammer" }
        };
    }

    void Start()
    {
        LoadDatabase();
        
        // Gắn sự kiện cho các nút
        if (leftButton != null) leftButton.onClick.AddListener(PreviousItem);
        if (rightButton != null) rightButton.onClick.AddListener(NextItem);
        if (buyButton != null) buyButton.onClick.AddListener(OnBuyOrEquipClicked);

        UpdateGoldUI(DataManager.Instance != null ? DataManager.Instance.playerData.Gold : 0);
    }

    void OnEnable()
    {
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.OnGoldChanged += OnGoldChanged;
        }
    }

    void OnDisable()
    {
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.OnGoldChanged -= OnGoldChanged;
        }
    }

    private void OnGoldChanged(int newGold)
    {
        UpdateGoldUI(newGold);
        if (shopPanel != null && shopPanel.activeInHierarchy)
        {
            UpdateDisplay(); // Cập nhật lại trạng thái mờ/sáng của nút Mua
        }
    }

    private void LoadDatabase()
    {
        TextAsset jsonText = Resources.Load<TextAsset>(databaseFileName);
        if (jsonText != null)
        {
            skinDatabase = JsonUtility.FromJson<SkinDatabase>(jsonText.text);
            Debug.Log($"[ShopManager] Nạp thành công {skinDatabase.skins.Count} skins từ JSON.");
        }
        else
        {
            Debug.LogError("[ShopManager] Không tìm thấy file JSON cấu hình skin!");
        }
    }

    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            
            // [DOTween] Hiệu ứng nảy lên (Pop-up) khi mở Shop
            shopPanel.transform.localScale = Vector3.zero;
            shopPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }
        
        // Luôn hiển thị tab Skins khi mở shop
        OpenSkinTab();
        UpdateGoldUI(DataManager.Instance != null ? DataManager.Instance.playerData.Gold : 0);
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            // [DOTween] Hiệu ứng thu nhỏ dần rồi mới tắt
            shopPanel.transform.DOScale(Vector3.zero, 0.3f)
                .SetEase(Ease.InBack)
                .OnComplete(() => {
                    shopPanel.SetActive(false);
                });
        }
    }

    public void UpdateGoldUI(int currentGold)
    {
        if (goldText != null)
        {
            goldText.text = currentGold.ToString();
        }
    }

    // ================== LOGIC TAB ==================

    public void OpenSkinTab()
    {
        if (carouselTabContent != null) carouselTabContent.SetActive(true);
        
        currentTab = ShopTab.Skins;
        currentIndex = 0;
        GenerateDots();
        UpdateDisplay();
    }

    public void OpenBoosterTab()
    {
        if (carouselTabContent != null) carouselTabContent.SetActive(true);
        
        currentTab = ShopTab.Boosters;
        currentIndex = 0;
        
        // Cập nhật giá lỡ có thay đổi trong Inspector
        if (boosterDatabase != null && BoosterManager.Instance != null)
        {
            boosterDatabase[0].price = BoosterManager.Instance.rerollCost;
            boosterDatabase[1].price = BoosterManager.Instance.trashCost;
            boosterDatabase[2].price = BoosterManager.Instance.swapCost;
            boosterDatabase[3].price = BoosterManager.Instance.hammerCost;
        }
        
        GenerateDots();
        UpdateDisplay();
    }

    // ================== LOGIC CAROUSEL ==================

    private int GetCurrentListCount()
    {
        if (currentTab == ShopTab.Skins) return skinDatabase != null ? skinDatabase.skins.Count : 0;
        else return boosterDatabase != null ? boosterDatabase.Count : 0;
    }

    public void NextItem()
    {
        int count = GetCurrentListCount();
        if (count == 0) return;
        
        currentIndex++;
        if (currentIndex >= count) currentIndex = 0; // Quay vòng lại đầu
        UpdateDisplay();
    }

    public void PreviousItem()
    {
        int count = GetCurrentListCount();
        if (count == 0) return;
        
        currentIndex--;
        if (currentIndex < 0) currentIndex = count - 1; // Quay vòng về cuối
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (GetCurrentListCount() == 0) return;

        string name = "";
        string iconPath = "";
        int price = 0;
        bool isOwned = false;
        bool isEquipped = false;

        if (currentTab == ShopTab.Skins)
        {
            SkinData currentSkin = skinDatabase.skins[currentIndex];
            name = currentSkin.name;
            iconPath = currentSkin.iconPath;
            price = currentSkin.price;
            
            if (DataManager.Instance != null)
            {
                isOwned = DataManager.Instance.playerData.PurchasedSkins.Contains(currentSkin.id) || currentSkin.price == 0;
                isEquipped = DataManager.Instance.playerData.EquippedPlateSkin == currentSkin.id;
            }
        }
        else // Boosters
        {
            BoosterData currentBooster = boosterDatabase[currentIndex];
            name = currentBooster.name;
            iconPath = currentBooster.iconPath;
            price = currentBooster.price;
            // Booster thì không có khái niệm Trang bị, mua là xài (tích trữ vào kho)
            isOwned = false; 
            isEquipped = false;
        }

        // Cập nhật Tên
        if (itemNameText != null) itemNameText.text = name;
        
        // Tải Icon 2D
        if (itemIcon != null)
        {
            // [DOTween] Hiệu ứng giật nhẹ (Punch Scale) khi chuyển ảnh
            itemIcon.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.2f, 5, 1);

            if (!string.IsNullOrEmpty(iconPath))
            {
                Sprite loadedSprite = Resources.Load<Sprite>(iconPath);
                if (loadedSprite != null)
                {
                    itemIcon.sprite = loadedSprite;
                    itemIcon.color = Color.white;
                }
                else
                {
                    itemIcon.color = Color.clear; // Ẩn hình nếu lỗi
                }
            }
            else
            {
                itemIcon.color = Color.clear; // Ẩn hình nếu không có icon
            }
        }

        // Cập nhật trạng thái Nút Mua/Trang Bị
        if (DataManager.Instance != null && buyButtonText != null && buyButton != null)
        {
            if (currentTab == ShopTab.Skins)
            {
                if (itemPriceText != null)
                {
                    itemPriceText.text = isOwned ? "SỞ HỮU" : price.ToString();
                }

                if (isEquipped)
                {
                    buyButtonText.text = "ĐANG DÙNG";
                    buyButton.interactable = false;
                }
                else if (isOwned)
                {
                    buyButtonText.text = "TRANG BỊ";
                    buyButton.interactable = true;
                }
                else
                {
                    buyButtonText.text = "MUA";
                    buyButton.interactable = DataManager.Instance.playerData.Gold >= price;
                }
            }
            else // Boosters
            {
                if (itemPriceText != null)
                {
                    itemPriceText.text = price.ToString();
                }
                buyButtonText.text = "MUA";
                buyButton.interactable = DataManager.Instance.playerData.Gold >= price;
            }
        }

        UpdateDots();
    }

    // ================== LOGIC CHẤM TRANG ==================

    private void GenerateDots()
    {
        if (dotPrefab == null || dotsContainer == null) return;
        
        int count = GetCurrentListCount();

        // Dọn dẹp chấm cũ
        foreach (Transform child in dotsContainer)
        {
            Destroy(child.gameObject);
        }
        paginationDots.Clear();

        // Sinh chấm mới
        for (int i = 0; i < count; i++)
        {
            GameObject dot = Instantiate(dotPrefab, dotsContainer);
            Image dotImage = dot.GetComponent<Image>();
            if (dotImage != null)
            {
                paginationDots.Add(dotImage);
            }
        }
    }

    private void UpdateDots()
    {
        for (int i = 0; i < paginationDots.Count; i++)
        {
            if (i == currentIndex)
            {
                paginationDots[i].color = dotActiveColor;
            }
            else
            {
                paginationDots[i].color = dotInactiveColor;
            }
        }
    }

    // ================== LOGIC NÚT MUA ==================

    private void OnBuyOrEquipClicked()
    {
        if (DataManager.Instance == null) return;

        if (currentTab == ShopTab.Skins)
        {
            SkinData currentSkin = skinDatabase.skins[currentIndex];
            bool isOwned = DataManager.Instance.playerData.PurchasedSkins.Contains(currentSkin.id) || currentSkin.price == 0;

            if (isOwned)
            {
                DataManager.Instance.EquipSkin(currentSkin.id);
                UpdateAllPlatesInScene();
            }
            else
            {
                bool success = DataManager.Instance.BuySkin(currentSkin.id, currentSkin.price);
                if (success)
                {
                    DataManager.Instance.EquipSkin(currentSkin.id);
                    UpdateAllPlatesInScene();
                }
            }
        }
        else // Boosters
        {
            BoosterData currentBooster = boosterDatabase[currentIndex];
            if (DataManager.Instance.playerData.Gold >= currentBooster.price)
            {
                DataManager.Instance.AddGold(-currentBooster.price);
                
                switch (currentBooster.type)
                {
                    case BoosterType.Hammer: DataManager.Instance.playerData.hammerCount++; break;
                    case BoosterType.Swap: DataManager.Instance.playerData.swapCount++; break;
                    case BoosterType.Trash: DataManager.Instance.playerData.trashCount++; break;
                    case BoosterType.Reroll: DataManager.Instance.playerData.rerollCount++; break;
                }
                
                DataManager.Instance.SaveData();
                Debug.Log($"[ShopManager] Đã mua thành công: {currentBooster.name}");
                
                // Gợi ý: Có thể gọi 1 Particle System "Ting!" bay lên từ nút mua ở đây
            }
            else
            {
                Debug.Log("[ShopManager] Không đủ Vàng để mua Booster!");
            }
        }

        UpdateDisplay();
    }

    private void UpdateAllPlatesInScene()
    {
        PizzaPlate[] allPlates = FindObjectsByType<PizzaPlate>(FindObjectsSortMode.None);
        foreach (var plate in allPlates)
        {
            plate.ApplySkin();
        }
    }

    public SkinData GetSkinData(string skinId)
    {
        if (skinDatabase == null) return null;
        return skinDatabase.skins.Find(s => s.id == skinId);
    }
}
