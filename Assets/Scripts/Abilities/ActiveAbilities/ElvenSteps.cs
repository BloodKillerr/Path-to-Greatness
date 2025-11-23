using System.Collections;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Elven Steps",
    menuName = "Abilities/Active/Elven Steps"
)]
public class ElvenSteps : Ability
{
    public int boostAmount = 2;
    public float duration = 5f;
    public int mpCost = 10;

    public override void Use()
    {
        PlayerController controller = Player.Instance.GetComponent<PlayerController>();

        if(Player.Instance.GetComponent<PlayerStats>().UseMP(mpCost))
        {
            controller.ApplyTemporaryJumpSpeed(boostAmount, duration);
        } 
    }
}
