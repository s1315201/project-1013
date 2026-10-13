using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>A self-contained Luye balloon prototype. Starts when entering Play mode.</summary>
public sealed class LuyeBalloonGame : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAnyObjectByType<LuyeBalloonGame>() == null)
            new GameObject("Luye Balloon Game").AddComponent<LuyeBalloonGame>();
    }

    float altitude, velocity, heat, throttle, elapsed, goldenTime, drift, wind;
    bool started, ended, won, showHelp;
    string reason = "";
    Texture2D disc, paper;
    LuyeWorld world;
    GUIStyle label;
    const float Duration = 60f;
    const float GoldenLow = 42f, GoldenHigh = 62f;

    void Awake()
    {
        disc = new Texture2D(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                disc.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) < 31 ? Color.white : Color.clear);
        disc.Apply();
        world = new GameObject("Luye 3D Travel World").AddComponent<LuyeWorld>();
        world.Build();
        paper = new Texture2D(128, 128);
        for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
        {
            float n = Mathf.PerlinNoise(x * .7f, y * .7f) * .055f;
            paper.SetPixel(x, y, new Color(.97f - n, .94f - n, .86f - n));
        }
        paper.Apply();
        ResetRound();
    }

    void OnDestroy() { if (disc != null) Destroy(disc); if (paper != null) Destroy(paper); if (world != null) Destroy(world.gameObject); }
    void LateUpdate() { if (world != null) world.Animate(altitude, drift, started && !ended ? throttle : 0, wind); }

    void ResetRound()
    {
        altitude = 52; velocity = 0; heat = .48f; throttle = 0;
        elapsed = goldenTime = drift = wind = 0;
        started = ended = won = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame) ResetRound();
        if (keyboard != null && keyboard.hKey.wasPressedThisFrame) showHelp = !showHelp;
        if (keyboard != null && keyboard.enterKey.wasPressedThisFrame && (!started || ended)) { ResetRound(); started = true; throttle = .48f; }
        if (!started || ended) return;
        float dt = Mathf.Min(Time.deltaTime, .05f);
        if (keyboard != null)
        {
            if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed) throttle += dt * .55f;
            if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) throttle -= dt * .55f;
            if (keyboard.spaceKey.isPressed) throttle = 1;
            if (keyboard.spaceKey.wasReleasedThisFrame) throttle = 0;
        }
        var pad = Gamepad.current;
        if (pad != null) throttle = pad.rightTrigger.ReadValue();
        throttle = Mathf.Clamp01(throttle);
        elapsed += dt;
        wind = Mathf.Sin(elapsed * .49f) * .7f + Mathf.Sin(elapsed * 1.13f + 1.8f) * .3f;
        heat = Mathf.MoveTowards(heat, throttle, dt * (throttle > heat ? .42f : .25f));
        velocity += ((heat - .48f) * 19 + wind * 2.8f - velocity * .65f) * dt;
        altitude += velocity * dt;
        // The golden current shelters the balloon; climbing out exposes it to stronger wind.
        float exposure = Mathf.Abs(altitude - 52) / 22f;
        drift += (wind * (.3f + exposure * 1.5f) - drift * .13f) * dt;
        if (altitude >= GoldenLow && altitude <= GoldenHigh) goldenTime += dt;
        if (altitude <= 12) Finish(false, "下降過低，吊籃撞上山谷！");
        else if (altitude >= 92) Finish(false, "噴火過猛，飛出了安全高度！");
        else if (Mathf.Abs(drift) >= 9) Finish(false, "離開黃金氣流，被強風吹離航線！");
        else if (elapsed >= Duration) Finish(true, "完成鹿野高台飛行！");
    }

    void Finish(bool success, string message) { ended = true; won = success; reason = message; throttle = 0; }
    void Box(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
    void Oval(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, disc); GUI.color = Color.white; }
    void Text(float x, float y, string value, GUIStyle style) { GUI.Label(new Rect(x, y, 1100, 80), value, style); }

    readonly Color Ink = new Color(.12f, .23f, .22f);
    readonly Color Gold = new Color(.94f, .72f, .33f);
    void Line(Vector2 a, Vector2 b, float width, Color color)
    {
        // Draw in the same logical coordinate space as the icon, including letterboxed views.
        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b)));
        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
            Oval(new Rect(p.x - width * .5f, p.y - width * .5f, width, width), color);
        }
    }
    bool IconButton(Rect rect, int icon, bool selected = false)
    {
        Oval(rect, selected ? Ink : new Color(.97f, .94f, .86f, .90f));
        Color color = selected ? Color.white : Ink;
        Vector2 c = rect.center;
        if (icon == 0) // balloon
        {
            Oval(new Rect(c.x - 9, c.y - 14, 18, 22), color);
            Line(c + new Vector2(-5, 5), c + new Vector2(-4, 13), 2, color);
            Line(c + new Vector2(5, 5), c + new Vector2(4, 13), 2, color);
            Box(new Rect(c.x - 5, c.y + 11, 10, 5), color);
        }
        else if (icon == 1) // traveler
        {
            Oval(new Rect(c.x - 6, c.y - 14, 12, 12), color);
            Oval(new Rect(c.x - 11, c.y, 22, 16), color);
        }
        else if (icon == 2) // music
        {
            Line(c + new Vector2(-6, -10), c + new Vector2(8, -13), 3, color);
            Line(c + new Vector2(-6, -10), c + new Vector2(-6, 10), 2, color);
            Line(c + new Vector2(8, -13), c + new Vector2(8, 7), 2, color);
            Oval(new Rect(c.x - 13, c.y + 6, 10, 7), color);
            Oval(new Rect(c.x + 1, c.y + 3, 10, 7), color);
        }
        else if (icon == 3) // play
        {
            Line(c + new Vector2(-5, -12), c + new Vector2(10, 0), 3, color);
            Line(c + new Vector2(10, 0), c + new Vector2(-5, 12), 3, color);
            Line(c + new Vector2(-5, 12), c + new Vector2(-5, -12), 3, color);
        }
        else // reset
        {
            Line(c + new Vector2(-9, 4), c + new Vector2(-9, -8), 3, color);
            Line(c + new Vector2(-9, -8), c + new Vector2(8, -8), 3, color);
            Line(c + new Vector2(8, -8), c + new Vector2(8, 10), 3, color);
            Line(c + new Vector2(8, 10), c + new Vector2(-5, 10), 3, color);
            Line(c + new Vector2(-9, 4), c + new Vector2(-15, -2), 3, color);
        }
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }
    void OnGUI()
    {
        if (label == null)
        {
            var font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft JhengHei", "Arial" }, 18);
            label = new GUIStyle(GUI.skin.label) { font = font, fontSize = 16, wordWrap = true, normal = { textColor = Ink } };
        }
        Matrix4x4 previous = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) * .5f, (Screen.height - 800 * scale) * .5f, 0), Quaternion.identity, Vector3.one * scale);
        for (int i = 0; i < 3; i++)
            if (IconButton(new Rect(24 + i * 60, 24, 48, 48), i, world != null && world.CameraMode == i) && world != null) world.SetCameraMode(i);
        if (IconButton(new Rect(1208, 24, 48, 48), 4)) ResetRound();
        // A slim altitude ruler and highlighted safe band replace the written instrument panel.
        Box(new Rect(1218, 272, 26, 208), new Color(.97f, .94f, .86f, .8f));
        Box(new Rect(1226, 286, 10, 180), new Color(.14f, .25f, .23f, .25f));
        Box(new Rect(1226, 286 + 180 * .38f, 10, 180 * .20f), Gold);
        Oval(new Rect(1218, 286 + 180 * (1 - Mathf.Clamp01(altitude / 100)) - 9, 26, 18),
            altitude >= GoldenLow && altitude <= GoldenHigh ? Gold : Ink);
        Box(new Rect(32, 746, 185, 31), new Color(.97f, .94f, .86f, .85f));
        GUI.enabled = started && !ended;
        throttle = GUI.HorizontalSlider(new Rect(48, 755, 153, 20), throttle, 0, 1);
        GUI.enabled = true;
        Box(new Rect(48, 787, 153, 4), new Color(.14f, .25f, .23f, .25f));
        Box(new Rect(48, 787, 153 * heat, 4), Gold);
        if (started && !ended)
        {
            Box(new Rect(360, 776, 560, 4), new Color(1, 1, 1, .35f));
            Box(new Rect(360, 776, 560 * Mathf.Clamp01(elapsed / Duration), 4), Gold);
        }
        if (!started || ended)
        {
            if (ended)
            {
                Box(new Rect(430, 687, 420, 47), new Color(.97f, .94f, .86f, .9f));
                GUI.Label(new Rect(448, 697, 390, 30), won ? $"飛行完成 · 釋迦 {(int)(goldenTime / 5)} 顆" : reason, label);
            }
            if (IconButton(new Rect(612, 740, 56, 56), 3)) { ResetRound(); started = true; throttle = .48f; }
        }
        if (showHelp)
        {
            Box(new Rect(24, 90, 330, 140), new Color(.97f, .94f, .86f, .94f));
            GUI.Label(new Rect(40, 104, 305, 126), "Enter 啟程  /  R 重來\n↑↓ 或滑桿微調油門，空白鍵噴火\nC 循環鏡頭，1 / 2 / 3 選擇視角\n高度尺金色區域是安全氣流\nH 開關說明", label);
        }
        GUI.matrix = previous;
    }
}
