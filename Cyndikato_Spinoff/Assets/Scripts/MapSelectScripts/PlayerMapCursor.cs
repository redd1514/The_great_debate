using UnityEngine;
using UnityEngine.UI;

public class PlayerMapCursor : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Image cursorImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.2f;
    
    private Color playerColor;
    private Vector3 baseScale;
    private bool isActive = false;
    
    void Start()
    {
        baseScale = transform.localScale;
    }
    
    void Update()
    {
        if (isActive && borderImage != null)
        {
            // Pulse animation
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = baseScale * pulse;
        }
    }
    
    public void SetPlayerColor(Color color)
    {
        playerColor = color;
        
        if (cursorImage != null)
        {
            cursorImage.color = color;
        }
        
        if (borderImage != null)
        {
            borderImage.color = color;
        }
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
        gameObject.SetActive(active);
    }
    
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }
    
    public void AttachToMapOption(RectTransform mapOptionTransform)
    {
        if (mapOptionTransform != null)
        {
            transform.SetParent(mapOptionTransform, false);
            transform.localPosition = Vector3.zero;
            transform.SetAsLastSibling(); // Render on top
        }
    }
}
