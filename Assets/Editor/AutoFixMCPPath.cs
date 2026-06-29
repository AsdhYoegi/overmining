using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AutoFixMCPPath
{
    static AutoFixMCPPath()
    {
        EditorPrefs.SetString("MCPForUnity.UvxPath", @"C:\Users\ringo\AppData\Local\Programs\Python\Python313\Scripts\uvx.exe");
        Debug.Log("✅ MCPForUnity.UvxPath has been automatically fixed to: uvx.exe");
    }
}
