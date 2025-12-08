using UnityEngine;

[CreateAssetMenu(fileName = "New Map", menuName = "Map Select/Map Data")]
public class MapData : ScriptableObject
{
    [Header("Map Info")]
    public string mapName;
    public string mapDisplayName;
    public string sceneName; // Scene to load when this map is selected
    
    [Header("Visual Assets")]
    public Sprite mapPreviewImage;
    
    [Header("Map Settings")]
    public string mapDescription;
}
