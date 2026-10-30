using UnityEngine;

public class WindFluidSim : MonoBehaviour
{
    [SerializeField] private ComputeShader compute;

    [Header("Parametry")]
    [Range(10f, 100f)] public float vorticity = 50f; 
    [Range(0f, 5f)] public float dissipation = 0.2f;
    [Range(1, 60)] public int pressureIterations = 20;
    [Range(0.1f, 100f)] public float advectionScale = 20f;
    [Range(0f, 20f)] public float forceStrength = 5f;
    [Range(0f, 1f)] public float pressureDecay = 0.8f;
    [Range(0.1f, 20f)] public float maxSpeed = 2f;

    public RenderTexture Velocity => velA;
    public int Size => n;

    private int n;
    private RenderTexture velA, velB, curl, div, presA, presB;
    private int kForce, kCurl, kVort, kDiv, kScaleP, kPres, kGrad, kAdvect;

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

        velA = Make(RenderTextureFormat.RGFloat);
        velB = Make(RenderTextureFormat.RGFloat);
        curl = Make(RenderTextureFormat.RFloat);
        div = Make(RenderTextureFormat.RFloat);
        presA = Make(RenderTextureFormat.RFloat);
        presB = Make(RenderTextureFormat.RFloat);
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

    public void Step(Texture forceTex, float dt)
    {
        dt = Mathf.Min(dt, 1f / 30f);

        compute.SetInt("N", n);
        compute.SetFloat("dt", dt);
        compute.SetFloat("vorticity", vorticity);
        compute.SetFloat("dissipation", dissipation);
        compute.SetFloat("advectionScale", advectionScale);
        compute.SetFloat("forceStrength", forceStrength);
        compute.SetFloat("pressureDecay", pressureDecay);
        compute.SetFloat("maxSpeed", maxSpeed);

        compute.SetTexture(kForce, "VelIn", velA);
        compute.SetTexture(kForce, "ForceTex", forceTex);
        compute.SetTexture(kForce, "VelOut", velB);
        Dispatch(kForce);
        (velA, velB) = (velB, velA);

        compute.SetTexture(kCurl, "VelIn", velA);
        compute.SetTexture(kCurl, "CurlOut", curl);
        Dispatch(kCurl);

        compute.SetTexture(kVort, "VelIn", velA);
        compute.SetTexture(kVort, "CurlIn", curl);
        compute.SetTexture(kVort, "VelOut", velB);
        Dispatch(kVort);
        (velA, velB) = (velB, velA);

        compute.SetTexture(kDiv, "VelIn", velA);
        compute.SetTexture(kDiv, "DivOut", div);
        Dispatch(kDiv);

        compute.SetTexture(kScaleP, "PresIn", presA);
        compute.SetTexture(kScaleP, "PresOut", presB);
        Dispatch(kScaleP);
        (presA, presB) = (presB, presA);

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
        compute.SetTexture(kGrad, "VelOut", velB);
        Dispatch(kGrad);
        (velA, velB) = (velB, velA);

        compute.SetTexture(kAdvect, "VelIn", velA);
        compute.SetTexture(kAdvect, "VelOut", velB);
        Dispatch(kAdvect);
        (velA, velB) = (velB, velA);
    }

    private void Release()
    {
        foreach (var rt in new[] { velA, velB, curl, div, presA, presB })
            if (rt != null) rt.Release();
    }

    private void OnDestroy() => Release();
}
