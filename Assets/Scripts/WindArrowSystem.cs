using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WindArrowSystem : MonoBehaviour
{
    private static WindArrowSystem _instance;

    public static WindArrowSystem instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<WindArrowSystem>();
            return _instance;
        }
    }//singleton
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => _instance = null;

    [Range(0f, 2f)] public float globalMultiplier = 1f;

    public bool invertX = true;

    public float gizmoScale = 3f;

    private readonly List<WindArrowNode> nodes = new List<WindArrowNode>();
    //private int lastHierarchyCount = -1;
    public IReadOnlyList<WindArrowNode> Nodes => nodes;

    public bool createArrow = false;
    Vector3 newFrom = -Vector3.up;
    Vector3 newTo = -Vector3.up;

    Plane plane;

    [Header("Wiatr z myszki")]
    public float mouseRadius = 32f;
    public float mouseStrength = 5f;

    bool hasPrev;
    Vector2 prevCell;

    private void Awake()
    {
        //RefreshNodes();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        plane = new Plane(Vector3.up, new Vector3(0, 1, 0));

        CreateArrow(new Vector3(0, 1, 0), new Vector3(3, 1, 0));
    }

    private Vector2 MapDirection(Vector2 d)
    {
        if (invertX) d.x = -d.x;
        return d;
    }

    public void Inject(float dt)
    {
        //if (transform.hierarchyCount != lastHierarchyCount) RefreshNodes();

        foreach (var node in nodes)
        {
            if (node == null || !node.isActiveAndEnabled) continue;
            //Transform child = node.transform;
            //Vector2 a = grass.WorldToCell(child.parent.position);
            //Vector2 b = grass.WorldToCell(child.position);
            Vector2 a = InstanceGrass.instance.WorldToCell(node.GetFrom());
            Vector2 b = InstanceGrass.instance.WorldToCell(node.GetTo());

            Vector2 d = b - a;
            if (d.sqrMagnitude < 1e-6f) continue;

            WindFluidSim.instance.AddSegment(a, b, node.radiusCells, node.strength * globalMultiplier, MapDirection(d.normalized), dt);
        }
    }

    private void CreateArrow(Vector3 from, Vector3 to)
    {
        var arrow = new GameObject("Arrow");
        arrow.transform.parent = transform;
        var node = arrow.AddComponent<WindArrowNode>();
        node.Init(arrow.transform);
        node.SetFromTo(from, to);
        nodes.Add(node);
    }

    public void RemoveArrow(WindArrowNode node)
    {
        if (node == null) return;
        nodes.Remove(node);
        Destroy(node.gameObject);
    }

    void MouseWind(Mouse mouse, Vector3 mousePosition, float dt)
    {
        if (mouse == null || !mouse.leftButton.isPressed)
        {
            hasPrev = false;
            return;
        }

        Vector2 cell = InstanceGrass.instance.WorldToCell(mousePosition);

        if (hasPrev)
        {
            Vector2 d = cell - prevCell;
            if (d.sqrMagnitude > 1e-4f)
                WindFluidSim.instance.AddSegment(prevCell, cell, mouseRadius, mouseStrength, d.normalized, dt);
        }

        prevCell = cell;
        hasPrev = true;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
        bool hit = plane.Raycast(ray, out float dist);
        if (!hit) return;
        Vector3 m = hit ? ray.GetPoint(dist) : Vector3.zero;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (createArrow)
            {
                if (newFrom == -Vector3.up)
                {
                    newFrom = new Vector3(m.x, 1, m.z);
                }
                else if (newTo == -Vector3.up)
                {
                    newTo = new Vector3(m.x, 1, m.z);
                    createArrow = false;
                    CreateArrow(newFrom, newTo);
                    newFrom = -Vector3.up;
                    newTo = -Vector3.up;
                }
            }
        }

        if (mouse.leftButton.IsPressed())
        {
            foreach (var node in nodes)
            {
                if (node.CheckAndSetArrowDistance(m))
                {
                    return;
                }
            }
        }

        MouseWind(mouse, m, Time.deltaTime);
    
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }



    //public void RefreshNodes()
    //{
    //    nodes.Clear();
    //    Collect(transform);
    //    lastHierarchyCount = transform.hierarchyCount;
    //}

    //private void Collect(Transform parent)
    //{
    //    foreach (Transform child in parent)
    //    {
    //        if (!child.TryGetComponent(out WindArrowNode node))
    //            node = child.gameObject.AddComponent<WindArrowNode>();
    //        nodes.Add(node);
    //        //node.Init(parent.transform);
    //        Collect(child);
    //    }
    //}


    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireSphere(transform.position, gizmoScale * 0.4f);
    //    DrawTree(transform);
    //}

    //private void DrawTree(Transform parent)
    //{
    //    foreach (Transform child in parent)
    //    {
    //        Gizmos.color = Color.cyan;
    //        Vector3 a = parent.position;
    //        Vector3 b = child.position;
    //        Gizmos.DrawLine(a, b);
    //        Gizmos.DrawWireSphere(b, gizmoScale * 0.3f);

    //        Vector3 dir = b - a;
    //        dir.y = 0f;
    //        if (dir.sqrMagnitude > 1e-4f)
    //        {
    //            dir.Normalize();
    //            Vector3 right = Vector3.Cross(Vector3.up, dir);
    //            Gizmos.DrawLine(b, b - dir * gizmoScale + right * gizmoScale * 0.5f);
    //            Gizmos.DrawLine(b, b - dir * gizmoScale - right * gizmoScale * 0.5f);
    //        }

    //        DrawTree(child);
    //    }
    //}
}