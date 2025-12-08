using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public Sprite characterSprite;
    public RuntimeAnimatorController animatorController;
    public GameObject characterPrefab;

    [TextArea(3, 5)]
    public string characterDescription;
}