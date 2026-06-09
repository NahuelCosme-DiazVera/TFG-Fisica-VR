using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
[RequireComponent(typeof(XRGrabInteractable))]
public class VRArrowInfo : MonoBehaviour
{
    public TMP_Text informationText;
    public GameObject canvas;
    public float magneticFieldMagnitude;

    private Vector3 initialPosition;
    private float distanceToCamera;
    public float arrowX;
    public float arrowY;
    public float Bx;
    public float By;
    private XRGrabInteractable grabInteractable;
    private XRBaseInteractor currentInteractor;

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
        canvas.SetActive(true);
        UpdateInformationText();
    }

    private void OnReleased(SelectExitEventArgs args) {
        currentInteractor = null;
        if (informationText != null) {
                canvas.SetActive(false);
            }
    }

    void Update() {
        if (informationText != null && informationText.gameObject.activeSelf) {
            canvas.transform.LookAt(canvas.transform.position + Camera.main.transform.rotation * Vector3.forward,
            Camera.main.transform.rotation * Vector3.up);
            
            float distance = Vector3.Distance(transform.position, Camera.main.transform.position);
            canvas.transform.localScale = Vector3.one * (distance * 0.0005f);
        }
    }

    void UpdateInformationText() {
        string fieldMagnitude = magneticFieldMagnitude.ToString("E2");
        string BxString = Bx.ToString("E2");
        string ByString = By.ToString("E2");
        string[] split = fieldMagnitude.Split('E');
        string[] splitBx = BxString.Split('E');
        string[] splitBy = ByString.Split('E');

        informationText.text = $"B: {split[0]} x 10<sup>{int.Parse(split[1])}</sup> T\n" +
        $"Bx: {splitBx[0]} x 10<sup>{int.Parse(splitBx[1])}</sup> T\n" +
        $"By: {splitBy[0]} x 10<sup>{int.Parse(splitBy[1])}</sup> T\n" +
        $"X: {arrowX.ToString("0.###")}\n " +
        $"Y: {arrowY.ToString("0.###")}";
    }

    public bool isBeingGrabbed() {
        return currentInteractor != null;
    }
}
