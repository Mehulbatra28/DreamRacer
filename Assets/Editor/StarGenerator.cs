using UnityEngine;
using UnityEditor;
using System.IO;

public class StarGenerator : EditorWindow
{
    [MenuItem("DreamRacer/Generate Star Map")]
    public static void GenerateStarMap()
    {
        int width = 4096;
        int height = 2048;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.black;
        }

        // Generate a dense, realistic starfield
        int numStars = 40000;
        for (int i = 0; i < numStars; i++)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            // Natural star colors: mostly white, some pale blue, some pale orange
            float r = 1f, g = 1f, b = 1f;
            float colorType = Random.value;
            if (colorType > 0.8f) { r = 0.8f; g = 0.9f; b = 1f; } // Blueish
            else if (colorType > 0.6f) { r = 1f; g = 0.9f; b = 0.8f; } // Yellowish

            // Exponential distribution for brightness (many dim stars, few bright ones)
            float intensity = Mathf.Pow(Random.value, 4f); 
            
            // Only a tiny fraction of stars get a boost
            if (Random.value > 0.99f) intensity *= 2f;

            // Strict 1-pixel stars so they look like pinpricks, not lightbulbs
            pixels[y * width + x] = new Color(r * intensity, g * intensity, b * intensity, 1f);
        }

        texture.SetPixels(pixels);
        texture.Apply();

        byte[] bytes = texture.EncodeToPNG();
        
        string dirPath = Application.dataPath + "/Textures";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        string filePath = dirPath + "/ProceduralStarMap_Crisp.png";
        File.WriteAllBytes(filePath, bytes);
        
        AssetDatabase.Refresh();

        // Automatically configure the texture in Unity for perfect crisp stars
        string assetPath = "Assets/Textures/ProceduralStarMap_Crisp.png";
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (importer != null)
        {
            importer.textureShape = TextureImporterShape.TextureCube;
            importer.filterMode = FilterMode.Point; // Forces sharp pixels instead of blurry blobs
            importer.textureCompression = TextureImporterCompression.Uncompressed; // Prevents compression artifacts
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }
        
        Debug.Log("Star Map Generated successfully at: " + filePath);
        EditorUtility.DisplayDialog("Success", "Crisp Star Map generated!\n\nTo use it:\n1. Open your Global Volume.\n2. Go to Physically Based Sky -> Space Emission Texture.\n3. Drag the new 'ProceduralStarMap_Crisp' texture from the Textures folder into the slot!", "OK");
    }
}
