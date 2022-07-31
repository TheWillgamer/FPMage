using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fireball : MonoBehaviour
{
    [SerializeField] private float explosion_radius = 1f;
    [SerializeField] private float knockback_amount = 1f;
    [SerializeField] private float knockback_growth = 20f;
    private void OnCollisionEnter(Collision collision)
    {
        Explode();
        Destroy(gameObject);
    }
    private void Explode()
    {
        foreach(Collider col in Physics.OverlapSphere(transform.position, explosion_radius))
        {
            if(col.gameObject.tag == "Player")
            {
                PlayerHealth ph = col.gameObject.GetComponent<PlayerHealth>();
                ph.TakeDamage(15);
                ph.Knockback(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized + Vector3.up, knockback_amount, knockback_growth);
            }
        }
    }
}
