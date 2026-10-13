using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;

// All scenery and characters are real meshes, generated locally without asset downloads.
public sealed class LuyeWorld : MonoBehaviour
{
    readonly List<Material> materials = new List<Material>();
    readonly List<Mesh> sculptedMeshes = new List<Mesh>();
    readonly List<Texture2D> fabricTextures = new List<Texture2D>();
    readonly List<Transform> visitors = new List<Transform>();
    readonly List<VisitorState> visitorStates = new List<VisitorState>();
    readonly List<Transform> stageLights = new List<Transform>();
    Transform balloon, traveler, flame;
    readonly Color ink = new Color(.13f, .20f, .19f);
    Material skin, hair, cream, teal, denim, leather, dark;
    Camera view;
    int cameraMode;
    public int CameraMode => cameraMode;
    public void SetCameraMode(int mode) { cameraMode = Mathf.Clamp(mode, 0, 2); }

    void Update()
    {
        var keys = Keyboard.current;
        if (keys == null) return;
        if (keys.cKey.wasPressedThisFrame) cameraMode = (cameraMode + 1) % 3;
        if (keys.digit1Key.wasPressedThisFrame) SetCameraMode(0);
        if (keys.digit2Key.wasPressedThisFrame) SetCameraMode(1);
        if (keys.digit3Key.wasPressedThisFrame) SetCameraMode(2);
    }

    Material Paint(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var m = new Material(shader);
        m.color = color;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .12f);
        materials.Add(m);
        return m;
    }

    Material Fabric(Color color)
    {
        var material = Paint(color);
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, true);
        texture.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
        {
            float weave = .90f + .055f * Mathf.PerlinNoise(x * .43f, y * .43f)
                + ((x % 3 == 0 || y % 3 == 0) ? .045f : 0);
            texture.SetPixel(x, y, new Color(weave, weave, weave, 1));
        }
        texture.Apply(); material.mainTexture = texture; fabricTextures.Add(texture);
        return material;
    }

    Transform Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        var collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        return go.transform;
    }

    Transform Group(string name, Transform parent, Vector3 position)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false); t.localPosition = position;
        return t;
    }

    void Rod(string name, Transform parent, Vector3 a, Vector3 b, float radius, Material material)
    {
        var t = Shape(name, PrimitiveType.Cylinder, parent, (a + b) * .5f,
            new Vector3(radius, Vector3.Distance(a, b) * .5f, radius), material);
        t.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
    }

    public void Build()
    {
        dark = Paint(ink); skin = Paint(new Color(.88f, .65f, .46f));
        hair = Paint(new Color(.19f, .12f, .09f)); cream = Paint(new Color(.94f, .88f, .71f));
        teal = Fabric(new Color(.27f, .35f, .30f)); denim = Fabric(new Color(.21f, .27f, .33f));
        leather = Fabric(new Color(.38f, .28f, .20f));
        view = Camera.main;
        if (view == null) view = new GameObject("Travel Camera").AddComponent<Camera>();
        view.orthographic = true; view.orthographicSize = 12.2f;
        view.transform.position = new Vector3(10, 17, -27);
        view.transform.LookAt(new Vector3(0, 7, 3));
        view.clearFlags = CameraClearFlags.SolidColor;
        view.allowHDR = false;
        view.backgroundColor = new Color(.68f, .81f, .80f);
        view.nearClipPlane = .1f; view.farClipPlane = 200;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.74f, .77f, .69f);
        RenderSettings.fog = false;
        // The template's global post processing is unnecessary for the clean festival view.
        foreach (var effect in FindObjectsByType<Behaviour>())
            if (effect.GetType().FullName == "UnityEngine.Rendering.Volume") effect.enabled = false;
        foreach (var existing in FindObjectsByType<Light>()) existing.enabled = false;
        var sun = new GameObject("Late afternoon sunlight").AddComponent<Light>();
        sun.transform.SetParent(transform); sun.type = LightType.Directional;
        sun.color = new Color(1, .85f, .65f); sun.intensity = 1.6f;
        sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(42, -38, 0);

        var grass = Paint(new Color(.39f, .49f, .28f));
        var foliage = Paint(new Color(.25f, .34f, .23f));
        Shape("Luye plateau", PrimitiveType.Cube, transform, new Vector3(0, -1.5f, 9), new Vector3(65, 3, 65), grass);
        FestivalGround(grass);
        var ridge = Paint(new Color(.28f, .44f, .39f));
        var distant = Paint(new Color(.43f, .58f, .55f));
        for (int i = 0; i < 14; i++)
        {
            float x = i * 7 - 46;
            Mountain(new Vector3(x, -1, 34 + i % 3 * 8), 9 + i % 4 * 2, i, i % 2 == 0 ? ridge : distant);
        }
        var fieldGold = Paint(new Color(.67f, .63f, .35f));
        for (int i = 0; i < 9; i++)
            Shape("Patchwork rice field", PrimitiveType.Cube, transform, new Vector3(i % 3 * 6 - 18, .03f, i / 3 * 5 + 5),
                new Vector3(5.7f, .05f, 4.7f), i % 2 == 0 ? fieldGold : teal);
        var cloud = Paint(new Color(.97f, .94f, .85f));
        for (int i = 0; i < 9; i++)
            for (int j = 0; j < 3; j++)
                Shape("High altitude cloud bank", PrimitiveType.Sphere, transform, new Vector3(i * 8 - 32 + j * 1.3f, 22 + i % 3 * 2 + j % 2 * .5f, 27 + i % 2 * 8), new Vector3(4, 1.1f, 2), cloud);
        for (int i = 0; i < 20; i++)
        {
            var tree = Group("Plateau tree", transform, new Vector3(Mathf.Sin(i * 5.2f) * 25, 0, 7 + i * 1.3f));
            Shape("Trunk", PrimitiveType.Cylinder, tree, new Vector3(0, 1, 0), new Vector3(.2f, 1, .2f), leather);
            for (int branch = 0; branch < 5; branch++)
            {
                float a = branch * 2.4f + i;
                Shape("Irregular foliage", PrimitiveType.Sphere, tree,
                    new Vector3(Mathf.Cos(a) * .55f, 2.1f + branch % 3 * .45f, Mathf.Sin(a) * .55f),
                    new Vector3(1.4f, 1.5f + branch % 2 * .4f, 1.25f), i % 2 == 0 ? grass : foliage);
            }
        }
        for (int i = 0; i < 12; i++)
        {
            var distantBalloon = Group("Special shape festival balloon", transform,
                new Vector3(-25 + i % 6 * 9 + i / 6 * 3, 6 + i % 4 * 1.7f, 15 + i / 6 * 10));
            Envelope(distantBalloon, .40f + i % 3 * .07f, i % 6);
            distantBalloon.localRotation = Quaternion.Euler(0, -18 + i % 3 * 15, 0);
        }
        balloon = Group("Player hot air balloon", transform, new Vector3(-3, 6, 0));
        Envelope(balloon, 1, 0);
        traveler = Group("Traveler Chen", balloon, new Vector3(-.38f, .25f, -.17f));
        Character(traveler, false);
        traveler.localScale = Vector3.one * .90f;
        var yu = Group("Traveler Yu", balloon, new Vector3(.48f, .25f, .03f));
        Character(yu, true);
        yu.localScale = Vector3.one * .90f;
        flame = Shape("Burner flame", PrimitiveType.Sphere, balloon, new Vector3(0, 2.4f, 0), new Vector3(.18f, .8f, .18f), Paint(new Color(1, .46f, .07f)));
        // A crate of custard apples beneath the basket rim.
        for (int i = 0; i < 3; i++)
        {
            var fruit = Group("Taitung custard apple", balloon, new Vector3(-.65f + i * .23f, .25f, -.52f));
            Shape("Fruit", PrimitiveType.Sphere, fruit, Vector3.zero, new Vector3(.22f, .27f, .22f), grass);
            for (int j = 0; j < 8; j++)
                Shape("Fruit scales", PrimitiveType.Sphere, fruit, new Vector3(Mathf.Sin(j * 2.4f) * .08f, j % 3 * .06f -.06f, -.085f), Vector3.one * .07f, teal);
        }
    }

    void Envelope(Transform root, float scale, int palette)
    {
        var body = Group("Balloon assembly", root, Vector3.zero);
        body.localScale = Vector3.one * scale;
        Color[] colors = { new Color(.79f, .34f, .22f), new Color(.85f, .66f, .35f), new Color(.27f, .48f, .46f), new Color(.90f, .83f, .62f) };
        Material[] gores = new Material[4];
        Color[][] festivalColors = {
            colors,
            new[] { new Color(.67f,.17f,.12f), new Color(.86f,.79f,.64f), new Color(.67f,.17f,.12f), new Color(.22f,.25f,.28f) },
            new[] { new Color(.18f,.30f,.47f), new Color(.79f,.79f,.72f), new Color(.42f,.55f,.64f), new Color(.79f,.79f,.72f) },
            new[] { new Color(.79f,.44f,.16f), new Color(.31f,.24f,.17f), new Color(.79f,.44f,.16f), new Color(.87f,.73f,.40f) },
            new[] { new Color(.24f,.30f,.31f), new Color(.62f,.66f,.64f), new Color(.24f,.30f,.31f), new Color(.76f,.75f,.65f) },
            new[] { new Color(.44f,.29f,.38f), new Color(.80f,.70f,.59f), new Color(.65f,.40f,.33f), new Color(.80f,.70f,.59f) }
        };
        for (int i = 0; i < 4; i++) gores[i] = Fabric(festivalColors[palette % 6][i]);
        // Curved vertical fabric panels form one continuous balloon envelope.
        const int panels = 16, rings = 24, segments = 4;
        for (int p = 0; p < panels; p++)
        {
            var vertices = new Vector3[(rings + 1) * (segments + 1)];
            var uv = new Vector2[vertices.Length];
            var triangles = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                float radius = Mathf.Max(0, Mathf.Sin(t * Mathf.PI)) * (1.5f + .9f * t) + .16f * (1 - t);
                float height = 5.4f, stretch = 1;
                if (palette == 1) { radius = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * Mathf.PI)), .85f) * (1.05f + t) + .12f * (1-t); height = 7; }
                if (palette == 2) { radius = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * Mathf.PI)), .60f) * 2.3f + .1f * (1-t); height = 4.4f; }
                if (palette == 3) { radius = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * Mathf.PI)), .28f) * 1.45f + .1f * (1-t); height = 6; }
                if (palette == 4) { radius = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * Mathf.PI)), .65f) * 1.6f + .1f * (1-t); height = 3.9f; stretch = 2.25f; }
                if (palette == 5) { radius = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * Mathf.PI)), .75f) * 1.85f + .1f * (1-t); height = 4.9f; }
                for (int s = 0; s <= segments; s++)
                {
                    float a = (p + s / (float)segments) / panels * Mathf.PI * 2;
                    float ribs = palette == 5 ? 1 + .23f * Mathf.Cos(a * 5) : 1;
                    int index = r * (segments + 1) + s;
                    vertices[index] = new Vector3(Mathf.Cos(a) * radius * stretch * ribs, 2.8f + t * height, Mathf.Sin(a) * radius * ribs);
                    uv[index] = new Vector2(s / (float)segments * 2, t * 8);
                    if (r < rings && s < segments)
                    {
                        int v = r * (segments + 1) + s;
                        triangles.Add(v); triangles.Add(v + segments + 1); triangles.Add(v + 1);
                        triangles.Add(v + 1); triangles.Add(v + segments + 1); triangles.Add(v + segments + 2);
                    }
                }
            }
            var mesh = new Mesh { name = "Curved fabric gore" }; mesh.vertices = vertices; mesh.uv = uv; mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
            var go = new GameObject("Fabric panel"); go.transform.SetParent(body, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = gores[(palette % 2 == 0 ? p : p / 2) % 4];
        }
        Shape("Basket floor", PrimitiveType.Cube, body, new Vector3(0, -.28f, 0), new Vector3(1.8f, .22f, 1.4f), leather);
        for (int i = 0; i < 12; i++)
        {
            float y = -.14f + i * .058f;
            Shape("Woven basket front", PrimitiveType.Cube, body, new Vector3(0, y, -.7f), new Vector3(1.8f, .038f, .10f), i % 2 == 0 ? leather : cream);
            Shape("Woven basket back", PrimitiveType.Cube, body, new Vector3(0, y, .7f), new Vector3(1.8f, .038f, .10f), leather);
            Shape("Woven basket side", PrimitiveType.Cube, body, new Vector3(-.87f, y, 0), new Vector3(.10f, .038f, 1.4f), leather);
            Shape("Woven basket side", PrimitiveType.Cube, body, new Vector3(.87f, y, 0), new Vector3(.10f, .038f, 1.4f), leather);
        }
        for (int i = 0; i < 4; i++)
        {
            Vector3 a = new Vector3(i % 2 == 0 ? -.8f : .8f, .5f, i < 2 ? -.6f : .6f);
            Rod("Suspension rope", body, a, new Vector3(a.x * .5f, 3.1f, a.z * .5f), .045f, dark);
        }
        Shape("Burner frame", PrimitiveType.Cube, body, new Vector3(0, 2, 0), new Vector3(1.4f, .10f, 1), dark);
        BalloonSilhouette(body, palette);
    }

    void BalloonSilhouette(Transform body, int variant)
    {
        // Festival silhouettes with structural details; no mascot faces or enlarged cartoon eyes.
        if (variant == 3)
        {
            Shape("Lantern lower collar", PrimitiveType.Cylinder, body, new Vector3(0, 2.85f, 0), new Vector3(.85f, .12f, .85f), dark);
            Shape("Lantern upper collar", PrimitiveType.Cylinder, body, new Vector3(0, 8.65f, 0), new Vector3(.85f, .12f, .85f), leather);
        }
        if (variant == 4)
        {
            for (int i = 0; i < 3; i++)
            {
                var fin = Shape("Airship tail stabilizer", PrimitiveType.Cube, body, new Vector3(-2.65f, 4.75f, 0), new Vector3(1.3f, .06f, 1.5f), denim);
                fin.localRotation = Quaternion.Euler(i * 60, 0, 0);
            }
        }
    }
    void Limb(Transform parent, string name, Vector3 a, Vector3 b, float bottom, float top, Material material)
    {
        float length = Vector3.Distance(a, b);
        var t = Sculpt(name, parent, a, new[] { 0f, length * .08f, length * .9f, length },
            new[] { bottom * .8f, bottom, top, top * .85f }, .85f, material);
        t.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
    }

    void Character(Transform root, bool female, bool seated = false)
    {
        // Adult proportions: smaller shaped face, longer limbs and a fitted silhouette.
        float hips = seated ? .26f : .93f;
        var jacket = female ? teal : dark;
        var torso = Group("Upper body", root, new Vector3(0, hips, 0));
        Sculpt("Tailored travel jacket", torso, Vector3.zero,
            new[] { 0f, .12f, .39f, .50f, .57f }, new[] { .19f, .18f, .24f, .22f, .10f }, .68f, jacket);
        var seam = cream;
        Shape("T-shirt", PrimitiveType.Cube, torso, new Vector3(0, .31f, -.145f), new Vector3(.12f, .43f, .025f), cream);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 hip = new Vector3(side * .11f, hips, 0);
            Vector3 knee = seated ? new Vector3(side * .26f, .17f, -.43f) : new Vector3(side * .11f, .49f, -.02f);
            Vector3 ankle = seated ? new Vector3(side * .18f, .08f, -.80f) : new Vector3(side * .12f, .11f, -.03f);
            Limb(root, "Denim thigh", hip, knee, .105f, .085f, denim);
            Limb(root, "Denim calf", knee, ankle, .087f, .057f, denim);
            var shoe = Group("Detailed sneaker", root, ankle);
            Shape("Shoe upper", PrimitiveType.Sphere, shoe, new Vector3(0, -.025f, -.07f), new Vector3(.17f, .115f, .29f), cream);
            Shape("Rubber sole", PrimitiveType.Cube, shoe, new Vector3(0, -.077f, -.07f), new Vector3(.17f, .032f, .28f), seam);
            for (int lace = 0; lace < 4; lace++)
                Rod("Lace", shoe, new Vector3(-.048f, .025f, -.14f + lace * .028f), new Vector3(.048f, .025f, -.12f + lace * .028f), .006f, dark);
            Vector3 shoulder = new Vector3(side * .21f, .47f, 0);
            Vector3 elbow = new Vector3(side * .29f, .18f, -.025f);
            Vector3 hand = new Vector3(side * .19f, seated ? .02f : .20f, -.27f);
            Limb(torso, "Sleeve", shoulder, elbow, .082f, .066f, jacket);
            Limb(torso, "Forearm", elbow, hand, .055f, .035f, skin);
            Shape("Palm", PrimitiveType.Sphere, torso, hand, new Vector3(.074f, .10f, .037f), skin);
            for (int finger = 0; finger < 4; finger++)
                Limb(torso, "Finger", hand + new Vector3(-.025f + finger * .016f, -.025f, 0),
                    hand + new Vector3(-.025f + finger * .016f, -.075f, -.014f), .008f, .006f, skin);
            Limb(torso, "Lapel", new Vector3(side * .14f, .53f, -.09f), new Vector3(side * .067f, .29f, -.15f), .028f, .015f, female ? cream : leather);
            Shape("Jacket pocket", PrimitiveType.Cube, torso, new Vector3(side * .12f, .13f, -.132f), new Vector3(.10f, .105f, .016f), jacket);
            Rod("Pocket seam", torso, new Vector3(side * .12f -.046f, .16f, -.146f), new Vector3(side * .12f + .046f, .16f, -.146f), .004f, cream);
        }
        Shape("Neck", PrimitiveType.Cylinder, torso, new Vector3(0, .60f, 0), new Vector3(.105f, .07f, .105f), skin);
        var head = Group("Illustrated traveler head", torso, new Vector3(0, .64f, 0));
        head.localScale = new Vector3(.92f, 1.02f, .92f);
        Sculpt("Shaped jaw and cheekbones", head, Vector3.zero,
            new[] { 0f, .032f, .09f, .17f, .25f, .31f, .35f }, new[] { .035f, .068f, .11f, .145f, .143f, .105f, .015f }, .86f, skin);
        for (int side = -1; side <= 1; side += 2)
        {
            Shape("Ear", PrimitiveType.Sphere, head, new Vector3(side * .14f, .18f, 0), new Vector3(.044f, .065f, .031f), skin);
            Shape("Eye white", PrimitiveType.Sphere, head, new Vector3(side * .060f, .207f, -.113f), new Vector3(.052f, .020f, .016f), cream);
            Shape("Iris", PrimitiveType.Sphere, head, new Vector3(side * .059f, .207f, -.125f), new Vector3(.017f, .019f, .008f), hair);
            Shape("Catchlight", PrimitiveType.Sphere, head, new Vector3(side * .059f -.004f, .214f, -.131f), Vector3.one * .005f, cream);
            Rod("Upper eyelid", head, new Vector3(side * .06f -.03f, .222f, -.119f), new Vector3(side * .06f + .028f, .224f, -.119f), .005f, hair);
            Rod("Brow", head, new Vector3(side * .06f -.031f, .253f, -.111f), new Vector3(side * .06f + .025f, .258f, -.11f), .007f, hair);
            if (female)
            {
                for (int j = 0; j < 16; j++)
                {
                    float a = j * Mathf.PI * 2 / 16, b = (j + 1) * Mathf.PI * 2 / 16;
                    Rod("Round glasses", head, new Vector3(side * .06f + Mathf.Cos(a) * .043f, .205f + Mathf.Sin(a) * .038f, -.14f),
                        new Vector3(side * .06f + Mathf.Cos(b) * .043f, .205f + Mathf.Sin(b) * .038f, -.14f), .004f, dark);
                }
                for (int j = 0; j < 6; j++)
                    Strand(head, new Vector3(side * .12f, .30f, .035f + j * .02f), new Vector3(side * .17f, .04f, .09f),
                        new Vector3(side * (.12f + j * .01f), -.28f, .07f), .027f, hair);
            }
        }
        Shape("Nose", PrimitiveType.Sphere, head, new Vector3(0, .165f, -.119f), new Vector3(.026f, .051f, .033f), skin);
        Rod("Mouth", head, new Vector3(-.026f, .094f, -.084f), new Vector3(.026f, .095f, -.084f), .005f, hair);
        Sculpt("Hair crown", head, new Vector3(0, .24f, .017f), new[] { 0f, .05f, .10f, .125f }, new[] { .147f, .145f, .11f, .018f }, .87f, hair);
        for (int i = 0; i < 11; i++)
        {
            float x = -.13f + i * .025f;
            Strand(head, new Vector3(x, .33f, -.028f), new Vector3(x + .033f, .27f, -.125f),
                new Vector3(x -.022f, .20f + i % 4 * .013f, -.124f), .022f, hair);
        }
        var hat = female ? leather : teal;
        Shape("Fabric cap", PrimitiveType.Sphere, head, new Vector3(0, .355f, .022f), new Vector3(.33f, .13f, .285f), hat);
        Shape("Curved cap brim", PrimitiveType.Sphere, head, new Vector3(0, .332f, -.142f), new Vector3(.30f, .025f, .19f), hat);
        Shape("Cap embroidered patch", PrimitiveType.Cube, head, new Vector3(0, .372f, -.122f), new Vector3(.065f, .039f, .008f), cream);
        var bag = Group("Travel backpack", torso, new Vector3(0, .29f, .215f));
        Shape("Canvas bag", PrimitiveType.Sphere, bag, Vector3.zero, new Vector3(.34f, .44f, .21f), leather);
        Shape("Bag pocket", PrimitiveType.Cube, bag, new Vector3(0, -.08f, .10f), new Vector3(.24f, .16f, .03f), teal);
        for (int side = -1; side <= 1; side += 2)
            Rod("Canvas shoulder strap", torso, new Vector3(side * .14f, .08f, -.12f), new Vector3(side * .14f, .51f, -.11f), .026f, leather);
        if (female)
        {
            Shape("Camera body", PrimitiveType.Cube, torso, new Vector3(0, .17f, -.25f), new Vector3(.19f, .12f, .08f), dark);
            var lens = Shape("Camera lens", PrimitiveType.Cylinder, torso, new Vector3(0, .17f, -.32f), new Vector3(.065f, .055f, .065f), denim);
            lens.localRotation = Quaternion.Euler(90, 0, 0);
        }
    }
    Transform MeshObject(string name, Transform parent, Vector3 position, Vector3[] vertices, int[] indices, Material material)
    {
        var mesh = new Mesh { name = name }; mesh.vertices = vertices; mesh.triangles = indices;
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); sculptedMeshes.Add(mesh);
        var t = Group(name, parent, position);
        t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        return t;
    }

    void Mountain(Vector3 position, float height, int seed, Material material)
    {
        const int n = 24;
        var v = new Vector3[(n + 1) * (n + 1)]; var tris = new List<int>();
        for (int z = 0; z <= n; z++) for (int x = 0; x <= n; x++)
        {
            float px = (x / (float)n - .5f) * 22, pz = (z / (float)n - .5f) * 20;
            float edge = Mathf.Max(0, 1 - Mathf.Sqrt(px * px / 121 + pz * pz / 100));
            float rough = .68f + Mathf.PerlinNoise(x * .18f + seed * 2.1f, z * .18f) * .7f;
            v[z * (n + 1) + x] = new Vector3(px, Mathf.Pow(edge, .65f) * height * rough, pz);
            if (x < n && z < n)
            {
                int k = z * (n + 1) + x;
                tris.AddRange(new[] { k, k + n + 1, k + 1, k + 1, k + n + 1, k + n + 2 });
            }
        }
        MeshObject("Irregular mountain ridge", transform, position, v, tris.ToArray(), material);
    }

    // Ring profiles produce a shaped jaw, fitted garments and tapered fabric, rather than round capsules.
    Transform Sculpt(string name, Transform parent, Vector3 position, float[] heights, float[] radii, float depth, Material material)
    {
        const int sides = 32;
        var v = new Vector3[heights.Length * sides + 2];
        var tris = new List<int>();
        for (int r = 0; r < heights.Length; r++)
            for (int s = 0; s < sides; s++)
            {
                float a = s * Mathf.PI * 2 / sides;
                v[r * sides + s] = new Vector3(Mathf.Cos(a) * radii[r], heights[r], Mathf.Sin(a) * radii[r] * depth);
                if (r < heights.Length - 1)
                {
                    int n = (s + 1) % sides, k = r * sides;
                    tris.AddRange(new[] { k + s, k + sides + s, k + n, k + n, k + sides + s, k + sides + n });
                }
            }
        int low = v.Length - 2, high = v.Length - 1;
        v[low] = new Vector3(0, heights[0], 0); v[high] = new Vector3(0, heights[heights.Length - 1], 0);
        for (int s = 0; s < sides; s++)
        {
            int n = (s + 1) % sides, last = (heights.Length - 1) * sides;
            tris.AddRange(new[] { low, s, n, high, last + n, last + s });
        }
        return MeshObject(name, parent, position, v, tris.ToArray(), material);
    }

    void Strand(Transform root, Vector3 a, Vector3 b, Vector3 c, float width, Material material)
    {
        const int steps = 10, sides = 8;
        var v = new Vector3[(steps + 1) * sides]; var tris = new List<int>();
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * b + t * t * c;
            float r = width * Mathf.Pow(1 - t, .6f) + .001f;
            for (int j = 0; j < sides; j++)
            {
                float angle = j * Mathf.PI * 2 / sides;
                v[i * sides + j] = p + new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r * .45f);
                if (i < steps)
                {
                    int k = i * sides, next = (j + 1) % sides;
                    tris.AddRange(new[] { k + j, k + sides + j, k + next, k + next, k + sides + j, k + sides + next });
                }
            }
        }
        MeshObject("Tapered hair lock", root, Vector3.zero, v, tris.ToArray(), material);
    }

    static float GroundHeight(float x, float z)
    {
        return .18f + .85f * Mathf.Exp(-(x * x / 400 + (z - 6) * (z - 6) / 160))
            + .09f * Mathf.Sin(x * .32f) * Mathf.Cos(z * .27f);
    }

    sealed class VisitorState
    {
        readonly Transform root, upper, head;
        readonly Transform[] thighs = new Transform[2], calves = new Transform[2], shoes = new Transform[2];
        readonly float[] thighLengths = new float[2], calfLengths = new float[2];
        readonly System.Random rng;
        readonly float speed;
        Vector3 destination;
        float timer, sitBlend, stride;
        bool resting, seated;

        public VisitorState(Transform character, bool sitting, int seed)
        {
            root = character; rng = new System.Random(seed); speed = .48f + (float)rng.NextDouble() * .38f;
            upper = root.Find("Upper body"); head = upper.Find("Illustrated traveler head");
            int ti = 0, ci = 0, si = 0;
            foreach (Transform child in root)
            {
                if (child.name == "Denim thigh" && ti < 2) thighs[ti++] = child;
                if (child.name == "Denim calf" && ci < 2) calves[ci++] = child;
                if (child.name == "Detailed sneaker" && si < 2) shoes[si++] = child;
            }
            for (int i = 0; i < 2; i++)
            {
                thighLengths[i] = thighs[i].GetComponent<MeshFilter>().sharedMesh.bounds.size.y;
                calfLengths[i] = calves[i].GetComponent<MeshFilter>().sharedMesh.bounds.size.y;
            }
            seated = sitting; sitBlend = sitting ? 1 : 0;
            resting = sitting; timer = 8 + (float)rng.NextDouble() * 22;
            destination = root.localPosition;
            if (!sitting) ChooseDestination();
        }

        void ChooseDestination()
        {
            destination = new Vector3(-11 + (float)rng.NextDouble() * 22, 0, -6 + (float)rng.NextDouble() * 8);
        }

        public void Tick(float dt, List<Transform> people)
        {
            dt = Mathf.Min(dt, .05f);
            timer -= dt;
            if (resting && timer <= 0)
            { resting = false; seated = false; ChooseDestination(); }
            sitBlend = Mathf.MoveTowards(sitBlend, seated ? 1 : 0, dt * .8f);
            bool walking = !resting && sitBlend < .05f;
            Vector3 current = root.localPosition;
            if (walking)
            {
                Vector3 delta = destination - current; delta.y = 0;
                if (delta.magnitude < .3f)
                {
                    resting = true; seated = rng.NextDouble() < .60;
                    timer = seated ? 14 + (float)rng.NextDouble() * 28 : 3 + (float)rng.NextDouble() * 5;
                    walking = false;
                }
                else
                {
                    Vector3 direction = delta.normalized;
                    foreach (var person in people)
                    {
                        if (person == root) continue;
                        Vector3 away = current - person.localPosition; away.y = 0;
                        float distance = away.magnitude;
                        if (distance < .85f && distance > .01f)
                            direction += away.normalized * (.85f - distance) * 2;
                    }
                    direction.Normalize();
                    current += direction * speed * dt;
                    current.x = Mathf.Clamp(current.x, -11.5f, 11.5f);
                    current.z = Mathf.Clamp(current.z, -6.5f, 2.5f);
                    root.localRotation = Quaternion.Slerp(root.localRotation, Quaternion.LookRotation(-direction), 1 - Mathf.Exp(-dt * 5));
                    stride += dt * speed * 7;
                }
            }
            if (resting)
            {
                Vector3 stageDirection = new Vector3(7, 0, 8) - current; stageDirection.y = 0;
                root.localRotation = Quaternion.Slerp(root.localRotation, Quaternion.LookRotation(-stageDirection), 1 - Mathf.Exp(-dt * 2));
            }
            current.y = GroundHeight(current.x, current.z) + Mathf.Lerp(-.022f, .012f, sitBlend) * root.localScale.y;
            root.localPosition = current;
            float hipY = Mathf.Lerp(.93f, .26f, sitBlend);
            upper.localPosition = new Vector3(0, hipY + (walking ? Mathf.Abs(Mathf.Sin(stride)) * .012f : 0), 0);
            upper.localRotation = Quaternion.Euler(0, 0, walking ? Mathf.Sin(stride) * 1.2f : 0);
            head.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * .28f + speed * 40) * (resting ? 8 : 3), 0);
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1 : 1;
                float gait = walking ? Mathf.Sin(stride + i * Mathf.PI) : 0;
                float thighAngle = Mathf.Lerp(gait * 24, -78, sitBlend) * Mathf.Deg2Rad;
                float calfAngle = Mathf.Lerp(gait * 24 - Mathf.Max(0, -gait) * 34, -77, sitBlend) * Mathf.Deg2Rad;
                Vector3 hip = new Vector3(side * .11f, hipY, 0);
                Vector3 knee = hip + new Vector3(side * .15f * sitBlend, -Mathf.Cos(thighAngle) * thighLengths[i], Mathf.Sin(thighAngle) * thighLengths[i]);
                Vector3 ankle = knee + new Vector3(-side * .08f * sitBlend, -Mathf.Cos(calfAngle) * calfLengths[i], Mathf.Sin(calfAngle) * calfLengths[i]);
                if (walking)
                {
                    float phase = stride + i * Mathf.PI;
                    ankle = new Vector3(side * .11f, .12f + Mathf.Max(0, Mathf.Sin(phase)) * .095f, Mathf.Cos(phase) * .15f);
                    Vector3 leg = ankle - hip;
                    float distance = Mathf.Min(leg.magnitude, thighLengths[i] + calfLengths[i] - .001f);
                    Vector3 axis = leg.normalized;
                    ankle = hip + axis * distance;
                    float along = (thighLengths[i] * thighLengths[i] - calfLengths[i] * calfLengths[i] + distance * distance) / (2 * distance);
                    float bend = Mathf.Sqrt(Mathf.Max(0, thighLengths[i] * thighLengths[i] - along * along));
                    Vector3 forwardBend = new Vector3(0, -axis.z, axis.y).normalized;
                    knee = hip + axis * along + forwardBend * bend;
                }
                thighs[i].localPosition = hip;
                thighs[i].localRotation = Quaternion.FromToRotation(Vector3.up, knee - hip);
                calves[i].localPosition = knee;
                calves[i].localRotation = Quaternion.FromToRotation(Vector3.up, ankle - knee);
                shoes[i].localPosition = ankle;
                shoes[i].localRotation = Quaternion.Euler(walking ? Mathf.Max(0, gait) * 14 : 0, 0, 0);
            }
        }
    }

    void FestivalGround(Material grass)
    {
        const int n = 64;
        var vertices = new Vector3[(n + 1) * (n + 1)]; var tris = new List<int>();
        for (int z = 0; z <= n; z++) for (int x = 0; x <= n; x++)
        {
            float px = x - 32, pz = z - 18;
            vertices[z * (n + 1) + x] = new Vector3(px, GroundHeight(px, pz), pz);
            if (x < n && z < n)
            {
                int k = z * (n + 1) + x;
                tris.AddRange(new[] { k, k + n + 1, k + 1, k + 1, k + n + 1, k + n + 2 });
            }
        }
        MeshObject("Sculpted festival hillside", transform, Vector3.zero, vertices, tris.ToArray(), grass);
        var stage = Group("Hillside acoustic concert", transform, new Vector3(7, GroundHeight(7, 8), 8));
        Shape("Stage deck", PrimitiveType.Cube, stage, new Vector3(0, .35f, 0), new Vector3(6.5f, .7f, 3.4f), leather);
        Shape("Stage backdrop", PrimitiveType.Cube, stage, new Vector3(0, 2.2f, 1.4f), new Vector3(6.1f, 3, .15f), teal);
        for (int side = -1; side <= 1; side += 2)
        {
            Rod("Truss upright", stage, new Vector3(side * 3.3f, .6f, 0), new Vector3(side * 3.3f, 4.2f, 0), .12f, dark);
            Shape("PA speaker", PrimitiveType.Cube, stage, new Vector3(side * 2.9f, 1.2f, -.9f), new Vector3(.65f, 1.1f, .55f), dark);
            Shape("Speaker cone", PrimitiveType.Sphere, stage, new Vector3(side * 2.9f, 1.2f, -1.2f), new Vector3(.40f, .40f, .07f), denim);
        }
        Rod("Lighting truss", stage, new Vector3(-3.3f, 4.2f, 0), new Vector3(3.3f, 4.2f, 0), .12f, dark);
        for (int i = 0; i < 4; i++)
        {
            var fixture = Group("Moving concert fixture", stage, new Vector3(-2.4f + i * 1.6f, 4, -.1f));
            Shape("Light housing", PrimitiveType.Cylinder, fixture, Vector3.zero, new Vector3(.22f, .18f, .22f), dark);
            Shape("Colored concert lens", PrimitiveType.Sphere, fixture, new Vector3(0, -.19f, 0),
                new Vector3(.20f, .035f, .20f), i % 2 == 0 ? cream : teal);
            stageLights.Add(fixture);
        }
        Sign(stage, new Vector3(0, 2.6f, 1.28f), "LUYE LIVE\n鹿野草地音樂會", .16f, cream);
        for (int i = 0; i < 3; i++)
        {
            var performer = Group("Live musician", stage, new Vector3(-1.6f + i * 1.6f, .88f, .2f));
            Character(performer, i == 1); performer.localRotation = Quaternion.Euler(0, 12 - i * 12, 0);
            Rod("Microphone stand", stage, new Vector3(-1.6f + i * 1.6f, .7f, -.5f), new Vector3(-1.6f + i * 1.6f, 2.25f, -.5f), .035f, dark);
            Shape("Microphone", PrimitiveType.Capsule, stage, new Vector3(-1.6f + i * 1.6f, 2.25f, -.5f), new Vector3(.07f, .12f, .07f), dark);
            if (i != 1)
            {
                Shape("Acoustic guitar body", PrimitiveType.Sphere, performer, new Vector3(.10f, .64f, -.43f), new Vector3(.39f, .47f, .12f), leather);
                Shape("Guitar sound hole", PrimitiveType.Sphere, performer, new Vector3(.10f, .70f, -.51f), new Vector3(.12f, .12f, .015f), dark);
                Rod("Guitar neck", performer, new Vector3(.1f, .77f, -.43f), new Vector3(.47f, 1.07f, -.43f), .06f, leather);
            }
        }
        var warm = Paint(new Color(.83f, .70f, .47f));
        var red = Paint(new Color(.60f, .30f, .24f));
        for (int i = 0; i < 4; i++)
        {
            float x = -14 + i * 3.3f, z = 8 + i % 2;
            var stall = Group("Festival food stall", transform, new Vector3(x, GroundHeight(x, z), z));
            Shape("Stall counter", PrimitiveType.Cube, stall, new Vector3(0, .65f, 0), new Vector3(2.6f, 1.3f, 1.4f), warm);
            Shape("Awning", PrimitiveType.Cube, stall, new Vector3(0, 2.4f, 0), new Vector3(2.9f, .14f, 2), i % 2 == 0 ? red : teal);
            for (int j = -1; j <= 1; j += 2) Rod("Tent support", stall, new Vector3(j * 1.2f, 0, .55f), new Vector3(j * 1.2f, 2.4f, .55f), .065f, cream);
            Sign(stall, new Vector3(0, 1.82f, -.75f), i % 2 == 0 ? "台東釋迦" : "咖啡 · 點心", .12f, cream);
        }
        // Fixed seed keeps arrangements intentional and repeatable, without regular rows of clones.
        var rng = new System.Random(1013);
        for (int i = 0; i < 34; i++)
        {
            float x = (float)rng.NextDouble() * 22 - 11;
            float z = (float)rng.NextDouble() * 10 - 6;
            bool seated = rng.NextDouble() < .72;
            var p = new Vector3(x, GroundHeight(x, z) + (seated ? .035f : .10f), z);
            var guest = Group(i % 4 == 0 ? "Walking festival visitor" : "Concert audience", transform, p);
            Character(guest, i % 3 != 0, seated);
            guest.localScale = Vector3.one * (.78f + (float)rng.NextDouble() * .18f);
            guest.localRotation = Quaternion.Euler(0, i % 4 == 0 ? 45 : 175 + (float)rng.NextDouble() * 30, 0);
            // Outfit variation without modifying the two main travelers' materials.
            Color[] wardrobe = { new Color(.28f,.30f,.29f), new Color(.49f,.45f,.37f), new Color(.30f,.36f,.41f),
                new Color(.61f,.58f,.50f), new Color(.36f,.29f,.26f), new Color(.43f,.46f,.39f) };
            var outfit = Fabric(wardrobe[rng.Next(wardrobe.Length)]);
            var complexion = Paint(Color.Lerp(new Color(.60f,.40f,.29f), new Color(.86f,.70f,.56f), (float)rng.NextDouble()));
            var hairstyle = Paint(Color.Lerp(new Color(.09f,.07f,.06f), new Color(.28f,.21f,.16f), (float)rng.NextDouble()));
            foreach (var renderer in guest.GetComponentsInChildren<Renderer>())
            {
                if (renderer.name.Contains("jacket") || renderer.name.Contains("Sleeve")) renderer.sharedMaterial = outfit;
                if (renderer.sharedMaterial == skin) renderer.sharedMaterial = complexion;
                if (renderer.sharedMaterial == hair) renderer.sharedMaterial = hairstyle;
                if (i % 3 == 0 && (renderer.name.Contains("cap") || renderer.name.Contains("Cap"))) renderer.enabled = false;
                if (i % 5 == 0 && renderer.name == "Canvas bag") renderer.enabled = false;
            }
            visitors.Add(guest); visitorStates.Add(new VisitorState(guest, seated, 1013 + i * 71));
        }
        for (int i = 0; i < 5; i++)
        {
            float x = -9 + i * 4.5f, z = -6 + i % 2 * 2;
            var picnic = Group("Picnic blanket and travel bags", transform, new Vector3(x, GroundHeight(x, z) + .025f, z));
            Shape("Picnic blanket", PrimitiveType.Cube, picnic, Vector3.zero, new Vector3(2, .025f, 1.4f), i % 2 == 0 ? red : cream);
            Shape("Picnic hamper", PrimitiveType.Cube, picnic, new Vector3(.5f, .18f, .3f), new Vector3(.42f, .35f, .33f), leather);
            Shape("Water bottle", PrimitiveType.Cylinder, picnic, new Vector3(-.4f, .2f, .3f), new Vector3(.12f, .2f, .12f), teal);
        }
        for (int i = 0; i < 10; i++)
        {
            float x = -17 + i * 3.6f;
            var pole = new Vector3(x, GroundHeight(x, -8), -8);
            Rod("Festival pennant pole", transform, pole, pole + Vector3.up * 2.6f, .055f, cream);
            if (i > 0) Rod("Pennant cord", transform, pole + Vector3.up * 2.5f, new Vector3(x - 3.6f, GroundHeight(x - 3.6f, -8) + 2.5f, -8), .015f, dark);
            for (int j = 0; j < 4; j++)
            {
                float px = x + j * .75f;
                MeshObject("Fabric pennant", transform, new Vector3(px, pole.y + 2.5f, -8),
                    new[] { Vector3.zero, new Vector3(.45f, 0, 0), new Vector3(.225f, -.55f, 0) },
                    new[] { 0, 1, 2, 2, 1, 0 }, j % 2 == 0 ? red : warm);
            }
        }
    }

    void Sign(Transform root, Vector3 position, string text, float size, Material color)
    {
        var t = Group("Festival sign", root, position);
        // Physical emblems remain legible at a distance and work with the same lit materials as the scenery.
        Shape("Signboard", PrimitiveType.Cube, t, Vector3.zero, new Vector3(1.8f, .65f, .08f), color);
        if (text.StartsWith("LUYE"))
        {
            Rod("Music note stem", t, new Vector3(-.18f, -.13f, -.06f), new Vector3(-.18f, .22f, -.06f), .045f, dark);
            Rod("Music note stem", t, new Vector3(.22f, -.08f, -.06f), new Vector3(.22f, .24f, -.06f), .045f, dark);
            Rod("Music note beam", t, new Vector3(-.18f, .22f, -.06f), new Vector3(.22f, .24f, -.06f), .06f, dark);
            Shape("Note", PrimitiveType.Sphere, t, new Vector3(-.26f, -.13f, -.06f), new Vector3(.22f, .14f, .03f), dark);
            Shape("Note", PrimitiveType.Sphere, t, new Vector3(.14f, -.08f, -.06f), new Vector3(.22f, .14f, .03f), dark);
        }
        else
        {
            Shape("Local produce emblem", PrimitiveType.Sphere, t, new Vector3(0, 0, -.08f), new Vector3(.32f, .40f, .05f), teal);
            Rod("Leaf", t, new Vector3(0, .17f, -.09f), new Vector3(.12f, .27f, -.09f), .035f, dark);
        }
    }

    public void Animate(float altitude, float drift, float throttle, float wind)
    {
        if (balloon == null) return;
        balloon.localPosition = new Vector3(-3 + drift * .22f, 6 + (altitude - 52) * .08f, 0);
        balloon.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * .6f) * 1.2f, -12, -wind * 3);
        traveler.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * .9f) * 7, 0);
        flame.gameObject.SetActive(throttle > .04f);
        flame.localScale = new Vector3(.18f, .2f + throttle * .8f + Mathf.Sin(Time.time * 23) * .05f, .18f);
        Vector3 cameraPosition, cameraFocus;
        float cameraSize;
        if (cameraMode == 1)
        {
            cameraSize = 1.8f;
            cameraFocus = balloon.position + new Vector3(0, 1.1f, 0);
            cameraPosition = cameraFocus + new Vector3(2.3f, 1.2f, -9);
        }
        else if (cameraMode == 2)
        {
            // An elevated spectator view keeps the crowd in front and the musicians behind it.
            cameraSize = 6.4f;
            cameraFocus = new Vector3(4, 1.9f, 4);
            cameraPosition = new Vector3(10, 8, -13);
        }
        else
        {
            cameraSize = 12.2f;
            cameraPosition = new Vector3(10, 19, -29);
            cameraFocus = new Vector3(0, 7.2f, 4);
        }
        float blend = 1 - Mathf.Exp(-Time.deltaTime * 6);
        view.orthographicSize = Mathf.Lerp(view.orthographicSize, cameraSize, blend);
        view.transform.position = Vector3.Lerp(view.transform.position, cameraPosition, blend);
        view.transform.rotation = Quaternion.Slerp(view.transform.rotation,
            Quaternion.LookRotation(cameraFocus - cameraPosition), blend);
        for (int i = 0; i < visitorStates.Count; i++) visitorStates[i].Tick(Time.deltaTime, visitors);
        for (int i = 0; i < stageLights.Count; i++)
            stageLights[i].localRotation = Quaternion.Euler(35 + Mathf.Sin(Time.time * .7f + i) * 15, Mathf.Sin(Time.time * .4f + i) * 35, 0);
    }

    void OnDestroy()
    {
        foreach (var m in materials) if (m != null) Destroy(m);
        foreach (var texture in fabricTextures) if (texture != null) Destroy(texture);
        foreach (var mesh in sculptedMeshes) if (mesh != null) Destroy(mesh);
        foreach (var filter in GetComponentsInChildren<MeshFilter>())
            if (filter.sharedMesh != null && filter.sharedMesh.name == "Curved fabric gore") Destroy(filter.sharedMesh);
    }
}
