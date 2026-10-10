using UnityEngine;

public class TerrainGrassPosition : MonoBehaviour
{
public Terrain terrain;
    public int detailLayer = 0;

    void Start()
    {
        if (terrain == null)
            terrain = Terrain.activeTerrain;

        if (terrain == null)
        {
            Debug.LogError("Nie znaleziono Terrain!");
            return;
        }

        TerrainData data = terrain.terrainData;

        int[,] details = data.GetDetailLayer(
            0, 0,
            data.detailWidth,
            data.detailHeight,
            detailLayer
        );

        int count = 0;

        for (int y = 0; y < data.detailHeight; y++)
        {
            for (int x = 0; x < data.detailWidth; x++)
            {
                int density = details[y, x];

                for (int i = 0; i < density; i++)
                {
                    float normalizedX =
                        (x + Random.value) / data.detailWidth;

                    float normalizedZ =
                        (y + Random.value) / data.detailHeight;

                    float worldX =
                        terrain.transform.position.x +
                        normalizedX * data.size.x;

                    float worldZ =
                        terrain.transform.position.z +
                        normalizedZ * data.size.z;

                    float worldY =
                        terrain.SampleHeight(
                            new Vector3(worldX, 0, worldZ)
                        ) + terrain.transform.position.y;

                    Vector3 position = new Vector3(worldX, worldY, worldZ);

                    count++;
                }
            }
        }

        Debug.Log($"Liczba odczytanych instancji: {count}");
    }
}
