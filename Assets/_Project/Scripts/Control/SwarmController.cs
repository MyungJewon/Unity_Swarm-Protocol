using UnityEngine;
using UnityEngine.EventSystems;

public class SwarmController : MonoBehaviour
{
    [Header("References")]
    public Transform targetObj;
    public LegionRenderer legion;
    public LayerMask waterLayer;
    
    [Header("Food Settings")]
    public GameObject foodPrefab;
    public int requiredFishCount = 500;

    private GameObject currentFoodVisual;

    void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
        if (CameraRig.Instance != null && CameraRig.Instance.isDragging) return;
        if (Input.GetMouseButtonUp(0))
        {
            SpawnFood();
        }

        // 4. 먹이 위치 동기화
        if (legion.hasFood && currentFoodVisual != null)
        {
            targetObj.position = currentFoodVisual.transform.position;

            if (legion.gatheredCount >= requiredFishCount)
            {
                RemoveFood();
            }
        }
        else if (legion.hasFood && currentFoodVisual == null)
        {
            legion.hasFood = false;
        }
    }

    void SpawnFood()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, waterLayer))
        {
            targetObj.position = hit.point;
            legion.hasFood = true;

            if (currentFoodVisual != null) Destroy(currentFoodVisual);

            if (foodPrefab != null)
            {
                currentFoodVisual = Instantiate(foodPrefab, hit.point, Quaternion.identity);
            }
        }
    }

    void RemoveFood()
    {
        legion.hasFood = false;
        if (currentFoodVisual != null)
        {
            Destroy(currentFoodVisual);
        }
    }
}