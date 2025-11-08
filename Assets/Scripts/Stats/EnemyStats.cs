using UnityEngine;

public class EnemyStats : CharacterStats
{
    [SerializeField] private Stat movementSpeed;

    public Stat MovementSpeed { get => movementSpeed; set => movementSpeed = value; }
}
