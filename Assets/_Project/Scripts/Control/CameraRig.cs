using UnityEngine;

public class CameraRig : MonoBehaviour
{
    public static CameraRig Instance;

    [Header("Target Settings")]
    public Transform pivot;
    public Vector3 pivotOffset = Vector3.zero;

    [Header("Control Settings")]
    public float rotateSpeed = 5.0f;
    public float zoomSpeed = 10.0f;
    public float minDistance = 10f;
    public float maxDistance = 200f;
    public float yMinLimit = 10f;
    public float yMaxLimit = 80f;

    [Header("Status")]
    public bool isDragging = false;

    private float x = 0.0f;
    private float y = 0.0f;
    private float currentDistance = 100f;

    private Vector3 lastMousePosition;
    private float dragThreshold = 5f;
    private float totalDragDistance = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        if (pivot != null)
        {
            currentDistance = Vector3.Distance(transform.position, pivot.position);
        }
    }

    void LateUpdate()
    {
        if (pivot == null) return;

        HandleInput();
        UpdateCameraPosition();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
            totalDragDistance = 0f;
            isDragging = false;
        }

        if (Input.GetMouseButton(0))
        {
            if (Input.touchCount >= 2) return;

            Vector3 delta = Input.mousePosition - lastMousePosition;
            totalDragDistance += delta.magnitude;
            if (totalDragDistance > dragThreshold)
            {
                isDragging = true;
            }

            if (isDragging)
            {
                x += Input.GetAxis("Mouse X") * rotateSpeed;
                y -= Input.GetAxis("Mouse Y") * rotateSpeed;
                y = Mathf.Clamp(y, yMinLimit, yMaxLimit);
            }

            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            Invoke("ResetDrag", 0.05f);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
            
            scroll = -deltaMagnitudeDiff * 0.01f;
            isDragging = true;
        }
        if (Mathf.Abs(scroll) > 0.001f)
        {
            currentDistance -= scroll * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }
    }

    void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -currentDistance) + (pivot.position + pivotOffset);

        transform.rotation = rotation;
        transform.position = position;
    }

    void ResetDrag()
    {
        isDragging = false;
    }
}