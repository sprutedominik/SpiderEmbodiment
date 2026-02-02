using UnityEngine;

public class CreateFloorMaterial : MonoBehaviour
{
    void Start()
    {
        // Neues Material im Unlit-Shader erzeugen
        Material floorMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));

        // Farbe setzen (hier Grau, wie dein Boden)
        floorMat.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.5f, 1f));

        // Material anwenden
        var rend = GetComponent<MeshRenderer>();
        rend.material = floorMat;

        // Schatten deaktivieren
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        // Cube abflachen
        Vector3 s = transform.localScale;
        s.y = 0.01f;
        transform.localScale = s;

        // Minimales Offset nach oben
        transform.position += new Vector3(0, 0.001f, 0);
    }
}