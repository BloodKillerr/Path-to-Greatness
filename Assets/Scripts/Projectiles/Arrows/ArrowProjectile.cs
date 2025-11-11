using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    public float Speed = 20f;

    public int Damage = 20;

    private void Update()
    {
        transform.Translate(Vector3.forward * Speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats playerStats = Player.Instance.GetComponent<PlayerStats>();
            playerStats.TakeDamage(Damage);
        }
        Destroy(gameObject);
    }
}
