using UnityEngine;
using UnityEngine.Rendering;

public class InstanceGrass : MonoBehaviour
{
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
    private int gridResolution; // to takie s z 3.2

    private int seed = 12347;
    private float sizeSquare;

    private Vector2[] windData;
    private Texture2D windTexture; // posiada wektory predkosc wiatru w danej komorce
    private System.Random random;

    private Matrix4x4[] matrices;
    private RenderParams renderParams;

    public Mesh mesh;
    public Material material;

    public float strength = 0.2f;

    //Dane do wyslania na GPU
    //pozycje posiada
    private Vector2 centerTerrain;
    private int side;
    private float spacing;
    //private Vector2 sizeWH;


    private void Awake()
    {
        random = new System.Random(seed);
        renderParams = new RenderParams(material)
        {
            shadowCastingMode = ShadowCastingMode.On,
            receiveShadows = true,
            layer = gameObject.layer,
            worldBounds = new Bounds(terrain.transform.position + terrain.terrainData.size / 2f, terrain.terrainData.size)
        };

        sizeSquare = gridResolution * Mathf.Max(sizeW, sizeH);

        windTexture = new Texture2D(gridResolution, gridResolution, TextureFormat.RGFloat, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        windData = new Vector2[gridResolution * gridResolution];
    }

    void Start()
    {

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

    void Update()
    {
        float t = Time.time * 0.5f;

        for (int z = 0; z < gridResolution; z++)
        {
            for (int x = 0; x < gridResolution; x++)
            {
                float noise = Mathf.PerlinNoise(
                    x * 0.1f + t,
                    z * 0.1f
                );

                // Kierunek od 0 do 360 stopni
                float angle = noise * Mathf.PI * 2f;

                Vector2 dir = new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                );

                // Si³a wiatru
                float n = Mathf.InverseLerp(
                    0.25f,
                    0.75f,
                    Mathf.PerlinNoise(x * 0.1f - t + 1000f, z * 0.1f)
                );

                float strength = Mathf.Lerp(0.5f, 2f, n);

                windData[z * gridResolution + x] = dir * strength;
            }
        }
        windTexture.SetPixelData(windData, 0);
        windTexture.Apply(false);

        Shader.SetGlobalFloat("_ExternalForce", strength);
        Shader.SetGlobalFloat("_Side", side);
        Shader.SetGlobalFloat("_Spacing", spacing);
        Shader.SetGlobalFloat("_GridResolution", gridResolution);
        Shader.SetGlobalVector("_SizeWSizeH", new Vector2(sizeW, sizeH));
        Shader.SetGlobalTexture("_WindTexture", windTexture);


        Graphics.RenderMeshInstanced(renderParams, mesh, 0, matrices);   
    }
}
