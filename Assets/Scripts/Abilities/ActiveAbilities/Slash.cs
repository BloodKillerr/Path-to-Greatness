using UnityEngine;

public enum Axis
{
    X, Y, Z
}

[CreateAssetMenu(
    fileName = "Slash",
    menuName = "Abilities/Active/Slash"
)]
public class Slash : Ability
{
    public GameObject AttackPrefab;

    public Vector3 SpawnOffset = Vector3.zero;

    public bool RandomizeRotation = true;
    public Axis RotationAxis = Axis.Y;
    public float MinAngle = 0f;
    public float MaxAngle = 360f;
    public bool AxisIsLocal = true;

    public int mpCost = 10;

    public override void Use()
    {
        if (AttackPrefab == null)
        {
            Debug.LogWarning("Slash.Use: AttackPrefab not assigned.");
            return;
        }

        if(Player.Instance.GetComponent<PlayerStats>().UseMP(mpCost))
        {
            Transform spawnPoint = AbilityUseContext.SpawnPoint;

            Vector3 pos;
            Quaternion baseRot;

            if (spawnPoint != null)
            {
                pos = spawnPoint.position + spawnPoint.TransformVector(SpawnOffset);
                baseRot = spawnPoint.rotation;
            }
            else
            {
                pos = SpawnOffset;
                baseRot = Quaternion.identity;
            }

            Quaternion finalRot = baseRot;

            if (RandomizeRotation)
            {
                float angle = Random.Range(MinAngle, MaxAngle);
                Vector3 axisVec;

                if (AxisIsLocal && spawnPoint != null)
                {
                    switch (RotationAxis)
                    {
                        case Axis.X:
                            axisVec = spawnPoint.TransformDirection(Vector3.right);
                            break;
                        case Axis.Y:
                            axisVec = spawnPoint.TransformDirection(Vector3.up);
                            break;
                        default:
                            axisVec = spawnPoint.TransformDirection(Vector3.forward);
                            break;
                    }
                }
                else
                {
                    switch (RotationAxis)
                    {
                        case Axis.X:
                            axisVec = Vector3.right;
                            break;
                        case Axis.Y:
                            axisVec = Vector3.up;
                            break;
                        default:
                            axisVec = Vector3.forward;
                            break;
                    }
                }

                Quaternion rand = Quaternion.AngleAxis(angle, axisVec);
                finalRot = rand * baseRot;
            }

            GameObject spawned = Instantiate(AttackPrefab, pos, finalRot);
        }
    }
}
