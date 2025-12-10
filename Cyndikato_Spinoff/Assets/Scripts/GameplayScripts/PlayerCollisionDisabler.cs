using UnityEngine;

public class PlayerCollisionDisabler : MonoBehaviour
{
    void Start()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++)
        {
            var collidersA = players[i].GetComponentsInChildren<Collider2D>();
            for (int j = i + 1; j < players.Length; j++)
            {
                var collidersB = players[j].GetComponentsInChildren<Collider2D>();
                foreach (var colA in collidersA)
                {
                    if (colA.isTrigger) continue;
                    foreach (var colB in collidersB)
                    {
                        if (colB.isTrigger) continue;
                        Physics2D.IgnoreCollision(colA, colB);
                    }
                }
            }
        }
    }
}
