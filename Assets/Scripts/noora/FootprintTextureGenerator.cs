// Place this file anywhere in your project.
// In Unity: top menu → Tools → Generate Footprint Texture
// It creates Assets/FootprintMask.png — a white shoe sole on black background.
// Assign this texture to your footprint material's _MainTex slot.

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class FootprintTextureGenerator
{
    [MenuItem("Tools/Generate Footprint Texture")]
    static void Generate()
    {
        const int SIZE = 128;
        Texture2D tex = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);

        // Fill transparent black
        Color[] pixels = new Color[SIZE * SIZE];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.black;

        // Draw a simple shoe-sole shape (two overlapping ellipses: heel + toe)
        // Toe blob — wider, upper half of texture
        DrawEllipse(pixels, SIZE,
            centerX: 64, centerY: 78,
            rx: 30, ry: 22);

        // Heel blob — narrower, lower half
        DrawEllipse(pixels, SIZE,
            centerX: 64, centerY: 34,
            rx: 20, ry: 18);

        // Bridge connecting toe to heel
        DrawEllipse(pixels, SIZE,
            centerX: 64, centerY: 56,
            rx: 14, ry: 16);

        tex.SetPixels(pixels);
        tex.Apply();

        // Save to project
        string path = "Assets/FootprintMask.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);

        // Set import settings: alpha from greyscale, no compression artefacts
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 256;
            importer.SaveAndReimport();
        }

        Debug.Log("[FootprintTextureGenerator] Created Assets/FootprintMask.png");
        EditorUtility.DisplayDialog("Done",
            "Footprint texture created at Assets/FootprintMask.png\n\n" +
            "Assign it to your footprint material's Main Tex slot.", "OK");
    }

    static void DrawEllipse(Color[] pixels, int size, int centerX, int centerY, int rx, int ry)
    {
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - centerX) / (float)rx;
                float dy = (y - centerY) / (float)ry;
                float dist = dx * dx + dy * dy;

                if (dist <= 1.0f)
                {
                    // Soft edge: full white at centre, fade near edge
                    float brightness = Mathf.Clamp01(1.0f - dist * 0.6f);
                    // Keep brightest value if overlapping with previous ellipse
                    float existing = pixels[y * size + x].r;
                    float val = Mathf.Max(existing, brightness);
                    pixels[y * size + x] = new Color(val, val, val, val);
                }
            }
        }
    }
}
#endif