using UnityEngine;

public class IgnorePlayerCollision : MonoBehaviour
{
    public Collider2D player1Collider;
    public Collider2D player2Collider;
    public Collider2D player3Collider;
    public Collider2D player4Collider;

    void Start()
    {
        // Ignore collisions between all players
        if (player1Collider != null && player2Collider != null)
        {
            Physics2D.IgnoreCollision(player1Collider, player2Collider);
        }
        
        if (player1Collider != null && player3Collider != null)
        {
            Physics2D.IgnoreCollision(player1Collider, player3Collider);
        }
        
        if (player1Collider != null && player4Collider != null)
        {
            Physics2D.IgnoreCollision(player1Collider, player4Collider);
        }
        
        if (player2Collider != null && player3Collider != null)
        {
            Physics2D.IgnoreCollision(player2Collider, player3Collider);
        }
        
        if (player2Collider != null && player4Collider != null)
        {
            Physics2D.IgnoreCollision(player2Collider, player4Collider);
        }
        
        if (player3Collider != null && player4Collider != null)
        {
            Physics2D.IgnoreCollision(player3Collider, player4Collider);
        }
    }
}
