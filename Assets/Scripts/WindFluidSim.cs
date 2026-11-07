using UnityEngine;

public class WindFluidSim : MonoBehaviour
{
    private static WindFluidSim _instance;

    public static WindFluidSim instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<WindFluidSim>();
            return _instance;
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => _instance = null;

    public const float VorticityMax = 50f;
    public const float DissipationMin = 0.05f, DissipationMax = 5f;
    public const int IterationsMin = 5, IterationsMax = 50;
    public const float MaxSpeedMin = 0.5f, MaxSpeedMax = 10f;
    public const float ForceStrengthMax = 5f;

    [SerializeField] private ComputeShader compute;

    [Header("Parametry")]
    [Range(0f, VorticityMax)]
    public float vorticity = 1f;
    
    [Range(DissipationMin, DissipationMax)]
    public float dissipation = 1f;
    
    [Range(IterationsMin, IterationsMax)]
    public int pressureIterations = 20;
    
    //[Range(0.1f, 30f)]
    //public float advectionScale = 3f;
    
    [Range(0f, ForceStrengthMax)]
    public float forceStrength = 1f;
    
    [Range(0f, 1f)]
    public float pressureDecay = 0.0f;
    
    [Range(MaxSpeedMin, MaxSpeedMax)]
    public float maxSpeed = 2f;
    public float AdvectionMax => 30f / maxSpeed;

    public RenderTexture Velocity => velA;

    public RenderTexture Preview => preview;
    public bool previewEnabled;

    public int Size => n;
    [SerializeField]
    private int n;
    [SerializeField]
    private RenderTexture velA, velB, curl, div, presA, presB, preview;
    private int kForce, kCurl, kVort, kDiv, kScaleP, kPres, kGrad, kAdvect, kSplat, kVis;

    public void Init(int size)
    {
        Release();
        n = size;

        kForce = compute.FindKernel("Force");
        kCurl = compute.FindKernel("Curl");
        kVort = compute.FindKernel("Vorticity");
        kDiv = compute.FindKernel("Divergence");
        kScaleP = compute.FindKernel("ScalePressure");
        kPres = compute.FindKernel("Pressure");
        kGrad = compute.FindKernel("Gradient");
        kAdvect = compute.FindKernel("Advect");
        kSplat = compute.FindKernel("Splat");
        kVis = compute.FindKernel("Visualize");

        velA = Make(RenderTextureFormat.RGFloat);
        velB = Make(RenderTextureFormat.RGFloat);
        curl = Make(RenderTextureFormat.RFloat);
        div = Make(RenderTextureFormat.RFloat);
        presA = Make(RenderTextureFormat.RFloat);
        presB = Make(RenderTextureFormat.RFloat);
        preview = Make(RenderTextureFormat.ARGB32);
    }

    private RenderTexture Make(RenderTextureFormat format)
    {
        var rt = new RenderTexture(n, n, 0, format)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        rt.Create();

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = prev;
        return rt;
    }

    private void Dispatch(int kernel)
    {
        int groups = Mathf.CeilToInt(n / 8f);
        compute.Dispatch(kernel, groups, groups, 1);
    }

    public void AddSegment(Vector2 a, Vector2 b, float radiusCells, float strength, Vector2 dir, float dt)
    {
        dt = Mathf.Min(dt, 1f / 60f);
        compute.SetInt("N", n);
        compute.SetFloat("dt", dt);
        compute.SetVector("segA", a);
        compute.SetVector("segB", b);
        compute.SetFloat("segRadius", radiusCells);
        compute.SetFloat("segStrength", strength);
        compute.SetVector("segDir", dir);
        compute.SetTexture(kSplat, "VelIn", velA);
        compute.SetTexture(kSplat, "VelOut", velB);
        Dispatch(kSplat);
        (velA, velB) = (velB, velA);
    }

    public void Step(Texture forceTex, float dt)
    {
        dt = Mathf.Min(dt, 1f / 60f);

        compute.SetInt("N", n); // gridResolution
        compute.SetFloat("dt", dt);
        compute.SetFloat("vorticity", vorticity); // lambda
        compute.SetFloat("dissipation", dissipation); // tlumienie
        //compute.SetFloat("advectionScale", Mathf.Min(advectionScale, AdvectionMax));
        compute.SetFloat("forceStrength", forceStrength);
        compute.SetFloat("pressureDecay", pressureDecay);
        compute.SetFloat("maxSpeed", maxSpeed);

        if (forceTex != null)
        {
            compute.SetTexture(kForce, "VelIn", velA);
            compute.SetTexture(kForce, "ForceTex", forceTex);
            compute.SetTexture(kForce, "VelOut", velB);
            Dispatch(kForce);
            (velA, velB) = (velB, velA);
        }

        //velA jest startowe
        compute.SetTexture(kCurl, "VelIn", velA);
        compute.SetTexture(kCurl, "CurlOut", curl);
        Dispatch(kCurl);

        compute.SetTexture(kVort, "VelIn", velA);
        compute.SetTexture(kVort, "CurlIn", curl);
        compute.SetTexture(kVort, "VelOut", velA);
        Dispatch(kVort);

        compute.SetTexture(kDiv, "VelIn", velA);
        compute.SetTexture(kDiv, "DivOut", div);
        Dispatch(kDiv);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = presA;

        GL.Clear(false, true, Color.black);

        RenderTexture.active = previous;

        //compute.SetTexture(kScaleP, "PresIn", presA);
        //compute.SetTexture(kScaleP, "PresOut", presA);
        //Dispatch(kScaleP);
        //(presA, presB) = (presB, presA);

        compute.SetTexture(kPres, "DivIn", div);
        for (int i = 0; i < pressureIterations; i++)
        {
            compute.SetTexture(kPres, "PresIn", presA);
            compute.SetTexture(kPres, "PresOut", presB);
            Dispatch(kPres);
            (presA, presB) = (presB, presA);
        }

        compute.SetTexture(kGrad, "VelIn", velA);
        compute.SetTexture(kGrad, "PresIn", presA);
        compute.SetTexture(kGrad, "VelOut", velA);
        Dispatch(kGrad);

        compute.SetTexture(kAdvect, "VelIn", velA);
        compute.SetTexture(kAdvect, "VelOut", velA);
        Dispatch(kAdvect);

        if (previewEnabled)
        {
            compute.SetTexture(kVis, "VelIn", velA);
            compute.SetTexture(kVis, "VisOut", preview);
            Dispatch(kVis);
        }
    }

    private void Release()
    {
        foreach (var rt in new[] { velA, velB, curl, div, presA, presB, preview })
            if (rt != null) rt.Release();
    }

    private void OnDestroy() => Release();
}