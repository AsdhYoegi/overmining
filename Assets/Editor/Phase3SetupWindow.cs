using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class Phase3SetupWindow : EditorWindow
{
    [MenuItem("Tools/Setup Phase 3")]
    public static void Setup()
    {
        string log = "";

        // 1. ItemDataの作成
        string dataFolder = "Assets/Resources/Data";
        if (!AssetDatabase.IsValidFolder(dataFolder))
        {
            string[] folders = dataFolder.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                if (!AssetDatabase.IsValidFolder(currentPath + "/" + folders[i]))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath += "/" + folders[i];
            }
        }

        string assetPath = dataFolder + "/RockItemData.asset";
        ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        if (itemData == null)
        {
            itemData = ScriptableObject.CreateInstance<ItemData>();
            itemData.itemName = "Rock";
            itemData.sellPrice = 10;
            AssetDatabase.CreateAsset(itemData, assetPath);
            log += "Created RockItemData. ";
        }

        // 2. ItemDrop プレハブの更新
        string prefabPath = "Assets/Resources/ItemDrop.prefab";
        GameObject itemDropPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (itemDropPrefab != null)
        {
            ItemDrop id = itemDropPrefab.GetComponent<ItemDrop>();
            if (id != null)
            {
                id.itemData = itemData;
                EditorUtility.SetDirty(itemDropPrefab);
                log += "Assigned ItemData to ItemDrop prefab. ";
            }
        }

        // 3. GameManager の作成
        GameManager gm = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<NetworkObject>();
            gm = gmObj.AddComponent<GameManager>();
            log += "Created GameManager in scene. ";
        }

        // 4. UI (Canvas) の作成
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            log += "Created Canvas. ";
        }

        // EventSystem
        UnityEngine.EventSystems.EventSystem es = UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // 5. CycleUIManager の作成とセットアップ
        CycleUIManager uiManager = canvas.GetComponent<CycleUIManager>();
        if (uiManager == null)
        {
            uiManager = canvas.gameObject.AddComponent<CycleUIManager>();
        }

        // テキストUI生成ヘルパー
        TextMeshProUGUI CreateText(string name, Vector2 pos)
        {
            Transform existing = canvas.transform.Find(name);
            if (existing != null) return existing.GetComponent<TextMeshProUGUI>();

            GameObject txtObj = new GameObject(name);
            txtObj.transform.SetParent(canvas.transform, false);
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = name;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            
            RectTransform rt = txtObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300, 40);
            
            return tmp;
        }

        uiManager.phaseText = CreateText("PhaseText", new Vector2(20, -20));
        uiManager.timeText = CreateText("TimeText", new Vector2(20, -60));
        uiManager.moneyText = CreateText("MoneyText", new Vector2(20, -100));
        uiManager.quotaText = CreateText("QuotaText", new Vector2(20, -140));
        uiManager.levelText = CreateText("LevelText", new Vector2(20, -180));

        // 6. ShopPanel
        Transform shopPanelTr = canvas.transform.Find("ShopPanel");
        GameObject shopPanel = null;
        if (shopPanelTr != null)
        {
            shopPanel = shopPanelTr.gameObject;
        }
        else
        {
            shopPanel = new GameObject("ShopPanel");
            shopPanel.transform.SetParent(canvas.transform, false);
            Image img = shopPanel.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.8f);
            RectTransform rt = shopPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            // Text in shop panel
            GameObject shopTitle = new GameObject("Title");
            shopTitle.transform.SetParent(shopPanel.transform, false);
            TextMeshProUGUI titleTxt = shopTitle.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "SHOP PHASE (Select items or proceed)";
            titleTxt.fontSize = 40;
            titleTxt.alignment = TextAlignmentOptions.Center;
            RectTransform trt = shopTitle.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0, 100);
            trt.sizeDelta = new Vector2(600, 100);
            
            shopPanel.SetActive(false);
            log += "Created ShopPanel. ";
        }
        uiManager.shopPanel = shopPanel;

        // 7. NextPhaseButton
        Transform nextBtnTr = canvas.transform.Find("NextPhaseButton");
        Button nextBtn = null;
        if (nextBtnTr != null)
        {
            nextBtn = nextBtnTr.GetComponent<Button>();
        }
        else
        {
            GameObject btnObj = new GameObject("NextPhaseButton");
            btnObj.transform.SetParent(canvas.transform, false);
            Image bg = btnObj.AddComponent<Image>();
            bg.color = Color.green;
            nextBtn = btnObj.AddComponent<Button>();
            
            RectTransform brt = btnObj.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0);
            brt.anchorMax = new Vector2(0.5f, 0);
            brt.pivot = new Vector2(0.5f, 0);
            brt.anchoredPosition = new Vector2(0, 50);
            brt.sizeDelta = new Vector2(200, 60);
            
            GameObject btnTxtObj = new GameObject("Text");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI btxt = btnTxtObj.AddComponent<TextMeshProUGUI>();
            btxt.text = "Next Phase";
            btxt.color = Color.black;
            btxt.fontSize = 24;
            btxt.alignment = TextAlignmentOptions.Center;
            RectTransform btrt = btnTxtObj.GetComponent<RectTransform>();
            btrt.anchorMin = Vector2.zero;
            btrt.anchorMax = Vector2.one;
            btrt.offsetMin = Vector2.zero;
            btrt.offsetMax = Vector2.zero;
            
            log += "Created NextPhaseButton. ";
        }
        uiManager.nextPhaseButton = nextBtn;

        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Debug.Log("Setup Phase 3 Complete: " + log);
    }
}
