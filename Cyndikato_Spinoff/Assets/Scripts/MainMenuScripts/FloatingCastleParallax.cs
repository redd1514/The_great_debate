using UnityEngine;

/// <summary>
/// FloatingCastleParallax creates subtle floating and bobbing motion
/// for the castle to make it feel alive and dynamic.
/// </summary>
public class FloatingCastleParallax : MonoBehaviour
{
    [SerializeField] private float horizontalSpeed = 1.5f;
    [SerializeField] private float horizontalRangePercent = 0.05f; // 5% of parent width
    [SerializeField] private float verticalSpeed = 1f;
    [SerializeField] private float verticalRangePercent = 0.05f; // 5% of parent height
    
    private Vector2 startPosition;
    private float elapsedTime = 0f;
    private bool isMoving = true;
    private RectTransform parentRect;
    private RectTransform rectTransform;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = transform.parent.GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
        isMoving = true;
    }

    private void Update()
    {
        if (!isMoving || parentRect == null) return;

        elapsedTime += Time.deltaTime;

        // Calculate ranges based on parent size
        float horizontalRange = parentRect.rect.width * horizontalRangePercent;
        float verticalRange = parentRect.rect.height * verticalRangePercent;

        // Smooth floating motion
        float horizontalOffset = Mathf.Sin(elapsedTime * horizontalSpeed) * horizontalRange;
        float verticalOffset = Mathf.Cos(elapsedTime * verticalSpeed) * verticalRange;

        // Apply position using anchoredPosition
        rectTransform.anchoredPosition = startPosition + new Vector2(horizontalOffset, verticalOffset);
    }

    public void SetHorizontalMovement(float speed, float rangePercent)
    {
        horizontalSpeed = speed;
        horizontalRangePercent = rangePercent;
    }

    public void SetVerticalMovement(float speed, float rangePercent)
    {
        verticalSpeed = speed;
        verticalRangePercent = rangePercent;
    }

    public void SetMoving(bool moving)
    {
        isMoving = moving;
        if (!moving && rectTransform != null)
        {
            rectTransform.anchoredPosition = startPosition;
            elapsedTime = 0f;
        }
    }

    public void ResetPosition()
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = startPosition;
            elapsedTime = 0f;
        }
    }
}
