using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
//using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class a_flameslash : NetworkBehaviour, Ability
{
    [SerializeField] private int damage = 15;
    [SerializeField] private float knockback_amount = 1f;
    [SerializeField] private float knockback_growth = 20f;

    [SerializeField] private Transform proj_spawn;

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
    private bool slashStarted;
    #endregion

    #region UI
    //[SerializeField] GameObject cdRepresentation;
    [SerializeField] Image background;
    [SerializeField] Image meter;
    [SerializeField] TMP_Text countdown;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        slash_offcd = Time.deltaTime;
        ownerSlash = ownerSlashGM.GetComponent<ParticleSystem>();
        clientSlash = clientSlashGM.GetComponent<ParticleSystem>();
        mv = GetComponent<Movement>();
        slashStarted = false;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!mv.disableAB && Input.GetButtonDown("Fire2") && Time.time > slash_offcd)
        {
            ownerSlash.Play();
            missed.Play();
            slashStarted = true;
            Invoke("slashEnded", 0.25f);

            //animator.SetTrigger("Claw");
            DoDamage(proj_spawn.position, proj_spawn.rotation, base.Owner.ClientId);
            slash_offcd = Time.time + slash_cd;
        }
        UpdateUI();
    }

    private void slashEnded()
    {
        slashStarted = false;
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
        float remainingCD = slash_offcd - Time.time;

        if (slashStarted)
        {
            background.color = new Color32(255, 190, 0, 255);
        }
        else if (remainingCD > 0)
        {
            background.color = new Color32(100, 100, 100, 255);
            meter.fillAmount = 1 - remainingCD / slash_cd;
            countdown.text = ((int)(remainingCD) + 1).ToString();
        }
        else
        {
            background.color = new Color32(255, 255, 255, 255);
            meter.fillAmount = 0;
            countdown.text = "";
        }
    }

    public void Reset()
    {
        slash_offcd = Time.time;
    }
}
