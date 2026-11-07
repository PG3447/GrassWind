using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class WindArrowNode : MonoBehaviour
{
    public const float StrengthMax = 20f;
    public const float RadiusMin = 1f, RadiusMax = 300f;

    [Range(0f, StrengthMax)] public float strength = 2f;
    [Range(RadiusMin, RadiusMax)] public float radiusCells = 8f;

    private Transform arrowParent = null;

    private LineRenderer trzon = null;
    private LineRenderer grot = null;


    private Vector3 from = new Vector3(0, 1, 0);
    private Vector3 to = new Vector3(3, 1, 0);
    public float shaftWidth = 0.1f;
    public float headWidth = 0.4f;
    public bool expanded = false;
    public void Init(Transform parent)
    {
        arrowParent = parent;

        Vector3 neck = to - (to - from).normalized * 0.5f;
        trzon = CreateLine(from, neck, shaftWidth, shaftWidth);
        grot = CreateLine(neck, to, headWidth, 0f);
    }

    LineRenderer CreateLine(Vector3 a, Vector3 b, float w1, float w2)
    {
        var lr = new GameObject("Line").AddComponent<LineRenderer>();
        lr.transform.parent = arrowParent;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = Color.red;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.startWidth = w1;
        lr.endWidth = w2;
        return lr;
    }

    void SetLine(LineRenderer line, Vector3 a, Vector3 b)
    {
        if (line == null) return;
        line.SetPosition(0, a);
        line.SetPosition(1, b);
    }

    void SetArrow()
    {
        Vector3 neck = to - (to - from).normalized * 0.5f;
        SetLine(trzon, from, neck);
        SetLine(grot, neck, to);
    }

    public Vector3 GetFrom() => from;
    public Vector3 GetTo() => to;
    public void SetFromTo(Vector3 a, Vector3 b)
    {
        from = a;
        to = b;
        SetArrow();
    }

    public bool CheckAndSetArrowDistance(Vector3 mouse)
    {
        bool change = false;

        if (Vector3.Distance(mouse, from) < 0.75f)
        {
            from = new Vector3(mouse.x, 1, mouse.z);
            change = true;
        }
        else if (Vector3.Distance(mouse, to) < 0.75f)
        {
            to = new Vector3(mouse.x, 1, mouse.z);
            change = true;
        }

        if (change)
        {
            SetArrow();
            return true;
        }

        return false;
    }

}
