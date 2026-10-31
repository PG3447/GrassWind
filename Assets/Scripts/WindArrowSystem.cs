using System.Collections.Generic;
using UnityEngine;

public class WindArrowSystem : MonoBehaviour
{
    [Range(0f, 2f)] public float globalMultiplier = 1f;

    public bool invertZ = true;

    public float gizmoScale = 3f;

    private readonly List<WindArrowNode> nodes = new List<WindArrowNode>();
    public IReadOnlyList<WindArrowNode> Nodes => nodes;

    private void Awake()
    {
        if (transform.childCount == 0) CreateDefaults();
        RefreshNodes();
    }

    private void Reset()
    {
        CreateDefaults();
    }

    [ContextMenu("Utworz domyslne strzalki")]
    public void CreateDefaults()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
            else DestroyImmediate(transform.GetChild(i).gameObject);
        }

        Terrain t = Terrain.activeTerrain;
        Vector3 c = t != null ? t.transform.position + t.terrainData.size * 0.5f : Vector3.zero;
        float s = t != null ? t.terrainData.size.x : 100f;

        Vector3 P(float dx, float dz)
        {
            Vector3 p = new Vector3(c.x + dx * s, 0f, c.z + dz * s);
            p.y = (t != null ? t.SampleHeight(p) + t.transform.position.y : 0f) + 2f;
            return p;
        }

        transform.position = P(-0.3f, 0f);

        Transform a = NewEnd("Koniec A", transform, P(0f, 0f));
        NewEnd("Koniec B", a, P(0.3f, 0.05f));
        NewEnd("Koniec C", transform, P(-0.05f, 0.2f));
    }

    private static Transform NewEnd(string name, Transform parent, Vector3 worldPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = worldPos;
        go.AddComponent<WindArrowNode>();
        return go.transform;
    }

    public void RefreshNodes()
    {
        nodes.Clear();
        Collect(transform);
    }

    private void Collect(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (!child.TryGetComponent(out WindArrowNode node))
                node = child.gameObject.AddComponent<WindArrowNode>();
            nodes.Add(node);
            Collect(child);
        }
    }

    public void Inject(InstanceGrass grass, WindFluidSim fluid, float dt)
    {
        float flipY = invertZ ? -1f : 1f;
        foreach (var node in nodes)
        {
            if (node == null || !node.isActiveAndEnabled) continue;
            Transform child = node.transform;
            Vector2 a = grass.WorldToCell(child.parent.position);
            Vector2 b = grass.WorldToCell(child.position);
            fluid.AddSegment(a, b, node.radiusCells, node.strength * globalMultiplier, flipY, dt);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, gizmoScale * 0.4f);
        DrawTree(transform);
    }

    private void DrawTree(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Gizmos.color = Color.cyan;
            Vector3 a = parent.position;
            Vector3 b = child.position;
            Gizmos.DrawLine(a, b);
            Gizmos.DrawWireSphere(b, gizmoScale * 0.3f);

            Vector3 dir = b - a;
            dir.y = 0f;
            if (dir.sqrMagnitude > 1e-4f)
            {
                dir.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, dir);
                Gizmos.DrawLine(b, b - dir * gizmoScale + right * gizmoScale * 0.5f);
                Gizmos.DrawLine(b, b - dir * gizmoScale - right * gizmoScale * 0.5f);
            }

            DrawTree(child);
        }
    }
}
