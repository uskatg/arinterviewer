using UnityEngine;
using Meta.XR.MRUtilityKit; // Required to talk to the Room system

public class SnapToFloor : MonoBehaviour
{
    void Start()
    {
        // We can't snap immediately because the room might not be loaded yet.
        // We listen for the "SceneLoaded" event instead.
        MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
    }

    void OnSceneLoaded()
    {
        // 1. Get the current room
        MRUKRoom room = MRUK.Instance.GetCurrentRoom();

        // 2. Check if a floor was found
        if (room != null && room.FloorAnchor != null)
        {
            // 3. Get the exact Y height of the real floor
            float realFloorHeight = room.FloorAnchor.transform.position.y;

            // 4. Move this object to that height, keeping X and Z the same
            transform.position = new Vector3(transform.position.x, realFloorHeight, transform.position.z);
        }
    }
}