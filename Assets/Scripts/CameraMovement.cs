using UnityEngine;
using UnityEngine.EventSystems;

public class CameraMovement : MonoBehaviour
{

    public Transform player;
    public float movementSpeed = 5.0f;
    public float horizontalRotationSpeed = 2.0f;
    public float verticalRotationSpeed = 2.0f;

    private float rotationX = 0.0f;
    private float rotationY = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        HandleCursorLock();

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            HandleMovement();
            HandleRotation();
        }
        
    }

    private void HandleCursorLock()
    {
        if (EventSystem.current.IsPointerOverGameObject() || Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
        }
        else if (Input.GetMouseButtonDown(0)) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void HandleMovement()
    {
        Vector3 direction = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        if (direction.magnitude > 1)
        {
            direction.Normalize();
        }
        Vector3 movement = player.TransformDirection(direction);
        player.position += movement * movementSpeed * Time.deltaTime;
    }

    private void HandleRotation()
    {

        float mouseX = Input.GetAxis("Mouse X") * horizontalRotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * verticalRotationSpeed;

        rotationY += mouseX;
        player.localRotation = Quaternion.Euler(0f, rotationY, 0f);
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        player.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }
}
