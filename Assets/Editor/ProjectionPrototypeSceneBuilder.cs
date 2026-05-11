using System.Linq;
using Meta.XR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ProjectionPrototypeSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/ProjectionPrototype.unity";
    private const string CameraRigPath = "Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab";
    private const string MrukPath = "Packages/com.meta.xr.mrutilitykit/Core/Tools/MRUK.prefab";
    private const string EffectMeshPath = "Packages/com.meta.xr.mrutilitykit/Core/Tools/EffectMesh.prefab";
    private const string SceneDebuggerPath = "Packages/com.meta.xr.mrutilitykit/Core/Tools/ImmersiveSceneDebugger.prefab";

    [MenuItem("Tools/ShrunkMe/Create Projection Prototype Scene")]
    public static void CreateProjectionPrototypeScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "ProjectionPrototype";

        var cameraRig = InstantiatePrefab(CameraRigPath, "Meta XR Camera Rig");
        cameraRig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        ConfigureOvrManager(cameraRig);

        InstantiatePrefab(MrukPath, "MRUK");
        InstantiatePrefab(EffectMeshPath, "MRUK EffectMesh");
        InstantiatePrefab(SceneDebuggerPath, "MRUK Scene Debugger");

        CreatePassthroughCameraAccess();
        CreateDirectionalLight();
        CreateDebugUiCanvas();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Created {ScenePath}");
    }

    private static GameObject InstantiatePrefab(string assetPath, string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            throw new System.InvalidOperationException($"Missing prefab at {assetPath}");
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        return instance;
    }

    private static void ConfigureOvrManager(GameObject cameraRig)
    {
        var ovrManager = cameraRig.GetComponentInChildren<OVRManager>(true);
        if (ovrManager == null)
        {
            return;
        }

        var serialized = new SerializedObject(ovrManager);
        var requestCameraPermission = serialized.FindProperty("requestPassthroughCameraAccessPermissionOnStartup");
        if (requestCameraPermission != null)
        {
            requestCameraPermission.boolValue = true;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(ovrManager);
    }

    private static void CreatePassthroughCameraAccess()
    {
        var go = new GameObject("PassthroughCameraAccess");
        go.AddComponent<PassthroughCameraAccess>();
    }

    private static void CreateDirectionalLight()
    {
        var go = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        light.color = new Color(1f, 0.95686275f, 0.8392157f, 1f);
        go.transform.SetPositionAndRotation(new Vector3(0f, 3f, 0f), Quaternion.Euler(50f, -30f, 0f));
    }

    private static void CreateDebugUiCanvas()
    {
        var canvasGo = new GameObject("Debug UI Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.transform.SetPositionAndRotation(new Vector3(0f, 1.55f, 2f), Quaternion.Euler(0f, 180f, 0f));
        canvasGo.transform.localScale = Vector3.one * 0.0025f;

        var rect = canvasGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900f, 420f);

        var panelGo = new GameObject("Debug Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0.02f, 0.02f, 0.02f, 0.72f);

        var textGo = new GameObject("Status Text");
        textGo.transform.SetParent(panelGo.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(32f, 28f);
        textRect.offsetMax = new Vector2(-32f, -28f);
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 36;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
        text.text = "Projection Prototype\nMRUK scene data + passthrough camera access\nReady for Quest runtime permission checks";

        if (Object.FindObjectsOfType<EventSystem>().Length == 0)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }
}
