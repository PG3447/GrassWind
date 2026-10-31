using UnityEngine;

public class WindControlPanel : MonoBehaviour
{

    private const float FMax = 1f;
    private const float K1Max = 0.3f;
    private const float K2Max = 0.1f;
    private const int GridMin = 10, GridMax = 1000;

    [SerializeField] private InstanceGrass grass;
    [SerializeField] private WindFluidSim fluid;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    private bool visible = true;
    private int pendingN;
    private float pendingW, pendingH;
    private Vector2 scroll;
    private Rect window;
    private GUIStyle header;

    private void Awake()
    {
        if (grass == null) grass = GetComponent<InstanceGrass>();
        if (fluid == null) fluid = GetComponent<WindFluidSim>();
    }

    private void Start()
    {
        pendingN = grass.GridResolution;
        pendingW = grass.SizeW;
        pendingH = grass.SizeH;
        window = new Rect(10, 10, 400, Mathf.Min(720f, Screen.height - 20f));
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

        window = GUILayout.Window(7731, window, Draw, "Wiatr i trawa");
    }

    private void Draw(int id)
    {
        header ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        scroll = GUILayout.BeginScrollView(scroll);

        GUILayout.Label("Wiatr", header);
        if (GUILayout.Button($"Zrodlo: {grass.windSource}"))
            grass.windSource = grass.windSource == InstanceGrass.WindSource.Arrows
                ? InstanceGrass.WindSource.Perlin
                : InstanceGrass.WindSource.Arrows;

        if (grass.windSource == InstanceGrass.WindSource.Perlin)
        {
            fluid.forceStrength = Slider("sila zrodla Perlin", fluid.forceStrength, 0f, WindFluidSim.ForceStrengthMax);
        }
        else
        {
            WindArrowSystem arrows = grass.Arrows;
            if (arrows != null)
            {
                arrows.globalMultiplier = Slider("mnoznik strzalek", arrows.globalMultiplier, 0f, 2f);
                arrows.invertZ = GUILayout.Toggle(arrows.invertZ, "odwroc kierunek w osi Z");
                foreach (var node in arrows.Nodes)
                {
                    if (node == null) continue;
                    GUILayout.Label(node.name);
                    node.strength = Slider("   sila", node.strength, 0f, WindArrowNode.StrengthMax);
                    node.radiusCells = Slider("   zasieg (komorki)", node.radiusCells, WindArrowNode.RadiusMin, WindArrowNode.RadiusMax);
                }
            }
        }

        GUILayout.Space(6);
        GUILayout.Label("Symulacja Wiatru", header);
        fluid.vorticity = Slider("lambda wirowosc", fluid.vorticity, 0f, WindFluidSim.VorticityMax);
        fluid.dissipation = Slider("lambda dysypacja", fluid.dissipation, WindFluidSim.DissipationMin, WindFluidSim.DissipationMax);
        fluid.pressureIterations = Mathf.RoundToInt(Slider("iteracje cisnienia", fluid.pressureIterations, WindFluidSim.IterationsMin, WindFluidSim.IterationsMax));
        fluid.advectionScale = Slider("adwekcja (s)", fluid.advectionScale, 0.1f, fluid.AdvectionMax);

        GUILayout.Space(6);
        GUILayout.Label("Trawa", header);
        grass.strength = Slider("F sila wiatru", grass.strength, 0f, FMax);
        grass.elasticity = Slider("lambda elastycznosc", grass.elasticity, 0f, 1f);
        grass.k1 = Slider("k1 obnizenie Y", grass.k1, 0f, K1Max);
        grass.k2 = Slider("k2 wychylenie Z", grass.k2, -K2Max, K2Max);

        GUILayout.Space(6);
        GUILayout.Label("Siatka (pkt 3.1) - po zmianie kliknij Zastosuj", header);
        pendingN = Mathf.RoundToInt(Slider("s rozmiar siatki", pendingN, GridMin, GridMax) / 10f) * 10;
        pendingN = Mathf.Clamp(pendingN, GridMin, GridMax);

        float ideal = grass.TerrainSize.x / pendingN;
        pendingW = Slider("w szer. komorki", pendingW, ideal * 0.25f, ideal * 4f);
        pendingH = Slider("h wys. komorki", pendingH, ideal * 0.25f, ideal * 4f);
        GUILayout.Label($"obszar siatki:");

        if (GUILayout.Button("Dopasuj w, h do terenu"))
            pendingW = pendingH = ideal;
        if (GUILayout.Button("Zastosuj uklad (s, w, h)"))
            grass.ApplyLayout(pendingN, pendingW, pendingH);


        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    private static float Slider(string label, float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {value:0.###}", GUILayout.Width(190));
        value = GUILayout.HorizontalSlider(value, min, max);
        GUILayout.EndHorizontal();
        return value;
    }
}
