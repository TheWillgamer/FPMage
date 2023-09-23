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


    private Movement mv;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource hitted;
    [SerializeField] private AudioSource missed;

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
        mv = GetComponent<Movement>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!mv.disableAB && Input.GetButtonDown("Fire2") && Time.time > slash_offcd)
        {
            ownerSlash.Play();
            missed.Play();
            //animator.SetTrigger("Claw");
            DoDamage(proj_spawn.position, proj_spawn.rotation, base.Owner.ClientId);
            slash_offcd = Time.time + slash_cd;
        }
        UpdateUI();
    }

    [ServerRpc]
    private void DoDamage(Vector3 pos, Quaternion rot, int owner)
    {
        bool wasHit = false;
        
        Collider[] hitColliders = Physics.OverlapBox(pos, new Vector3(2f, 1f, 2f), rot);
        foreach (var hit in hitColliders)
        {
            if (hit.transform.tag == "Player" && hit.transform.parent.GetComponent<NetworkObject>().Owner.ClientId != owner)
            {
                wasHit = true;
                PlayerHealth ph = hit.transform.gameObject.GetComponent<PlayerHealth>();
                ph.Knockback(rot * Vector3.forward, knockback_amount, knockback_growth);
                ph.TakeDamage(damage);
                ph.startFire();
            }
            else if (hit.transform.tag == "Projectile")
            {
                hit.GetComponent<Projectile>().Reflect(rot * Vector3.forward, owner);
            }
        }

        setUpSlash(wasHit);
    }

    [ObserversRpc]
    private void setUpSlash(bool wasHit)
    {
        if (wasHit)
            hitted.Play();
        else if (!base.IsOwner)
        {
            missed.Play();
            clientSlashGM.transform.rotation = proj_spawn.rotation;
            Invoke("showSlash", .1f);
            animator.SetTrigger("Claw");
        }
    }

    private void showSlash()
    {
        clientSlash.Play();
    }

    private void UpdateUI()
    {
        Slash.fillAmount = 1 - (slash_offcd - Time.time) / slash_cd;
    }
}
