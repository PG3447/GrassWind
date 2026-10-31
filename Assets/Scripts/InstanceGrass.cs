using UnityEngine;
using UnityEngine.Rendering;

public class InstanceGrass : MonoBehaviour
{
    public enum WindSource { Arrows, Perlin }

    [SerializeField]
    private GameObject prefab;

    [SerializeField]
    private Terrain terrain;

    [SerializeField]
    private int countGrass;

    [SerializeField]
    private float sizeW;

    [SerializeField]
    private float sizeH;

    [SerializeField]
    private int gridResolution;

    [SerializeField]
    private WindFluidSim fluid;

    [SerializeField]
    private WindArrowSystem arrows;

    public WindSource windSource = WindSource.Arrows;

    private int seed = 12347;
    private float sizeSquare;

    private Vector2[] windData;
    private Texture2D windTexture;
    private System.Random random;

    private Matrix4x4[] matrices;
    private RenderParams renderParams;

    public Mesh mesh;
    public Material material;

    public float strength = 0.2f;

    //Dane do wyslania na GPU
    //pozycje posiada
    public float k1 = 0.05f;
    public float k2 = 0.1f;
    public float elasticity = 0.2f;

    public float SizeW => sizeW;
    public float SizeH => sizeH;
    public int GridResolution => gridResolution;
    public float CellSize => Mathf.Max(sizeW, sizeH);
    public Vector3 TerrainSize => terrain.terrainData.size;
    public WindArrowSystem Arrows => arrows;

    private Vector2 centerTerrain;
    private int side;
    private float spacing;
    //private Vector2 sizeWH;


    private void Awake()
    {
        if (fluid == null) fluid = GetComponent<WindFluidSim>();
        if (arrows == null) arrows = FindFirstObjectByType<WindArrowSystem>();
        if (arrows == null)
            arrows = new GameObject("WindArrows").AddComponent<WindArrowSystem>();

        random = new System.Random(seed);
        renderParams = new RenderParams(material)
        {
            shadowCastingMode = ShadowCastingMode.On,
            receiveShadows = true,
            layer = gameObject.layer,
            worldBounds = new Bounds(terrain.transform.position + terrain.terrainData.size / 2f, terrain.terrainData.size)
        };

        InitWind();
    }

    private void InitWind()
    {
        sizeSquare = gridResolution * Mathf.Max(sizeW, sizeH);

        if (windTexture != null) Destroy(windTexture);

        windTexture = new Texture2D(gridResolution, gridResolution, TextureFormat.RGFloat, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        windData = new Vector2[gridResolution * gridResolution];

        fluid.Init(gridResolution);
    }

    public void ApplyLayout(int newGridResolution, float newW, float newH)
    {
        gridResolution = Mathf.Clamp(newGridResolution, 10, 1000);
        sizeW = newW;
        sizeH = newH;
        InitWind();
        BuildGrass();
    }

    void Start()
    {
        BuildGrass();
    }

    private void BuildGrass()
    {
        random = new System.Random(seed);

        centerTerrain = new Vector2(terrain.transform.position.x + terrain.terrainData.size.x / 2f, terrain.transform.position.z + terrain.terrainData.size.z / 2f);

        side = Mathf.CeilToInt(Mathf.Sqrt(countGrass));
        spacing = sizeSquare / (float)side;

        matrices = new Matrix4x4[countGrass];

        for (int i = 0; i < countGrass; i++)
        {
            float x = centerTerrain.x + (i % side - side / 2f) * spacing;
            float z = centerTerrain.y + (i / side - side / 2f) * spacing;

            float noiseX = (float)random.NextDouble() * spacing;
            float noiseZ = (float)random.NextDouble() * spacing;

            Vector3 position = new Vector3(x + noiseX, 0f, z + noiseZ);

            //Vector2 indeksNaSiatce = new Vector2((int)((position.x - centerTerrain.x + side / 2f * spacing) / sizeW + 0.5f), (int)((position.z - centerTerrain.y + side / 2f * spacing) / sizeH + 0.5f));
            //Debug.Log("Indeks " +indeksNaSiatce + " pozycja " + position); 

            position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
            float rotationY = Mathf.PerlinNoise(position.x * 0.3f + 200f, position.z * 0.3f + 200f) * 60f + Random.Range(-15f, 15f);

            float n = Mathf.PerlinNoise(position.x * 0.5f, position.z * 0.5f) * 0.7f + Mathf.PerlinNoise(position.x * 3f + 50f, position.z * 3f + 50f) * 0.3f;

            float h = Mathf.Lerp(0.5f, 1.4f, n);

            matrices[i] = Matrix4x4.TRS(position, Quaternion.Euler(0f, rotationY, 0f), new Vector3(1f, h, 1f));

            //Instantiate(prefab, position, Quaternion.identity);
        }

        Shader.SetGlobalVector("_CenterTerrain", centerTerrain);
    }

    public Vector2 WorldToCell(Vector3 world)
    {
        float half = sizeSquare / 2f;
        return new Vector2((world.x - centerTerrain.x + half) / sizeW + 0.5f,
                           (world.z - centerTerrain.y + half) / sizeH + 0.5f);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (windSource == WindSource.Perlin)
        {
            float t = Time.time * 0.5f;

            for (int z = 0; z < gridResolution; z++)
            {
                for (int x = 0; x < gridResolution; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.1f + t, z * 0.1f);

                    float angle = noise * Mathf.PI * 2f;

                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                    float n = Mathf.InverseLerp(0.25f, 0.75f, Mathf.PerlinNoise(x * 0.1f - t + 1000f, z * 0.1f));
                    float windSpeed = Mathf.Lerp(0.5f, 2f, n);

                    windData[z * gridResolution + x] = dir * windSpeed;
                }
            }
            windTexture.SetPixelData(windData, 0);
            windTexture.Apply(false);

            fluid.Step(windTexture, dt);
        }
        else
        {
            if (arrows != null) arrows.Inject(this, fluid, dt);
            fluid.Step(null, dt);
        }

        Shader.SetGlobalFloat("_ExternalForce", strength);
        Shader.SetGlobalFloat("_Side", side);
        Shader.SetGlobalFloat("_Spacing", spacing);
        Shader.SetGlobalFloat("_GridResolution", gridResolution);
        Shader.SetGlobalVector("_SizeWSizeH", new Vector2(sizeW, sizeH));
        Shader.SetGlobalTexture("_WindTexture", fluid.Velocity);

        Shader.SetGlobalFloat("_K1", k1);
        Shader.SetGlobalFloat("_K2", k2);
        Shader.SetGlobalFloat("_Elasticity", elasticity);

        Graphics.RenderMeshInstanced(renderParams, mesh, 0, matrices);
    }

}
