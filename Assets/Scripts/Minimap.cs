using UnityEngine;

public class Minimap : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform mapImage;      // The giant map image inside the mask
    public RectTransform playerIcon;    // The arrow icon

    [Header("Map Settings")]
    [Tooltip("The X and Z world coordinates that the center of your map image represents. (Based on your camera position!)")]
    public Vector2 mapWorldCenter = new Vector2(453.7f, 508f);
    
    [Tooltip("Adjust this to scale the map movement perfectly with your car.")]
    public float mapScale = 1.98f;

    // The transform of the local car
    public static Transform LocalPlayer;

    void Update()
    {
        if (LocalPlayer == null) return;

        // 1. Calculate player's offset from the center of the world map
        Vector3 playerPos = LocalPlayer.position;
        float offsetX = playerPos.x - mapWorldCenter.x;
        float offsetZ = playerPos.z - mapWorldCenter.y;

        // 2. Move the map image in the OPPOSITE direction so the player stays centered
        mapImage.anchoredPosition = new Vector2(-offsetX * mapScale, -offsetZ * mapScale);

        // 3. Rotate the Player Icon to match the car's rotation
        // (Unity's Y rotation goes clockwise, UI Z rotation goes counter-clockwise)
        playerIcon.localEulerAngles = new Vector3(0, 0, -LocalPlayer.eulerAngles.y + 90f);
    }
}
