using UnityEngine;

public class WindArrowNode : MonoBehaviour
{
    public const float StrengthMax = 10f;
    public const float RadiusMin = 1f, RadiusMax = 30f;

    [Range(0f, StrengthMax)] public float strength = 2f;
    [Range(RadiusMin, RadiusMax)] public float radiusCells = 8f;
}
