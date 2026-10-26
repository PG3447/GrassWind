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

        for (int y = 0; y < data.detailHeight; y++)
        {
            for (int x = 0; x < data.detailWidth; x++)
            {
                if (details[y, x] > 0)
                {
                    // Œrodek komórki siatki detali w lokalnych wspó³rzêdnych Terrain
                    float localX = (x + 0.5f) / data.detailWidth * data.size.x;
                    float localZ = (y + 0.5f) / data.detailHeight * data.size.z;

                    float worldX = terrain.transform.position.x + localX;
                    float worldZ = terrain.transform.position.z + localZ;

                    float worldY = terrain.SampleHeight(
                        new Vector3(worldX, 0, worldZ)
                    ) + terrain.transform.position.y;

                    Vector3 position = new Vector3(worldX, worldY, worldZ);

                    Debug.Log($"Detail: {position}, count: {details[y, x]}");
                }
            }
        }



    }
}
