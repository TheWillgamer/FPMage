using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class a_lightningcrash : NetworkBehaviour
{
    [SerializeField] private int damage = 15;
    [SerializeField] private float knockback_amount = 1f;
    [SerializeField] private float knockback_growth = 20f;

    [SerializeField] private GameObject explosion;
    [SerializeField] private GameObject indicator;
    [SerializeField] private Transform cam;
    [SerializeField] private float radius;
    [SerializeField] private float timeTillStrike;
    [SerializeField] Image Crash;

    private Movement mv;
    private Vector3 crashLoc;

    #region cooldowns
    [SerializeField] private float crash_cd;
    private float crash_offcd;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        crash_offcd = Time.deltaTime;
        mv = GetComponent<Movement>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!mv.disableAB && Input.GetButtonUp("Fire2") && Time.time > crash_offcd)
        {
            // only detects walls
            int layerMask = 1 << 6;

            RaycastHit hit;
            if (Physics.Raycast(cam.position, cam.forward, out hit, 1000f, layerMask))
            {
                DamageSetup(hit.point);
                crash_offcd = Time.time + crash_cd;
            }

        }
        UpdateUI();
    }

    [ServerRpc]
    private void DamageSetup(Vector3 pos)
    {
        crashLoc = pos;
        showIndicator(crashLoc);
        Invoke("DoDamage", timeTillStrike);
    }

    private void DoDamage()
    {
        showCrash(crashLoc);

        Collider[] hitColliders = Physics.OverlapSphere(crashLoc, radius);
        foreach (var hit in hitColliders)
        {
            if (hit.transform.tag == "Player")
            {
                // knockback direction
                Vector3 dir = (hit.transform.position - crashLoc).normalized;

                PlayerHealth ph = hit.transform.gameObject.GetComponent<PlayerHealth>();
                ph.Knockback(dir.normalized, knockback_amount, knockback_growth);
                ph.TakeDamage(damage);
            }
        }
    }

    [ObserversRpc]
    private void showIndicator(Vector3 pos)
    {
        GameObject spawned = Instantiate(indicator, pos, transform.rotation);
    }

    [ObserversRpc]
    private void showCrash(Vector3 pos)
    {
        GameObject spawned = Instantiate(explosion, pos, transform.rotation);
    }

    private void UpdateUI()
    {
        Crash.fillAmount = 1 - (crash_offcd - Time.time) / crash_cd;
    }
}