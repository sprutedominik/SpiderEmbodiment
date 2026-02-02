using UnityEngine;
using UnityEngine.InputSystem;

public class BlockRightControllerSecondaryButton : MonoBehaviour
{
    private InputAction blockB;

    void OnEnable()
    {
        // Create high-priority InputAction that "captures" the B button
        blockB = new InputAction(type: InputActionType.Button);
        
        // Correct binding for the Meta Quest Right Controller "B" button
        blockB.AddBinding("<XRController>{RightHand}/secondaryButton");

        // When pressed → do nothing, swallow it
        blockB.performed += ctx =>
        {
            Debug.Log("[BLOCK] Right Controller B button suppressed.");
        };

        blockB.Enable();
    }

    void OnDisable()
    {
        blockB.Disable();
    }
}
