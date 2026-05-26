using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEditor.SceneManagement;

public class GlobalFontSwapper : EditorWindow
{
    private Font targetLegacyFont;
    private TMP_FontAsset targetTMPFont;

    [MenuItem("Tools/Global Font Swapper")]
    public static void ShowWindow()
    {
        GetWindow<GlobalFontSwapper>("Font Swapper");
    }

    private void OnGUI()
    {
        GUILayout.Label("Global Multi-Scene Font Replacement Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetLegacyFont = (Font)EditorGUILayout.ObjectField("New Legacy Font (UI Text)", targetLegacyFont, typeof(Font), false);
        targetTMPFont = (TMP_FontAsset)EditorGUILayout.ObjectField("New TextMeshPro Font", targetTMPFont, typeof(TMP_FontAsset), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("Replace Fonts in ALL Build Scenes"))
        {
            if (EditorUtility.DisplayDialog("Confirm Font Overwrite", 
                "This will automatically cycle through, update, and save EVERY scene included in your Build Settings. Are you sure?", "Yes, Fix All Scenes", "Cancel"))
            {
                ReplaceFontsInAllScenes();
            }
        }
    }

    private void ReplaceFontsInAllScenes()
    {
        // 1. Save whatever scene the developer is currently working in right now
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[FONT SWAPPER] Operation cancelled by user.");
            return;
        }

        string originalScenePath = EditorSceneManager.GetActiveScene().path;
        int totalLegacyUpdated = 0;
        int totalTMPUpdated = 0;

        // 2. Look up all scenes added to your File -> Build Settings window
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;

        if (buildScenes.Length == 0)
        {
            EditorUtility.DisplayDialog("No Scenes Found", "There are no scenes added to your Build Settings to modify!", "OK");
            return;
        }

        foreach (EditorBuildSettingsScene buildScene in buildScenes)
        {
            if (!buildScene.enabled || string.IsNullOrEmpty(buildScene.path)) continue;

            // 3. Open the scene silently in the editor background
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(buildScene.path);
            if (sceneAsset == null) continue;

            EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            
            int currentSceneLegacy = 0;
            int currentSceneTMP = 0;

            // 4. Update Legacy Text Components in this scene
            if (targetLegacyFont != null)
            {
                Text[] legacyTexts = Resources.FindObjectsOfTypeAll<Text>();
                foreach (Text textComp in legacyTexts)
                {
                    if (!EditorUtility.IsPersistent(textComp.transform.root.gameObject))
                    {
                        textComp.font = targetLegacyFont;
                        EditorUtility.SetDirty(textComp);
                        currentSceneLegacy++;
                    }
                }
            }

            // 5. Update TextMeshPro Components in this scene
            if (targetTMPFont != null)
            {
                TMP_Text[] tmpTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
                foreach (TMP_Text tmpComp in tmpTexts)
                {
                    if (!EditorUtility.IsPersistent(tmpComp.transform.root.gameObject))
                    {
                        tmpComp.font = targetTMPFont;
                        EditorUtility.SetDirty(tmpComp);
                        currentSceneTMP++;
                    }
                }
            }

            // 6. Save the scene modifications before switching to the next one
            if (currentSceneLegacy > 0 || currentSceneTMP > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                
                totalLegacyUpdated += currentSceneLegacy;
                totalTMPUpdated += currentSceneTMP;
                
                Debug.Log($"[FONT SWAPPER] Updated {buildScene.path} (Legacy: {currentSceneLegacy}, TMP: {currentSceneTMP})");
            }
        }

        // 7. Return the developer cleanly back to the scene they started on
        if (!string.IsNullOrEmpty(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }

        AssetDatabase.SaveAssets();
        
        Debug.Log($"[GLOBAL COMPLETE] Successfully updated a grand total of {totalLegacyUpdated} Legacy and {totalTMPUpdated} TMP elements across your build pipeline!");
        EditorUtility.DisplayDialog("Global Success!", $"Processed all build scenes successfully.\n\nTotal Legacy UI Altered: {totalLegacyUpdated}\nTotal TMP UI Altered: {totalTMPUpdated}", "Awesome");
    }
}