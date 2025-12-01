using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeDoor : Interactable
{
    public int SceneIndex = 2;
    public override void Interact()
    {
        base.Interact();

        SoundManager.PlaySound(SoundType.DOOR, GetComponent<AudioSource>(), 1);

        LoadSceneAsync.Instance.LoadScene(SceneIndex);
    }
}
