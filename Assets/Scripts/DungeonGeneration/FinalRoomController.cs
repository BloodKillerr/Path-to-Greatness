using UnityEngine;

public class FinalRoomController : RoomController
{
    public Transform PortalSpawnPoint;

    public void SpawnPortal(GameObject portalPrefab, int nextScene)
    {
        GameObject go = Instantiate(portalPrefab, PortalSpawnPoint.transform.position,
                PortalSpawnPoint.transform.rotation, gameObject.transform);

        SceneChangeDoor door = go.GetComponent<SceneChangeDoor>();

        door.SceneIndex = nextScene;
    }
}
