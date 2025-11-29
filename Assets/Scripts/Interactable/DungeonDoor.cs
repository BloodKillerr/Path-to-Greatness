using UnityEngine;

public class DungeonDoor : Interactable
{
    [SerializeField] private Direction direction;
    private RoomController roomController;
    private void Start()
    {
        roomController = GetComponentInParent<RoomController>();
    }

    public override void Interact()
    {
        base.Interact();

        if(Player.Instance.GetComponent<PlayerController>().PlayerState.IsStateGroundedState(Player.Instance.GetComponent<PlayerController>().PlayerState.CurrentPlayerMovementState))
        {
            roomController.MovePlayerThroughDoor(direction);
        }

        SoundManager.PlaySound(SoundType.DOOR, GetComponent<AudioSource>(), .5f);
    }
}
