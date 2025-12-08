using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Character Select/Character Data")]
public class CharacterSelectData : ScriptableObject
{
    [Header("Character Info")]
    public string characterName;
    public string characterDescription;

    [Header("Visual Assets")]
    public Sprite characterIcon;        // Small icon for selection grid
    public Sprite characterFullArt;     // Full art when locked in (static fallback)

    [Header("Character Animation")]
    public Sprite[] idleAnimationFrames;  // Array of sprites for idle animation
    public float animationSpeed = 0.1f;   // Time between frames (lower = faster)
    public bool loopAnimation = true;     // Whether to loop the animation

    [Header("Game Assets")]
    public GameObject characterPrefab;  // For spawning in game scene

    [Header("Character Stats (Optional)")]
    public int health = 100;
    public int speed = 5;
    public int attack = 10;
}