using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class SetPlayerStartingPosition : MonoBehaviour
{
    private PlayerController TargetPlayerController;

    public Vector3 StartingPosition = Vector3.zero;
    public Vector3 StartingRotation = Vector3.zero;

    public bool disableCharacterControllerWhileMoving = true;
    public bool reenableNextFrame = true;

    private void Awake()
    {
        if(Player.Instance)
        {
            TargetPlayerController = Player.Instance.GetComponent<PlayerController>();
        }

        if (TargetPlayerController == null)
        {
            return;
        }

        Transform playerTransform = TargetPlayerController.transform;
        CharacterController charController = TargetPlayerController.GetComponent<CharacterController>();

        if (disableCharacterControllerWhileMoving && charController != null)
        {
            charController.enabled = false;
        }

        playerTransform.position = StartingPosition;
        playerTransform.rotation = Quaternion.Euler(StartingRotation);

        TrySyncPlayerControllerInternalRotation(TargetPlayerController, playerTransform.rotation);

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
    }

    private IEnumerator ReenableCharacterControllerNextFrame(CharacterController cc)
    {
        yield return null;
        if (cc != null)
        {
            cc.enabled = true;
        } 
    }

    private void TrySyncPlayerControllerInternalRotation(PlayerController pc, Quaternion playerRotation)
    {
        if (pc.PlayerCamera != null)
        {
            Vector3 e = playerRotation.eulerAngles;
            pc.PlayerCamera.transform.rotation = Quaternion.Euler(0f, e.y, 0f);
        }

        Type type = typeof(PlayerController);
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        try
        {
            FieldInfo camRotField = type.GetField("cameraRotation", flags);
            if (camRotField != null)
            {
                Vector2 camRot = Vector2.zero;
                camRot.x = playerRotation.eulerAngles.y;
                camRot.y = 0f;
                camRotField.SetValue(pc, camRot);
            }

            var targetRotField = type.GetField("playerTargetRotation", flags);
            if (targetRotField != null)
            {
                Vector2 targetRot = Vector2.zero;
                targetRot.x = playerRotation.eulerAngles.y;
                targetRotField.SetValue(pc, targetRot);
            }

            var mismatchField = type.GetField("rotationMismatch", flags);
            if (mismatchField != null)
            {
                mismatchField.SetValue(pc, 0f);
            }
        }
        catch
        {
            
        }
    }
}
