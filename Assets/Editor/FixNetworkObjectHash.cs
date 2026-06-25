using UnityEngine;
using UnityEditor;
using Unity.Netcode;

public class FixNetworkObjectHash : Editor
{
    [MenuItem("Tools/Fix Network Hashes")]
    public static void FixHashes()
    {
        // ItemDrop
        GameObject itemDrop = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ItemDrop.prefab");
        if (itemDrop != null)
        {
            NetworkObject no = itemDrop.GetComponent<NetworkObject>();
            if (no != null) DestroyImmediate(no, true);
            itemDrop.AddComponent<NetworkObject>();
            EditorUtility.SetDirty(itemDrop);
            PrefabUtility.SavePrefabAsset(itemDrop);
            Debug.Log("Fixed ItemDrop Hash.");
        }

        // RockNode
        GameObject rockNode = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RockNode.prefab");
        if (rockNode != null)
        {
            NetworkObject no = rockNode.GetComponent<NetworkObject>();
            if (no != null) DestroyImmediate(no, true);
            rockNode.AddComponent<NetworkObject>();
            EditorUtility.SetDirty(rockNode);
            PrefabUtility.SavePrefabAsset(rockNode);
            Debug.Log("Fixed RockNode Hash.");
        }
        
        // MINE-01 (念のためこれも)
        GameObject mine01 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MINE-01.prefab");
        if (mine01 != null)
        {
            NetworkObject no = mine01.GetComponent<NetworkObject>();
            if (no != null) DestroyImmediate(no, true);
            mine01.AddComponent<NetworkObject>();
            EditorUtility.SetDirty(mine01);
            PrefabUtility.SavePrefabAsset(mine01);
            Debug.Log("Fixed MINE-01 Hash.");
        }

        Debug.Log("All Network hashes fixed and saved!");
    }
}
