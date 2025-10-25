using UnityEngine;
using UnityEngine.Events;

public class Player : MonoBehaviour
{
    private bool isDead = false;

    private UnityEvent interactEvent = new UnityEvent();

    public bool IsDead { get => isDead; set => isDead = value; }

    public UnityEvent InteractEvent { get => interactEvent; set => interactEvent = value; }

    public static Player Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void SubscribeToInteraction(UnityAction callback)
    {
        interactEvent.AddListener(callback);
    }

    public void UnsubscribeFromInteraction(UnityAction callback)
    {
        interactEvent.RemoveListener(callback);
    }
}
