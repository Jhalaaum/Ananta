using UnityEngine;
using UnityEditor;
using System.IO;

public class MarmosetUnityTool : EditorWindow
{
    // Mask Map Variables
    Texture2D metallicTex;
    Texture2D occlusionTex;
    Texture2D roughnessTex;

    // Albedo Alpha Variables
    Texture2D albedoTex;
    Texture2D opacityTex;

    [MenuItem("Tools/Marmoset Unity Utility")]
    public static void ShowWindow()
    {
        GetWindow<MarmosetUnityTool>("Marmoset -> Unity");
    }

    void OnGUI()
    {
        // --- SECTION 1: MASK MAP ---
        GUILayout.Label("1. Pack Mask Map (Metallic/AO/Smoothness)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("R: Metallic | G: Occlusion | B: Empty | A: Inverted Roughness", MessageType.None);

        metallicTex = (Texture2D)EditorGUILayout.ObjectField("Metallic (R)", metallicTex, typeof(Texture2D), false);
        occlusionTex = (Texture2D)EditorGUILayout.ObjectField("Occlusion (G)", occlusionTex, typeof(Texture2D), false);
        roughnessTex = (Texture2D)EditorGUILayout.ObjectField("Roughness (A Invert)", roughnessTex, typeof(Texture2D), false);

        if (GUILayout.Button("Generate Mask Map"))
        {
            PackMaskMap();
        }

        EditorGUILayout.Space(20);
        Handles.DrawLine(new Vector3(0, GUILayoutUtility.GetLastRect().y + 25), new Vector3(position.width, GUILayoutUtility.GetLastRect().y + 25));
        EditorGUILayout.Space(10);

        // --- SECTION 2: ALBEDO ALPHA ---
        GUILayout.Label("2. Pack Albedo + Transparency", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Combines Color (RGB) and Opacity (A) for Cutout/Transparent shaders.", MessageType.None);

        albedoTex = (Texture2D)EditorGUILayout.ObjectField("Albedo (RGB)", albedoTex, typeof(Texture2D), false);
        opacityTex = (Texture2D)EditorGUILayout.ObjectField("Opacity (A)", opacityTex, typeof(Texture2D), false);

        if (GUILayout.Button("Generate Albedo Alpha"))
        {
            PackAlbedoAlpha();
        }
    }

    void PackMaskMap()
    {
        if (roughnessTex == null) { Debug.LogError("Roughness map is required for Smoothness calculation."); return; }

        ExecutePacking("MaskMap", (x, y) => {
            float r = metallicTex != null ? metallicTex.GetPixel(x, y).r : 0;
            float g = occlusionTex != null ? occlusionTex.GetPixel(x, y).r : 1;
            float b = 0;
            float a = 1.0f - roughnessTex.GetPixel(x, y).r; // Smoothness
            return new Color(r, g, b, a);
        }, roughnessTex.width, roughnessTex.height, new Texture2D[] { metallicTex, occlusionTex, roughnessTex });
    }

    void PackAlbedoAlpha()
    {
        if (albedoTex == null || opacityTex == null) { Debug.LogError("Both Albedo and Opacity are required."); return; }

        ExecutePacking("AlbedoAlpha", (x, y) => {
            Color alb = albedoTex.GetPixel(x, y);
            float alpha = opacityTex.GetPixel(x, y).r;
            return new Color(alb.r, alb.g, alb.b, alpha);
        }, albedoTex.width, albedoTex.height, new Texture2D[] { albedoTex, opacityTex });
    }

    // Shared execution logic to keep code clean
    void ExecutePacking(string defaultName, System.Func<int, int, Color> colorLogic, int w, int h, Texture2D[] sources)
    {
        foreach (var tex in sources) MakeTextureReadable(tex);

        Texture2D result = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                result.SetPixel(x, y, colorLogic(x, y));
            }
        }
        result.Apply();

        string path = EditorUtility.SaveFilePanelInProject("Save Texture", defaultName, "png", "Save texture");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllBytes(path, result.EncodeToPNG());
            AssetDatabase.Refresh();
            Debug.Log("Successfully saved: " + path);
        }
    }

    void MakeTextureReadable(Texture2D tex)
    {
        if (tex == null) return;
        string path = AssetDatabase.GetAssetPath(tex);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    }
}