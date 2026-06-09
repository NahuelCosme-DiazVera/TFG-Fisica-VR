using UnityEngine;
using UnityEngine.XR;
public class VerticalMovement : MonoBehaviour
{
    private CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void LateUpdate() {
        InputDevice rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightController.isValid) {
            if (rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 value)) {
                float verticalMovement = value.y; // Use the y-axis of the thumbstick for vertical movement
                if (Mathf.Abs(verticalMovement) > 0.1f) {
                    Vector3 move = new Vector3(0, verticalMovement, 0) * Time.deltaTime * 2f;
                    characterController.Move(move);
                }
            }
        }
    }
}
