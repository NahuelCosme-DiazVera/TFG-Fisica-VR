using UnityEngine;
using TMPro;

public class ArrowInfo : MonoBehaviour
{

    public TMP_Text informationText;
    public GameObject canvas;
    public float magneticFieldMagnitude;

    private Vector3 initialPosition;
    private bool isDragging = false;
    private Camera mainCamera;
    private float distanceToCamera;

    public float arrowX;
    public float arrowY;
    public float Bx;
    public float By;

    void Start() {
        mainCamera = Camera.main;
        if (canvas != null) {
            canvas.SetActive(false);
        }
    }

    void Update() {
        if (informationText != null && informationText.gameObject.activeSelf) {
            canvas.transform.LookAt(canvas.transform.position + mainCamera.transform.rotation * Vector3.forward,
            mainCamera.transform.rotation * Vector3.up);
            
            float distance = Vector3.Distance(transform.position, mainCamera.transform.position);
            canvas.transform.localScale = Vector3.one * (distance * 0.0005f);
        }
    }

    void OnMouseEnter() {
        if (informationText != null && Cursor.lockState == CursorLockMode.None) {
            canvas.SetActive(true);
            UpdateInformationText();
        }
    }

    void OnMouseExit() {
        if (informationText != null && !isDragging) {
            canvas.SetActive(false);
        }
    }

    void OnMouseDown() {
        if (Cursor.lockState == CursorLockMode.None) {
            initialPosition = transform.localPosition;
            isDragging = true;
            distanceToCamera = mainCamera.WorldToScreenPoint(transform.position).z;
        }
    }

    void OnMouseDrag() {
        if (isDragging) {
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = distanceToCamera;
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
            transform.position = worldPosition;
            canvas.transform.position = worldPosition;

            UpdateInformationText();
        } 
    }

    void OnMouseUp() {
       if (isDragging) {
            transform.localPosition = initialPosition;
            canvas.transform.localPosition = initialPosition;
            isDragging = false;
            if (informationText != null) {
                canvas.SetActive(false);
            }
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
        return isDragging;
    }
}
