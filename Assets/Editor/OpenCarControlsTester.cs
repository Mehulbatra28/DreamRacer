using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;

public static class TestInputActions
{
    [MenuItem("Tools/Test CarControls.inputactions")]
    public static void TestLoad()
    {
        string path = "Assets/CarControls.inputactions";
        try
        {
            string json = File.ReadAllText(path);
            var asset = InputActionAsset.FromJson(json);
            if (asset == null)
            {
                Debug.LogError("FromJson returned null!");
            }
            else
            {
                Debug.Log($"Successfully loaded asset with {asset.actionMaps.Count} maps. Trying to open window...");
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Exception loading input actions: " + e.ToString());
        }
    }
}
