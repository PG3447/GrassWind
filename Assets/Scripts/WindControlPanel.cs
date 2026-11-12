using UnityEngine;
using UnityEngine.InputSystem;

public class WindControlPanel : MonoBehaviour
{

    private const float FMax = 1f;
    private const float K1Max = 0.3f;
    private const float K2Max = 0.1f;
    private const int GridMin = 100, GridMax = 1000;
    private const float procent = 1.25f;
    private const float CellMin = 0.03f, CellMax = 0.07f;

    private InstanceGrass grass;
    private WindFluidSim fluid;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;
    private WindArrowSystem arrows;

    private bool visible = true;
    private bool showPreview = false;
    private int pendingN, pendingS;
    private float pendingW, pendingH;
    private Vector2 scroll;
    private Rect window;
    private GUIStyle header;


    private void Start()
    {
        if (grass == null) grass = InstanceGrass.instance;
        if (fluid == null) fluid = WindFluidSim.instance;

        pendingN = grass.countGrass;
        pendingS = grass.gridResolution;
        pendingW = grass.sizeW;
        pendingH = grass.sizeH;
        arrows = grass.Arrows;
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
            grass.windSource = grass.windSource == InstanceGrass.WindSource.Arrows ? InstanceGrass.WindSource.Perlin: InstanceGrass.WindSource.Arrows;

        if (grass.windSource == InstanceGrass.WindSource.Perlin)
        {
            fluid.forceStrength = Slider("sila zrodla Perlin", fluid.forceStrength, 0f, WindFluidSim.ForceStrengthMax);
        }
        else
        {
            if (arrows != null)
            {
                arrows.createArrow = GUILayout.Toggle(arrows.createArrow, "Tworzenie strzalki", "Button");

                //arrows.globalMultiplier = Slider("mnoznik strzalek", arrows.globalMultiplier, 0f, 2f);
                //arrows.invertX = GUILayout.Toggle(arrows.invertX, "odwroc kierunek w osi X");
                if (arrows.Nodes.Count == 0)
                    GUILayout.Label("Brak strzalek");
                WindArrowNode toDelete = null;

                foreach (var node in arrows.Nodes)
                {
                    if (node == null) continue;
                    node.expanded = GUILayout.Toggle(node.expanded, (node.expanded ? "v " : "> ") + node.name, "Button");

                    if (node.expanded)
                    {
                        node.strength = Slider("   sila ", node.strength, 0f, WindArrowNode.StrengthMax);
                        node.radiusCells = Slider("  promien/zasieg (komorki)", node.radiusCells, WindArrowNode.RadiusMin, WindArrowNode.RadiusMax);
                        if (GUILayout.Button("   Usun strzalke"))
                            toDelete = node;

                    }
                }

                if (toDelete != null)
                    arrows.RemoveArrow(toDelete);
            }
            else
            {
                GUILayout.Label("Brak WindArrowSystem na scenie");
            }
        }
        GUILayout.Label("Ustawienia myszki", header);
        arrows.mouseStrength = Slider("sila", arrows.mouseStrength, 0f, WindArrowNode.StrengthMax * 1.5f);
        arrows.mouseRadius = Slider("promien", arrows.mouseRadius, WindArrowNode.RadiusMin, WindArrowNode.RadiusMax);

        GUILayout.Space(6);
        GUILayout.Label("Symulacja Wiatru", header);
        fluid.vorticity = Slider("lambda wirowosc", fluid.vorticity, 0f, WindFluidSim.VorticityMax);
        fluid.dissipation = Slider("lambda dysypacja", fluid.dissipation, WindFluidSim.DissipationMin, WindFluidSim.DissipationMax);
        fluid.pressureIterations = Mathf.RoundToInt(Slider("iteracje cisnienia", fluid.pressureIterations, WindFluidSim.IterationsMin, WindFluidSim.IterationsMax));
        //fluid.advectionScale = Slider("adwekcja (s)", fluid.advectionScale, 0.1f, fluid.AdvectionMax);

        GUILayout.Space(6);
        showPreview = GUILayout.Toggle(showPreview, "Podglad wiatru", "Button");

        if (showPreview && fluid.Preview != null)
        {
            Rect r = GUILayoutUtility.GetRect(256, 256, GUILayout.ExpandWidth(false));
            GUI.DrawTexture(r, fluid.Preview, ScaleMode.ScaleToFit, false);
        }

        GUILayout.Space(6);
        GUILayout.Label("Trawa", header);
        grass.strength = Slider("F sila wiatru", grass.strength, 0f, FMax);
        grass.elasticity = Slider("lambda elastycznosc", grass.elasticity, 0f, 1f);
        grass.k1 = Slider("k1 obnizenie Y", grass.k1, 0f, K1Max);
        grass.k2 = Slider("k2 wychylenie Z", grass.k2, -K2Max, K2Max);

        GUILayout.Space(6);
        GUILayout.Label("Siatka", header);


        pendingN = (int)Slider("n ilosc trawy", pendingN, 10000, 200000);
        pendingS = (int)Slider("s rozmiar siatki", pendingS, GridMin, GridMax);

        float newW = Slider("w szer. komorki", pendingW, CellMin, CellMax);
        float newH = Slider("h wys. komorki", pendingH, CellMin, CellMax);

        if (newW != pendingW)
        {
            pendingW = newW;
            pendingH = Mathf.Clamp(pendingH, pendingW / procent, pendingW * procent);
            pendingH = Mathf.Clamp(pendingH, CellMin, CellMax);
        }
        else if (newH != pendingH)
        {
            pendingH = newH;
            pendingW = Mathf.Clamp(pendingW, pendingH / procent, pendingH * procent);
            pendingW = Mathf.Clamp(pendingW, CellMin, CellMax);
        }

        if (GUILayout.Button("Zastosuj uklad (s, w, h)"))
        {
            grass.ApplyLayout(pendingN, pendingS, pendingW, pendingH);
            pendingN = grass.countGrass;
            pendingS = grass.gridResolution;
            pendingW = grass.sizeW;
            pendingH = grass.sizeH;
        }


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
