using UnityEngine;

[ExecuteAlways]
public class GrassWind : MonoBehaviour
{
    public float strength = 1f;
    //public Vector2 direction = new Vector2(1f, 0f);

    void Update()
    {
        Shader.SetGlobalFloat("_ExternalForce", strength);
        //Shader.SetGlobalVector("_WindDir", direction);
    }
}
