using UnityEngine;

public class DamageAOE : MonoBehaviour
{
    [SerializeField] private int damage = 15;
    [SerializeField] private float knockback_amount = 1f;
    [SerializeField] private float knockback_growth = 20f;

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            ph.Knockback(transform.TransformDirection(Vector3.forward), knockback_amount, knockback_growth);
            ph.TakeDamage(damage);
            ph.startFire();
        }
    }
}
