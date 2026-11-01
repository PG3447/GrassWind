using System.Collections.Generic;
using UnityEngine;

public class WindArrowSystem : MonoBehaviour
{
    [Range(0f, 2f)] public float globalMultiplier = 1f;

    public bool invertX = true;

    public float gizmoScale = 3f;

    private readonly List<WindArrowNode> nodes = new List<WindArrowNode>();
    private int lastHierarchyCount = -1;
    public IReadOnlyList<WindArrowNode> Nodes => nodes;

    private void Awake()
    {
        RefreshNodes();
    }

    public void RefreshNodes()
    {
        nodes.Clear();
        Collect(transform);
        lastHierarchyCount = transform.hierarchyCount;
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

    private Vector2 MapDirection(Vector2 d)
    {
        if (invertX) d.x = -d.x;
        return d;
    }

    public void Inject(InstanceGrass grass, WindFluidSim fluid, float dt)
    {
        if (transform.hierarchyCount != lastHierarchyCount) RefreshNodes();

        foreach (var node in nodes)
        {
            if (node == null || !node.isActiveAndEnabled) continue;
            Transform child = node.transform;
            Vector2 a = grass.WorldToCell(child.parent.position);
            Vector2 b = grass.WorldToCell(child.position);

            Vector2 d = b - a;
            if (d.sqrMagnitude < 1e-6f) continue;

            fluid.AddSegment(a, b, node.radiusCells, node.strength * globalMultiplier, MapDirection(d.normalized), dt);
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