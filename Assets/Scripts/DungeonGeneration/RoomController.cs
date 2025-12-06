using System.Collections;
using System.Linq;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    private RoomData data;
    private bool isFinished = false;
    private bool checkCompletion = false;
    //protected List<Enemy> Enemies = new List<Enemy>();

    public bool disableCharacterControllerWhileMoving = true;
    public bool reenableNextFrame = true;

    //public List<Enemy> Enemies { get => Enemies; set => Enemies = value; }
    public bool IsFinished { get => isFinished; set => isFinished = value; }
    public bool CheckCompletion { get => checkCompletion; set => checkCompletion = value; }
    public RoomData Data { get => data; set => data = value; }

    public void Init(RoomData data)
    {
        this.data = data;

        if (data.IsStart)
        {
            //GetComponent<EnemySpawner>().enabled = false;
            isFinished = true;
            checkCompletion = false;
        }
    }

    private void Update()
    {
        //if (checkCompletion && !isFinished && Enemies.Count == 0)
        //{
        //    isFinished = true;
        //}

        if (checkCompletion && !isFinished)
        {
            isFinished = true;
        }
    }

    private IEnumerator ReenableCharacterControllerNextFrame(CharacterController cc)
    {
        yield return null;
        if (cc != null)
        {
            cc.enabled = true;
        }
    }

    public void MovePlayerThroughDoor(Direction direction)
    {
        if (!isFinished)
        {
            return;
        }

        Vector2Int targetPos = data.Position + DirectionExtensions.ToVector2Int(direction);
        RoomData targetData = DungeonManager.Instance.GetRoomData(targetPos);
        if (targetData == null)
        {
            return;
        }

        GameObject targetRoom = DungeonManager.Instance.PlacedRooms[targetPos];
        Vector3 entrySpot = targetRoom.GetComponent<RoomController>().GetDoorEntryPosition(DirectionExtensions.Opposite(direction));

        PlayerController pc = null;

        if (Player.Instance)
        {
            pc = Player.Instance.GetComponent<PlayerController>();
        }

        if (pc == null)
        {
            return;
        }

        Transform playerTransform = pc.gameObject.transform;
        CharacterController charController = pc.CharacterController;

        if (disableCharacterControllerWhileMoving && charController != null)
        {
            charController.enabled = false;
        }

        playerTransform.position = entrySpot;

        if (charController != null)
        {
            if (reenableNextFrame)
            {
                StartCoroutine(ReenableCharacterControllerNextFrame(charController));
            }
            else
            {
                charController.enabled = true;
            }
        }

        //Player.Instance.GetComponent<PlayerController>().Rb.MovePosition(entrySpot);
        targetRoom.GetComponent<RoomController>().TriggerOnEntryToRoom();
    }

    public Vector3 GetDoorEntryPosition(Direction incoming)
    {
        string name = $"Door_{DirectionExtensions.ToShortString(incoming)}_Entry";
        Transform t = transform
        .GetComponentsInChildren<Transform>()
        .FirstOrDefault(x => x.name == name);
        return t != null ? t.position : transform.position;
    }

    private void TriggerOnEntryToRoom()
    {
        if (data.IsStart)
        {
            data.HasBeenEntered = true;
            isFinished = true;
            checkCompletion = false;
            return;
        }

        if (!data.HasBeenEntered && !IsFinished)
        {
            //Spawn Objects and Enemies
            //GetComponent<EnemySpawner>()?.SpawnEnemies();
            //Enemy.OnEnemyKilled.AddListener(RemoveEnemy);
            data.HasBeenEntered = true;
            checkCompletion = true;
        }
    }

    private void OnDisable()
    {
        //Enemy.OnEnemyKilled.RemoveListener(RemoveEnemy);
    }

    //public void RemoveEnemy(Enemy enemy)
    //{
    //    if (Enemies.Contains(enemy))
    //    {
    //        Enemies.Remove(enemy);
    //    }
    //}
}
