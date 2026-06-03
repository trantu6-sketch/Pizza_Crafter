using UnityEngine;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.U2D;

public class OptimizationTools : EditorWindow
{
    [MenuItem("Pizza Crafter/Optimize/1. Pack UI Sprite Atlas (Giảm Draw Call)")]
    public static void CreateSpriteAtlas()
    {
        string atlasPath = "Assets/Resources/MainUIAtlas.spriteatlas";
        
        // Tạo file Atlas mới
        SpriteAtlas atlas = new SpriteAtlas();
        
        SpriteAtlasPackingSettings packingSettings = new SpriteAtlasPackingSettings()
        {
            blockOffset = 1,
            enableRotation = false,
            enableTightPacking = false,
            padding = 2,
        };
        atlas.SetPackingSettings(packingSettings);

        SpriteAtlasTextureSettings textureSettings = new SpriteAtlasTextureSettings()
        {
            readable = false,
            generateMipMaps = false,
            sRGB = true,
            filterMode = FilterMode.Bilinear,
        };
        atlas.SetTextureSettings(textureSettings);

        // Khai báo các thư mục chứa ảnh muốn gom lại
        string[] foldersToPack = new string[] {
            "Assets/Textures/UI_Game",
            "Assets/Resources/Icons",
            "Assets/Resources/Boosters"
        };

        foreach (string folderPath in foldersToPack)
        {
            // Tìm tất cả các file ảnh đã được định dạng là Sprite trong thư mục
            string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new string[] { folderPath });
            foreach (string guid in spriteGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (spr != null)
                {
                    SpriteAtlasExtensions.Add(atlas, new Object[] { spr });
                }
            }
        }

        // Lưu Atlas thành file trong Project
        AssetDatabase.CreateAsset(atlas, atlasPath);
        
        // Ép Unity đóng gói Atlas ngay lập tức
        SpriteAtlasUtility.PackAtlases(new SpriteAtlas[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
        AssetDatabase.SaveAssets();

        Debug.Log("[Optimization] Đã tạo thành công Sprite Atlas tại: " + atlasPath);
        EditorUtility.DisplayDialog("Tối Ưu Hóa", "Đã tạo Sprite Atlas thành công! (Giảm Draw Calls UI)", "Tuyệt vời");
    }

    [MenuItem("Pizza Crafter/Optimize/2. Bật GPU Instancing (Tối ưu render 3D)")]
    public static void EnableGPUInstancing()
    {
        // Tìm TẤT CẢ Material trong toàn bộ dự án
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            if (mat != null && !mat.enableInstancing)
            {
                // Loại trừ các Material của UI hoặc TextMeshPro vì UI không dùng Instancing
                if (!path.Contains("TextMesh Pro") && !path.Contains("UI") && !path.Contains("Hierarchy Designer"))
                {
                    mat.enableInstancing = true;
                    count++;
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"[Optimization] Đã tự động bật GPU Instancing cho {count} Materials.");
        EditorUtility.DisplayDialog("Tối Ưu Hóa", $"Đã bật GPU Instancing cho {count} Materials 3D!\n\n(Chống lag khi có hàng trăm lát pizza trên màn hình)", "Đã Hiểu");
    }
}
