using UnityEngine;

[CreateAssetMenu(fileName = "New Item Data", menuName = "MineRobot/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Info")]
    public string itemName = "New Item";
    [TextArea]
    public string description = "";
    
    [Header("Value")]
    public int sellPrice = 10;
    
    [Header("Visuals")]
    public GameObject prefab;
}
