using UnityEngine;

public class WindControlPanel : MonoBehaviour
{
    [SerializeField] private InstanceGrass grass;
    [SerializeField] private WindFluidSim fluid;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    [SerializeField] private Vector2 cellSizeRange = new Vector2(0.1f, 10f);

    private bool visible = true;
    private int pendingN;
    private Rect window = new Rect(10, 10, 360, 0);

    private void Start()
    {
        pendingN = grass.GridResolution;
    }

    private void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == toggleKey)
        {
            visible = !visible;
            e.Use();
        }
        if (!visible) return;

        window = GUILayout.Window(7731, window, Draw, "Wiatr i trawa (F1)");
    }

    private void Draw(int id)
    {
        GUILayout.Label("--- Wiatr ---");
        fluid.vorticity = Slider("lambda wirowosc", fluid.vorticity, 10f, 100f);
        fluid.dissipation = Slider("lambda dysypacja", fluid.dissipation, 0f, 5f);
        fluid.pressureIterations = Mathf.RoundToInt(Slider("iteracje cisnienia", fluid.pressureIterations, 1, 60));
        fluid.advectionScale = Slider("adwekcja (s)", fluid.advectionScale, 0.1f, 100f);
        fluid.forceStrength = Slider("sila zrodla", fluid.forceStrength, 0f, 20f);
        fluid.maxSpeed = Slider("max predkosc", fluid.maxSpeed, 0.1f, 20f);

        GUILayout.Label("--- Trawa ---");
        grass.strength = Slider("F sila wiatru", grass.strength, 0f, 20f);
        grass.elasticity = Slider("lambda elastycznosc", grass.elasticity, 0f, 1f);
        grass.k1 = Slider("k1 obnizenie Y", grass.k1, 0f, 0.2f);
        grass.k2 = Slider("k2 wychylenie Z", grass.k2, -0.1f, 0.1f);

        GUILayout.Label("--- Siatka ---");
        grass.SizeW = Slider("w szer. komorki", grass.SizeW, cellSizeRange.x, cellSizeRange.y);
        grass.SizeH = Slider("h wys. komorki", grass.SizeH, cellSizeRange.x, cellSizeRange.y);
        pendingN = Mathf.RoundToInt(Slider("s rozmiar siatki", pendingN, 10, 1000));

        if (GUILayout.Button("Zastosuj uklad (s, w, h)"))
            grass.ApplyLayout(pendingN);

        GUI.DragWindow();
    }

    private static float Slider(string label, float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {value:0.###}", GUILayout.Width(170));
        value = GUILayout.HorizontalSlider(value, min, max);
        GUILayout.EndHorizontal();
        return value;
    }
}
