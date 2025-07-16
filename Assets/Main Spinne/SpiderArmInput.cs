using UnityEngine;

public class SpiderArmInput : MonoBehaviour
{
    public Transform leftTarget;
    public Transform rightTarget;

    public float moveSpeed = 0.5f;

    void Update()
    {
        // 🔧 Ersetzt: horizontal/vertical aus GetAxis → manuelle Abfrage der WASD-Tasten
        float hLeft = 0f;
        float vLeft = 0f;

        if (Input.GetKey(KeyCode.A)) hLeft = -1f;
        if (Input.GetKey(KeyCode.D)) hLeft = 1f;
        if (Input.GetKey(KeyCode.W)) vLeft = 1f;
        if (Input.GetKey(KeyCode.S)) vLeft = -1f;

        // Bewegung links
        if (leftTarget != null)
        {
            Vector3 offset = new Vector3(hLeft, 0, vLeft) * moveSpeed * Time.deltaTime;
            leftTarget.Translate(offset, Space.Self);
        }

        // Rechte Hand mit Maus (vorübergehend)
        float rh = Input.GetAxis("Mouse X");
        float rv = Input.GetAxis("Mouse Y");

        if (rightTarget != null)
        {
            Vector3 offset = new Vector3(rh, 0, rv) * moveSpeed * Time.deltaTime;
            rightTarget.Translate(offset, Space.Self);
        }
    }
}
