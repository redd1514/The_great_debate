using UnityEngine;

[CreateAssetMenu(fileName = "New Map Data", menuName = "Character Select/Map Data", order = 1)]
[System.Serializable]
public class MapData : ScriptableObject
{
    [Header("Basic Info")]
    public string mapName = "New Map";
    
    [Header("Scene Management")]
    public string sceneName = ""; // Scene to load when this map is selected
    
    [Header("Visual Assets")]
    public Sprite mapIcon;
    public GameObject mapPrefab;
    
    [Header("Description")]
    [TextArea(3, 5)]
    public string mapDescription = "Enter map description here...";
    
    [Header("Map Properties (Optional)")]
    public MapType mapType = MapType.Standard;
    public int maxPlayers = 4;
    public float mapSize = 100f;
    
    [Header("Environment Settings (Optional)")]
    public bool hasWeatherEffects = false;
    public bool hasDayNightCycle = false;
    public Color ambientColor = Color.white;
    
    // Validate the map data
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(mapName) && mapIcon != null && !string.IsNullOrEmpty(sceneName);
    }
    
    // Get display name for UI
    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(mapName) ? "Unnamed Map" : mapName;
    }
    
    // Get description with fallback
    public string GetDescription()
    {
        return string.IsNullOrEmpty(mapDescription) ? "No description available." : mapDescription;
    }
    
    // Get scene name with fallback
    public string GetSceneName()
    {
        return string.IsNullOrEmpty(sceneName) ? "GameplayScene" : sceneName;
    }
}

public enum MapType
{
    Standard,
    Arena,
    Battlefield,
    Survival,
    KingOfTheHill,
    CaptureTheFlag
}