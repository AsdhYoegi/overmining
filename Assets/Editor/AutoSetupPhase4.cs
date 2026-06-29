using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class AutoSetupPhase4 : EditorWindow
{
    [MenuItem("Tools/Setup Phase 4")]
    public static void DoSetup()
    {
        string log = "";

        // 1. Create Materials
        var validMatPath = "Assets/Resources/PreviewValidMat.mat";
        var validMat = AssetDatabase.LoadAssetAtPath<Material>(validMatPath);
        if (validMat == null) {
            validMat = new Material(Shader.Find("Standard"));
            validMat.color = new Color(0, 1, 0, 0.5f);
            validMat.SetFloat("_Mode", 3); // Transparent
            validMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            validMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            validMat.SetInt("_ZWrite", 0);
            validMat.DisableKeyword("_ALPHATEST_ON");
            validMat.EnableKeyword("_ALPHABLEND_ON");
            validMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            validMat.renderQueue = 3000;
            AssetDatabase.CreateAsset(validMat, validMatPath);
        }

        var invalidMatPath = "Assets/Resources/PreviewInvalidMat.mat";
        var invalidMat = AssetDatabase.LoadAssetAtPath<Material>(invalidMatPath);
        if (invalidMat == null) {
            invalidMat = new Material(Shader.Find("Standard"));
            invalidMat.color = new Color(1, 0, 0, 0.5f);
            invalidMat.SetFloat("_Mode", 3); // Transparent
            invalidMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            invalidMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            invalidMat.SetInt("_ZWrite", 0);
            invalidMat.DisableKeyword("_ALPHATEST_ON");
            invalidMat.EnableKeyword("_ALPHABLEND_ON");
            invalidMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            invalidMat.renderQueue = 3000;
            AssetDatabase.CreateAsset(invalidMat, invalidMatPath);
        }
        log += "Created preview materials. ";

        // 2. Create DisplayShelf Prefab
        var shelfPrefabPath = "Assets/Resources/DisplayShelf.prefab";
        var shelfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shelfPrefabPath);
        if (shelfPrefab == null) {
            var existingShelf = GameObject.Find("DisplayShelf");
            if (existingShelf != null) {
                shelfPrefab = PrefabUtility.SaveAsPrefabAsset(existingShelf, shelfPrefabPath);
                log += "Created DisplayShelf Prefab. ";
            }
        }

        var rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TestRock_New.prefab");

        // 3. Update Player Prefab
        var playerPrefabPath = "Assets/Prefabs/Player.prefab"; 
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
        if (playerPrefab == null) playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Player.prefab");

        if (playerPrefab != null) {
            var placement = playerPrefab.GetComponent<PlayerPlacement>();
            if (placement == null) placement = playerPrefab.AddComponent<PlayerPlacement>();
            placement.rockPrefab = rockPrefab;
            placement.shelfPrefab = shelfPrefab;
            placement.previewValidMat = validMat;
            placement.previewInvalidMat = invalidMat;
            EditorUtility.SetDirty(playerPrefab);
            PrefabUtility.SavePrefabAsset(playerPrefab);
            log += "Updated Player Prefab. ";
        }

        // 4. Update UI
        var uiManager = Object.FindFirstObjectByType<CycleUIManager>();
        if (uiManager != null) {
            var shopPanel = uiManager.shopPanel;
            if (shopPanel != null) {
                // Create Buy Buttons
                var buyRockBtnTr = shopPanel.transform.Find("BuyRockBtn");
                if (buyRockBtnTr == null) {
                    var go = new GameObject("BuyRockBtn");
                    go.transform.SetParent(shopPanel.transform, false);
                    var img = go.AddComponent<Image>();
                    img.color = Color.gray;
                    var btn = go.AddComponent<Button>();
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(-150, -50);
                    rt.sizeDelta = new Vector2(200, 60);
                    
                    var txt = new GameObject("Text");
                    txt.transform.SetParent(go.transform, false);
                    var tmp = txt.AddComponent<TextMeshProUGUI>();
                    tmp.text = "Buy Rock (10G)";
                    tmp.color = Color.white;
                    tmp.alignment = TextAlignmentOptions.Center;
                    var trt = txt.GetComponent<RectTransform>();
                    trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                    trt.sizeDelta = Vector2.zero;
                    
                    uiManager.buyRockButton = btn;
                    log += "Created Buy Rock UI. ";
                }
                
                var buyShelfBtnTr = shopPanel.transform.Find("BuyShelfBtn");
                if (buyShelfBtnTr == null) {
                    var go = new GameObject("BuyShelfBtn");
                    go.transform.SetParent(shopPanel.transform, false);
                    var img = go.AddComponent<Image>();
                    img.color = Color.gray;
                    var btn = go.AddComponent<Button>();
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(150, -50);
                    rt.sizeDelta = new Vector2(200, 60);
                    
                    var txt = new GameObject("Text");
                    txt.transform.SetParent(go.transform, false);
                    var tmp = txt.AddComponent<TextMeshProUGUI>();
                    tmp.text = "Buy Shelf (10G)";
                    tmp.color = Color.white;
                    tmp.alignment = TextAlignmentOptions.Center;
                    var trt = txt.GetComponent<RectTransform>();
                    trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                    trt.sizeDelta = Vector2.zero;
                    
                    uiManager.buyShelfButton = btn;
                    log += "Created Buy Shelf UI. ";
                }
                
                var invTextTr = shopPanel.transform.parent.Find("InventoryText");
                if (invTextTr == null) {
                    var go = new GameObject("InventoryText");
                    go.transform.SetParent(shopPanel.transform.parent, false);
                    var tmp = go.AddComponent<TextMeshProUGUI>();
                    tmp.text = "Inventory";
                    tmp.fontSize = 24;
                    tmp.alignment = TextAlignmentOptions.TopRight;
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(1, 1);
                    rt.anchoredPosition = new Vector2(-20, -20);
                    rt.sizeDelta = new Vector2(300, 100);
                    
                    uiManager.inventoryText = tmp;
                    log += "Created Inventory Text UI. ";
                }
            }
        }

        // 5. Add to NetworkManager
        var nm = Object.FindFirstObjectByType<Unity.Netcode.NetworkManager>();
        if (nm != null && shelfPrefab != null) {
            bool found = false;
            foreach (var p in nm.NetworkConfig.Prefabs.NetworkPrefabsLists[0].PrefabList) {
                if (p.Prefab == shelfPrefab) found = true;
            }
            if (!found) {
                nm.NetworkConfig.Prefabs.Add(new Unity.Netcode.NetworkPrefab { Prefab = shelfPrefab });
                log += "Added Shelf to NetworkPrefabs. ";
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("Setup Phase 4 Complete: " + log);
    }
}
