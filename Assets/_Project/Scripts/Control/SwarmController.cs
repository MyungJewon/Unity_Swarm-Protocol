using UnityEngine;

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
        if (CameraRig.Instance != null && CameraRig.Instance.isDragging) return;

        if (Input.GetMouseButtonUp(0))
        {
            SpawnFood();
        }
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
        Debug.Log("냠냠! 먹이를 다 먹었습니다.");
        legion.hasFood = false;
        
        if (currentFoodVisual != null)
        {
            Destroy(currentFoodVisual);
        }
    }
}