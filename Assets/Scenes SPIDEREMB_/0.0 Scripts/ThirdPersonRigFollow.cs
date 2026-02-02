using UnityEngine;

public class ThirdPersonRigFollow : MonoBehaviour
{
    [Header("Referenzen")]
    [Tooltip("Hier die Spinne (FinalSpider) reinschieben")]
    public Transform spider;

    [Header("Einstellungen")]
    [Tooltip("Abstand in Weltkoordinaten von Kamera-Rig zur Spinne beim Start")]
    public Vector3 initialOffset;

    void Start()
    {
        if (spider == null)
        {
            Debug.LogError("ThirdPersonRigFollow: keine Spider-Transform zugewiesen!", this);
            enabled = false;
            return;
        }

        // Initialen Welt-Offset berechnen
        initialOffset = transform.position - spider.position;
    }

    void LateUpdate()
    {
        // 1) Neue Position: Spider-Position + um Y-Achse rotierten Offset
        Quaternion yRot = Quaternion.Euler(0f, spider.eulerAngles.y, 0f);
        Vector3 desiredPos = spider.position + yRot * initialOffset;
        transform.position = desiredPos;

        // 2) Rig so ausrichten, dass es zur Spinne schaut (only Y-axis)
        Vector3 lookDir = spider.position - transform.position;
        lookDir.y = 0f; // keine Neigung nach oben/unten
        if (lookDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
    }
}
