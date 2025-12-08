using UnityEngine;

/// <summary>
/// CloudParallax creates horizontal drifting movement for cloud layers
/// with adjustable speed for a smooth, floating effect.
/// </summary>
public class CloudParallax : MonoBehaviour
{
    [SerializeField] private float movingSpeed = 0.3f;
    
    private Vector3 startPosition;
    private float elapsedTime = 0f;
    private bool isMoving = true;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        if (!isMoving) return;

        elapsedTime += Time.deltaTime;

        // Smooth horizontal drifting motion
        float horizontalOffset = Mathf.Sin(elapsedTime * movingSpeed) * 1.5f;
        
        transform.position = startPosition + new Vector3(horizontalOffset, 0, 0);
    }

    public void SetMovingSpeed(float speed)
    {
        movingSpeed = speed;
    }

    public void SetMoving(bool moving)
    {
        isMoving = moving;
        if (!moving)
        {
            transform.position = startPosition;
            elapsedTime = 0f;
        }
    }

    public void ResetPosition()
    {
        transform.position = startPosition;
        elapsedTime = 0f;
    }
}
