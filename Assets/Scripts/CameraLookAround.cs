using UnityEngine;

public class CameraLookAround : MonoBehaviour
{
    public float sensitivity = 2f;
    public float zoomDuration = 0.3f;

    float rotationX;
    float rotationY;
    float targetFOV;
    float defaultFOV;
    Camera cam;
    bool initialized;

    void Start()
    {
        cam = GetComponent<Camera>();
        defaultFOV = cam.fieldOfView;
        targetFOV = defaultFOV;
    }

    void Update()
    {
        if (Input.anyKeyDown && Input.mousePosition.x >= 0 && Input.mousePosition.x <= Screen.width
            && Input.mousePosition.y >= 0 && Input.mousePosition.y <= Screen.height)
        {
            if (!initialized)
            {
                Vector3 currentRotation = transform.localEulerAngles;
                rotationX = currentRotation.x > 180 ? currentRotation.x - 360 : currentRotation.x;
                rotationY = currentRotation.y > 180 ? currentRotation.y - 360 : currentRotation.y;
                initialized = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Cursor.lockState == CursorLockMode.Locked && initialized)
        {
            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);
            rotationY += mouseX;

            transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
        }

        targetFOV = Input.GetMouseButton(1) ? defaultFOV / 1.5f : defaultFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime / zoomDuration);
    }
}