using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing.Logging;
using FishNet;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlayerHealth : NetworkBehaviour
{
    private int hp;  //keeps track of player health
    private int lives;  //keeps track of player lives
    public Image dmgScreen;
    private Rigidbody rb;
    private Movement mv;
    private Dash dh;
    private bool alive;
    private bool moving;
    private GameplayManager gm;
    [SerializeField] private Transform cam;
    private float respawnTime = 1f;
    private float dropDownTime = 3f;
    private float invulnerableTime = 2f;

    public bool invulnerable;

    // OnFire
    private int onFire;
    private int fireDmg = 2;
    private float fire_cd = 1.15f;
    private float fire_offcd;
    [SerializeField] private GameObject baseFireGM;
    private ParticleSystem baseFire;
    [SerializeField] private GameObject fireExplosionGM;
    private ParticleSystem fireExplosion;
    [SerializeField] private ParticleSystem shield;

    //Audio
    public AudioSource hit;
    public AudioSource burning;
    public AudioSource burningBurst1;
    public AudioSource burningBurst2;
    private bool burstplayed;           // Switch burst sounds

    // Damage Blinking White
    [SerializeField] private SkinnedMeshRenderer rend;
    List<Color> originalColors = new List<Color>();

    // Death Splash
    [SerializeField] private GameObject deathSplash;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mv = GetComponent<Movement>();
        dh = GetComponent<Dash>();
        gm = GameObject.FindWithTag("GameplayManager").GetComponent<GameplayManager>();
        onFire = 0;
        InstanceFinder.TimeManager.OnTick += TimeManager_OnTick;

        foreach (Material mat in rend.materials)
        {
            originalColors.Add(mat.color);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        hp = 0;
        lives = 4;
        baseFire = baseFireGM.GetComponent<ParticleSystem>();
        fireExplosion = fireExplosionGM.GetComponent<ParticleSystem>();
        alive = true;
        moving = true;
        invulnerable = false;
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

        if (!base.IsOwner)
            return;

        if (!moving && (Input.GetButtonDown("Jump") || Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0) || (Input.GetButtonDown("Fire1") || Input.GetButtonDown("Fire2") || Input.GetButtonDown("Fire3") || Input.GetButtonDown("Fire4")))
        {
            moving = true;
            ReenableGravity();
            CancelInvoke("ReenableGravity");
        }
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
        if (invulnerable)
            return;

        onFire = 3;
        fire_offcd = fire_cd / 2f;
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
        burning.Play();
        burstplayed = false;
        if (base.IsOwner)
            return;
        baseFireGM.SetActive(true);
        baseFire.Play();
    }

    [ObserversRpc]
    private void endFireGM()
    {
        burning.Stop();
        if (base.IsOwner)
            return;
        baseFireGM.SetActive(false);
    }

    [ObserversRpc]
    private void fireBurstGM()
    {
        if (burstplayed)
            burningBurst2.Play();
        else
            burningBurst1.Play();
        burstplayed = !burstplayed;

        if (base.IsOwner)
            return;
        fireExplosionGM.transform.position = transform.position;
        fireExplosion.Play();
    }

    // Reduces life count by 1
    [ObserversRpc]
    private void LoseLife(int life)
    {
        if (base.IsOwner)
            gm.SetLives(true, life);
        else
            gm.SetLives(false, life);

        if (life <= 0)
        {
            Camera c = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
            StartCoroutine(FreezeCam(c));
            gm.EndGame(base.IsOwner);
        }
    }

    IEnumerator FreezeCam(Camera c)
    {
        c.clearFlags = CameraClearFlags.Nothing;
        yield return null;
        c.cullingMask = 0;
    }

    // Reduces hp based on the parameter amount
    public void TakeDamage(int amt)
    {
        if (invulnerable)
            return;

        int oldHp = hp;
        hp += amt;

        if (base.IsServer)
        {
            ObserversTakeDamage(amt, oldHp);
            if (base.IsOwner)
            {
                StartCoroutine(HitBlink());
                gm.playerHp.text = hp.ToString() + "%";
                if (amt > 3)
                    StartCoroutine(Fade());
            }
            else
            {
                gm.oppoHp.text = hp.ToString() + "%";
                StartCoroutine(HitBlink());
            }
        }
    }

    // Knocks back the player in a given direction: kb_growth determines how much percentage determines the knockback amount
    public void Knockback(Vector3 direction, float base_kb, float kb_growth)
    {
        if (invulnerable)
            return;

        if (dh != null)
            dh.CancelDash();

        rb.velocity = Vector3.zero;
        rb.AddForce(Vector3.up * base_kb / 2f, ForceMode.Impulse);
        if (mv.grounded && direction.y < 0.1f)
        {
            Vector3 newDir = new Vector3(direction.x, 0f, direction.z);
            float force = (((hp / kb_growth) + 1) * base_kb);
            rb.AddForce((newDir.normalized / 2f + newDir / 2f) * force, ForceMode.Impulse);
        }
        else if (direction.y > 0f)
        {
            direction = direction + 2 * Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            rb.AddForce(direction.normalized * (((hp / kb_growth) + 1) * base_kb), ForceMode.Impulse);
        }
        else
            rb.AddForce(direction * (((hp / kb_growth) + 1) * base_kb), ForceMode.Impulse);
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
            gm.playerHp.text = hp.ToString() + "%";
            if (value > 3)
                StartCoroutine(Fade());
        }
        else
        {
            gm.oppoHp.text = hp.ToString() + "%";
            StartCoroutine(HitBlink());
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

    IEnumerator HitBlink()
    {
        int i;
        float multiplier;
        for (float alpha = 0f; alpha <= 1f; alpha += 0.008f)
        {
            i = 0;
            multiplier = alpha * alpha * alpha;
            foreach (Material mat in rend.materials)
            {
                mat.color = Color.white * (1f - multiplier) + originalColors[i] * multiplier;
                i++;
            }
            yield return null;
        }

    }

    public void Die()
    {
        if (base.IsServer && alive)
        {
            if (dh != null)
                dh.CancelDash();

            lives--;
            LoseLife(lives);
            SaveCam(false);
            Invoke("Respawn", respawnTime);
            alive = false;
            onFire = 0;
            endFireGM();

            DeathSplash();
        }
    }

    [ObserversRpc]
    public void DeathSplash()
    {
        Instantiate(deathSplash, transform.position, transform.rotation);
    }

    [ObserversRpc]
    // Doesnt destroy main camera with the rest of the objects
    private void SaveCam(bool respawning)
    {
        if (base.IsOwner)
        {
            Transform mct = GameObject.FindWithTag("MainCamera").transform;

            // Reset Abilities
            Ability[] abilites = GetComponents<Ability>();
            for (int i = 0; i < abilites.Length; i++)
            {
                abilites[i].Reset();
            }

            if (respawning)
            {
                mct.parent = cam;
                mct.localRotation = Quaternion.identity;
                mct.localPosition = Vector3.zero;
                moving = false;
                invulnerable = true;
                Invoke("ReenableGravity", dropDownTime);
                gm.playerHp.text = "0%";
            }
            else
            {
                mct.parent = null;
                gm.playerHp.text = "";
            }
        }
        else
        {
            if (respawning)
                gm.oppoHp.text = "0%";
            else
                gm.oppoHp.text = "";
        }
    }

    private void Respawn()
    {
        transform.position = new Vector3(0f, 40f, 0f);
        mv.gravity = false;
        rb.velocity = Vector3.zero;
        alive = true;
        invulnerable = true;
        hp = 0;
        SaveCam(true);
        ChangeShieldState(false);
    }

    [ServerRpc]
    private void ReenableGravity()
    {
        mv.gravity = true;
        CancelInvulObs();
    }

    [ObserversRpc]
    private void CancelInvulObs()
    {
        if (base.IsOwner)
        {
            Invoke("CancelInvul", invulnerableTime);
            moving = true;
        }
    }

    [ServerRpc]
    private void CancelInvul()
    {
        invulnerable = false;
        ChangeShieldState(true);
    }

    [ObserversRpc]
    private void ChangeShieldState(bool stop)
    {
        if (base.IsOwner)
            return;

        if (stop)
            shield.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        else
            shield.Play();
    }
}