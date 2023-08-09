using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
//using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class a_flameslash : NetworkBehaviour
{
    [SerializeField] private int damage = 15;
    [SerializeField] private float knockback_amount = 1f;
    [SerializeField] private float knockback_growth = 20f;

    [SerializeField] private Transform proj_spawn;
    [SerializeField] Image Slash;

    // shows the slash on screen
    [SerializeField] private GameObject ownerSlashGM;
    private ParticleSystem ownerSlash;
    [SerializeField] private GameObject clientSlashGM;
    private ParticleSystem clientSlash;

    #region cooldowns
    [SerializeField] private float slash_cd;
    private float slash_offcd;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        slash_offcd = Time.deltaTime;
        ownerSlash = ownerSlashGM.GetComponent<ParticleSystem>();
        clientSlash = clientSlashGM.GetComponent<ParticleSystem>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetButtonDown("Fire2") && Time.time > slash_offcd)
        {
            DoDamage(base.Owner.ClientId);
            slash_offcd = Time.time + slash_cd;
        }
        UpdateUI();
    }

    [ServerRpc]
    private void DoDamage(int owner)
    {
        showSlash();
        Collider[] hitColliders = Physics.OverlapBox(proj_spawn.position, new Vector3(2f, 1f, 2f), proj_spawn.rotation);
        foreach (var hit in hitColliders)
        {
            if (hit.transform.tag == "Player" && hit.transform.parent.GetComponent<NetworkObject>().Owner.ClientId != owner)
            {
                PlayerHealth ph = hit.transform.gameObject.GetComponent<PlayerHealth>();
                ph.Knockback(proj_spawn.rotation * Vector3.forward, knockback_amount, knockback_growth);
                ph.TakeDamage(damage);
                ph.startFire();
            }
            else if (hit.transform.tag == "Projectile")
            {
                hit.GetComponent<Projectile>().Reflect(proj_spawn.rotation * Vector3.forward, owner);
            }
        }
    }

    [ObserversRpc]
    private void showSlash()
    {
        if (base.IsOwner)
        {
            ownerSlash.Play();
        }
        else
        {
            clientSlashGM.transform.rotation = proj_spawn.rotation;
            clientSlash.Play();
        }
    }

    private void UpdateUI()
    {
        Slash.fillAmount = 1 - (slash_offcd - Time.time) / slash_cd;
    }
}
