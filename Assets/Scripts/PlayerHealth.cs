using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing.Logging;
using FishNet;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : NetworkBehaviour
{
    [SyncVar] public int hp;  //keeps track of player health
    public TMP_Text hpPercentage;
    public Image dmgScreen;
    private Rigidbody rb;
    private Movement mv;

    // OnFire
    private int onFire;
    [SerializeField] private int fireDmg;
    [SerializeField] private float fire_cd;
    private float fire_offcd;
    [SerializeField] private GameObject baseFireGM;
    private ParticleSystem baseFire;
    [SerializeField] private GameObject fireExplosionGM;
    private ParticleSystem fireExplosion;

    //Audio
    public AudioSource hit;
    public AudioSource dead;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mv = GetComponent<Movement>();
        onFire = 0;
        InstanceFinder.TimeManager.OnTick += TimeManager_OnTick;
    }

    // Start is called before the first frame update
    void Start()
    {
        hp = 0;
        baseFire = baseFireGM.GetComponent<ParticleSystem>();
        fireExplosion = fireExplosionGM.GetComponent<ParticleSystem>();
    }

    private void OnDestroy()
    {
        if (InstanceFinder.TimeManager != null)
        {
            InstanceFinder.TimeManager.OnTick -= TimeManager_OnTick;
        }
    }

    protected virtual void Update()
    {
        PerformUpdate(false);
    }

    private void TimeManager_OnTick()
    {
        PerformUpdate(true);
    }

    [Server(Logging = LoggingType.Off)]
    private void PerformUpdate(bool onTick)
    {
        /* If a tick but also host then do not
         * update. The update will occur outside of
         * OnTick, using the update loop. */
        if (onTick && base.IsHost)
            return;
        /* If not called from OnTick and is server
         * only then exit. OnTick will handle movements. */
        else if (!onTick && base.IsServerOnly)
            return;

        float delta = (onTick) ? (float)base.TimeManager.TickDelta : Time.deltaTime;

        DoFireTick(delta);
    }

    public void startFire()
    {
        onFire = 3;
        fire_offcd = fire_cd;
        startFireGM();
    }

    private void DoFireTick(float deltaTime)
    {
        fire_offcd -= deltaTime;
        if (onFire > 0 && fire_offcd <= 0f)
        {
            fireBurstGM();
            TakeDamage(fireDmg);
            onFire--;
            fire_offcd += fire_cd;
            if (onFire == 0)
                endFireGM();
        }
    }

    [ObserversRpc]
    private void startFireGM()
    {
        if (base.IsOwner)
            return;
        baseFireGM.SetActive(true);
        baseFire.Play();
    }

    [ObserversRpc]
    private void endFireGM()
    {
        if (base.IsOwner)
            return;
        baseFireGM.SetActive(false);
    }

    [ObserversRpc]
    private void fireBurstGM()
    {
        if (base.IsOwner)
            return;
        fireExplosionGM.transform.position = transform.position;
        fireExplosion.Play();
    }

    // Reduces hp based on the parameter amount
    public void TakeDamage(int amt)
    {
        int oldHp = hp;
        hp += amt;

        if (base.IsServer)
        {
            ObserversTakeDamage(amt, oldHp);
            if (base.IsOwner)
            {
                UpdateUI();
                StartCoroutine(Fade());
            }
        }
    }

    // Knocks back the player in a given direction: kb_growth determines how much percentage determines the knockback amount
    public void Knockback(Vector3 direction, float base_kb, float kb_growth)
    {
        rb.velocity = Vector3.zero;
        rb.AddForce(Vector3.up * base_kb, ForceMode.Impulse);
        if (mv.grounded && direction.y < 0.1f)
        {
            Vector3 newDir = new Vector3(direction.x, 0f, direction.z);
            float force = (((hp / kb_growth) + 1) * base_kb);
            rb.AddForce((newDir.normalized/2f + newDir/2f) * force, ForceMode.Impulse);
        }
        else if (direction.y > 0f)
        {
            direction = direction + 2 * Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            rb.AddForce(direction.normalized * (((hp / kb_growth) + 1) * base_kb), ForceMode.Impulse);
        }
    }

    [ObserversRpc]
    private void ObserversTakeDamage(int value, int priorHealth)
    {
        //Prevents endless loop
        if (base.IsServer)
            return;

        /* Set current health to prior health so that
         * in case client somehow magically got out of sync
         * this will fix it before trying to remove health. */
        hp = priorHealth;

        TakeDamage(value);
        if (base.IsOwner)
        {
            UpdateUI();
            StartCoroutine(Fade());
        }
    }

    IEnumerator Fade()
    {
        Color c = dmgScreen.color;
        for (float alpha = .2f; alpha >= 0; alpha -= 0.0015f)
        {
            c.a = alpha;
            dmgScreen.color = c;
            yield return null;
        }
    }

    private void UpdateUI()
    {
        hpPercentage.text = hp.ToString() + "%";
    }
}
