using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class GoldUIUpdater : MonoBehaviour
{
    private TextMeshProUGUI textComponent;
    private int lastGold = -1;

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (DataManager.Instance != null)
        {
            int currentGold = DataManager.Instance.playerData.Gold;
            if (currentGold != lastGold)
            {
                lastGold = currentGold;
                textComponent.text = lastGold.ToString();
            }
        }
    }
}
