using UnityEngine;

public class CameraRig : MonoBehaviour
{
    public static CameraRig Instance;

    [Header("Target Settings")]
    public Transform targetObj;
    public Vector3 targetOffset = new Vector3(0, 1, 0);

    [Header("Distance Settings")]
    public float distance = 20.0f;
    public float minDistance = 5.0f;
    public float maxDistance = 60.0f;
    public float zoomSpeed = 2.0f;
    public float zoomDampening = 5.0f;

    [Header("Rotation Settings")]
    public float xSpeed = 200.0f;
    public float ySpeed = 200.0f;
    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;
    public float rotationDampening = 5.0f;

    private float xDeg = 0.0f;
    private float yDeg = 0.0f;
    private float currentDistance;
    private float desiredDistance;
    private Quaternion currentRotation;
    private Quaternion desiredRotation;
    private Quaternion rotation;
    private Vector3 position;

    public bool isDragging { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InitCamera();
    }

    void InitCamera()
    {
        if (!targetObj)
        {
            GameObject go = new GameObject("Cam Target");
            go.transform.position = transform.position + (transform.forward * distance);
            targetObj = go.transform;
        }

        distance = Vector3.Distance(transform.position, targetObj.position);
        currentDistance = distance;
        desiredDistance = distance;

        position = transform.position;
        rotation = transform.rotation;
        currentRotation = transform.rotation;
        desiredRotation = transform.rotation;

        xDeg = Vector3.Angle(Vector3.right, transform.right);
        yDeg = Vector3.Angle(Vector3.up, transform.up);
    }

    void LateUpdate()
    {
        if (!targetObj) return;
        HandleInput();
        CalculatePosition();
    }

    void HandleInput()
    {
        isDragging = false;
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            float dx = Input.GetAxis("Mouse X");
            float dy = Input.GetAxis("Mouse Y");

            if (Mathf.Abs(dx) > 0.01f || Mathf.Abs(dy) > 0.01f)
            {
                isDragging = true;
                xDeg += dx * xSpeed * 0.02f;
                yDeg -= dy * ySpeed * 0.02f;
            }
        }
        else if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            Vector2 delta = Input.GetTouch(0).deltaPosition;
            if (delta.sqrMagnitude > 1.0f) 
            {
                isDragging = true;
                xDeg += delta.x * xSpeed * 0.005f;
                yDeg -= delta.y * ySpeed * 0.005f;
            }
        }

        if (Input.touchCount == 2)
        {
            isDragging = true;
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
            desiredDistance += deltaMagnitudeDiff * zoomSpeed * 0.01f;
        }
        else
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                desiredDistance -= scroll * Time.deltaTime * zoomSpeed * Mathf.Abs(desiredDistance);
            }
        }

        yDeg = ClampAngle(yDeg, yMinLimit, yMaxLimit);
        desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
    }

    void CalculatePosition()
    {
        desiredRotation = Quaternion.Euler(yDeg, xDeg, 0);
        currentRotation = Quaternion.Lerp(currentRotation, desiredRotation, Time.deltaTime * rotationDampening);
        currentDistance = Mathf.Lerp(currentDistance, desiredDistance, Time.deltaTime * zoomDampening);

        position = targetObj.position - (currentRotation * Vector3.forward * currentDistance + targetOffset);

        transform.position = position;
        transform.rotation = currentRotation;
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360) angle += 360;
        if (angle > 360) angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }
}