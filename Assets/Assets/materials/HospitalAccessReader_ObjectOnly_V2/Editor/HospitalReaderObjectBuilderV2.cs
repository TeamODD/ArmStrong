#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class HospitalReaderObjectBuilderV2
{
    private const string OUTPUT = "Assets/HospitalReaderObjectGenerated";

    [MenuItem("Tools/Hospital Horror/Create Pretty Access Reader Object")]
    public static void Build()
    {
        EnsureFolder(OUTPUT);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Texture2D metalTex = FindTexture("reader_worn_metal");
        Texture2D overlayTex = FindTexture("reader_panel_overlay");

        Material matMetal = CreateMat("MAT_Metal", shader, new Color(0.50f,0.51f,0.52f), 0.62f, 0.22f, metalTex);
        Material matDark = CreateMat("MAT_Dark", shader, new Color(0.04f,0.045f,0.05f), 0.05f, 0.18f, null);
        Material matButton = CreateMat("MAT_Button", shader, new Color(0.11f,0.115f,0.12f), 0.02f, 0.17f, null);
        Material matScreen = CreateMat("MAT_Screen", shader, new Color(0.02f,0.05f,0.05f), 0.0f, 0.10f, null);
        Material matGreen = CreateMat("MAT_GreenLED", shader, new Color(0.10f,0.85f,0.15f), 0.0f, 0.20f, null);
        Material matRed = CreateMat("MAT_RedLED", shader, new Color(0.75f,0.08f,0.08f), 0.0f, 0.20f, null);
        Material matOverlay = CreateMat("MAT_Overlay", shader, Color.white, 0.0f, 0.08f, overlayTex);

        GameObject root = new GameObject("Hospital_Access_Reader_Object");
        root.transform.position = Vector3.zero;

        // backplate, top, bottom
        MakeCube("BackPlate", root.transform, new Vector3(0,0,0), new Vector3(0.74f,1.00f,0.06f), matMetal, false);
        MakeCube("TopPlate", root.transform, new Vector3(0,0.57f,-0.005f), new Vector3(0.68f,0.18f,0.05f), matMetal, false);
        MakeCube("BottomPlate", root.transform, new Vector3(0,-0.56f,-0.005f), new Vector3(0.68f,0.14f,0.05f), matMetal, false);

        // front housing
        MakeCube("FrontHousing", root.transform, new Vector3(0,-0.01f,0.045f), new Vector3(0.67f,0.89f,0.09f), matMetal, false);

        // decorative front overlay panel as a thin plane with texture
        GameObject overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
        overlay.name = "FrontOverlayDecal";
        overlay.transform.SetParent(root.transform, false);
        overlay.transform.localPosition = new Vector3(0.0f, 0.01f, 0.093f);
        overlay.transform.localRotation = Quaternion.identity;
        overlay.transform.localScale = new Vector3(0.67f, 0.89f, 1f);
        overlay.GetComponent<Renderer>().sharedMaterial = matOverlay;
        Object.DestroyImmediate(overlay.GetComponent<Collider>());

        // screen bezel frame
        MakeCube("ScreenBezel", root.transform, new Vector3(-0.13f,0.22f,0.092f), new Vector3(0.40f,0.24f,0.024f), matDark, false);
        MakeCube("ScreenInset", root.transform, new Vector3(-0.13f,0.22f,0.103f), new Vector3(0.35f,0.19f,0.008f), matScreen, false);

        // right card slot housing
        MakeCube("CardSlotHousing", root.transform, new Vector3(0.18f,-0.12f,0.095f), new Vector3(0.16f,0.36f,0.032f), matDark, true);
        MakeCube("CardInsertSlit", root.transform, new Vector3(0.18f,-0.10f,0.110f), new Vector3(0.024f,0.18f,0.006f), matButton, true);

        // keypad panel base
        MakeCube("KeypadPanel", root.transform, new Vector3(-0.16f,-0.22f,0.092f), new Vector3(0.29f,0.38f,0.02f), matDark, false);

        // separate keypad buttons, named for future scripting
        string[] keys = new string[] {"1","2","3","4","5","6","7","8","9","*","0","#"};
        float x0 = -0.25f; float y0 = -0.09f; float dx = 0.08f; float dy = 0.08f;
        for (int i=0; i<keys.Length; i++)
        {
            int row = i / 3;
            int col = i % 3;
            Vector3 pos = new Vector3(x0 + col*dx, y0 - row*dy, 0.108f);
            GameObject key = MakeCube("Key_" + Sanitize(keys[i]), root.transform, pos, new Vector3(0.055f,0.055f,0.02f), matButton, true);

            // small raised face
            MakeCube("Cap", key.transform, Vector3.forward * 0.004f, new Vector3(0.044f,0.044f,0.006f), matDark, false);

            // number label as child plane for easier replacement later
            GameObject label = GameObject.CreatePrimitive(PrimitiveType.Quad);
            label.name = "Label";
            label.transform.SetParent(key.transform, false);
            label.transform.localPosition = new Vector3(0,0,0.0135f);
            label.transform.localScale = new Vector3(0.030f,0.030f,1f);
            Object.DestroyImmediate(label.GetComponent<Collider>());
        }

        // LEDs separate
        MakeCube("LED_Access", root.transform, new Vector3(0.25f,0.28f,0.109f), new Vector3(0.035f,0.018f,0.008f), matGreen, true);
        MakeCube("LED_Denied", root.transform, new Vector3(0.25f,0.235f,0.109f), new Vector3(0.035f,0.018f,0.008f), matRed, true);

        // screws
        Vector3[] screwPos = new Vector3[] {
            new Vector3(-0.31f,0.41f,0.092f), new Vector3(0.31f,0.41f,0.092f),
            new Vector3(-0.31f,-0.41f,0.092f), new Vector3(0.31f,-0.41f,0.092f),
            new Vector3(-0.29f,0.57f,0.022f), new Vector3(0.29f,0.57f,0.022f),
            new Vector3(-0.29f,-0.56f,0.022f), new Vector3(0.29f,-0.56f,0.022f)
        };
        foreach (Vector3 sp in screwPos)
            MakeCylinder("Screw", root.transform, sp, new Vector3(0.018f,0.005f,0.018f), matDark);

        // hook object for future script attachment
        GameObject hooks = new GameObject("_HOOKS");
        hooks.transform.SetParent(root.transform, false);
        CreateEmpty(hooks.transform, "HOOK_ScreenText", new Vector3(-0.13f,0.22f,0.110f));
        CreateEmpty(hooks.transform, "HOOK_KeypadRoot", new Vector3(-0.16f,-0.22f,0.108f));
        CreateEmpty(hooks.transform, "HOOK_CardSlot", new Vector3(0.18f,-0.10f,0.110f));
        CreateEmpty(hooks.transform, "HOOK_CardInsertTarget", new Vector3(0.18f,-0.14f,0.090f));

        string prefabPath = OUTPUT + "/Hospital_Access_Reader_Object.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("Created pretty Hospital Access Reader object prefab. This is object-only (no gameplay script).");
    }

    static string Sanitize(string s)
    {
        if (s == "*") return "Star";
        if (s == "#") return "Hash";
        return s;
    }

    static void CreateEmpty(Transform parent, string name, Vector3 localPos)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
    }

    static GameObject MakeCube(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, bool keepCollider)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        if (parent != null) g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localScale = localScale;
        g.GetComponent<Renderer>().sharedMaterial = mat;

        if (!keepCollider)
        {
            Collider c = g.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
        return g;
    }

    static GameObject MakeCylinder(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localRotation = Quaternion.Euler(90f,0f,0f);
        g.transform.localScale = localScale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        Collider c = g.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
        return g;
    }

    static Material CreateMat(string name, Shader shader, Color color, float metallic, float smoothness, Texture2D tex)
    {
        string path = OUTPUT + "/" + name + ".mat";
        Material exist = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (exist != null) return exist;

        Material m = new Material(shader);
        m.name = name;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        if (tex != null)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        }
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static Texture2D FindTexture(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:Texture2D");
        if (guids.Length == 0) return null;
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string cur = parts[0];
        for (int i=1; i<parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
#endif
