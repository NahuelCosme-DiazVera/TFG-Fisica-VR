using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
[RequireComponent(typeof(XRGrabInteractable))]
public class VRWireDrag : MonoBehaviour
{
    public MagneticFieldVisualizer visualizer;
    public int wireIndex;
    public bool isDoubleWire;
    private XRGrabInteractable grabInteractable;
    private XRBaseInteractor currentInteractor;
    [SerializeField] private XRBaseInteractor arrowInteractor;

    void Awake() {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    void OnDestroy() {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args) {
        currentInteractor = args.interactorObject as XRBaseInteractor;
        visualizer.SelectWire(wireIndex, gameObject);
        if (arrowInteractor != null) {
            arrowInteractor.enabled = false;
        }
    }

    private void OnReleased(SelectExitEventArgs args) {
        currentInteractor = null;
        if (isDoubleWire) {
            Vector3 newPosition = transform.position;
            float newX = newPosition.x;
            float newY = newPosition.y;
            visualizer.SetDoubleWirePosition(wireIndex, newX, newY, this.gameObject, true);
        }
        visualizer.UnselectCurrentWire();
        if (arrowInteractor != null) {
            arrowInteractor.enabled = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (currentInteractor != null) {
            Vector3 newPosition = transform.position;
            float newX = newPosition.x;
            float newY = newPosition.y;
            if (!isDoubleWire) {
                visualizer.SetWirePositionX(newX);
                visualizer.SetWirePositionY(newY);
            }
            else {
                visualizer.SetDoubleWirePosition(wireIndex, newX, newY, this.gameObject, false);
            }
        }
    }
}
