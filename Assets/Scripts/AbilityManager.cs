using FishNet.Object;
using FishNet.Managing.Logging;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class AbilityManager : NetworkBehaviour
{
    [SerializeField] private GameObject fb;
    [SerializeField] private GameObject ls;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float proj_force;
    [SerializeField] private float dashForce;
    [SerializeField] private float dashDur;

    /* keeps track of spells the player has equipped
    0   - Nothing
    1   - Fireball
    2   - LightningStrike
    3   - 
    4   - 
    5   - 
    6   - 
    7   -
    8   -
    9   - Dash
    10  - Teleport
    11  - Charge
    12  - 
    13  -
    14  -
    15  - */
    public int[] equipped = new int[5];

    AudioSource m_shootingSound;
    Movement mv;

    #region cooldowns
    //Fireball
    private int fb_charges;
    [SerializeField] private float fb_cd;
    private float fb_offcd;

    //LightningStrike
    [SerializeField] private float ls_cd;
    private float ls_offcd;

    //WindDash
    [SerializeField] private float dash_cd;
    private float dash_offcd;
    #endregion

    #region UI
    [SerializeField] GameObject cdRepresentation;
    [SerializeField] Image[] Fireball;
    [SerializeField] Image Lightning;
    [SerializeField] Image Wind;
    #endregion

    public override void OnStartClient()
    {
        base.OnStartClient();
        if(IsOwner)
            cdRepresentation.SetActive(true);
    }

    void Start()
    {
        m_shootingSound = GetComponent<AudioSource>();
        mv = GetComponent<Movement>();
        fb_charges = 3;
        fb_offcd = Time.deltaTime;
        ls_offcd = Time.deltaTime;
        dash_offcd = Time.deltaTime;

        // Unequips all spells
        for (int i = 0; i < 5; i++)
            equipped[i] = 0;
    }

    private void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            if (IsOwner && fb_charges>0)
            {
                shootFireball();
                fb_charges--;
                if (fb_charges == 2)
                {
                    fb_offcd = Time.time + fb_cd;
                }
            }
        }
        if (Input.GetButtonDown("Fire2"))
        {
            if (IsOwner && Time.time > ls_offcd)
            {
                fireLightning();
                ls_offcd = Time.time + ls_cd;
            }
        }
        if (Input.GetButtonDown("Fire3"))
        {
            if (IsOwner && Time.time > dash_offcd)
            {
                setDashing();
                if (!IsServer)
                {
                    //mv.h_dashing = true;
                    mv.dashModifier = dashForce;
                    mv.dashDuration = dashDur;
                }
                dash_offcd = Time.time + dash_cd;
            }
        }
        if (IsOwner)
        {
            if(fb_charges < 3 && Time.time > fb_offcd)
            {
                fb_charges += 1;
                fb_offcd = Time.time + fb_cd;
            }
            UpdateUI();
        }
    }

    [ObserversRpc]
    private void playShootSound(){
        m_shootingSound.Play();
    }

    [ServerRpc]
    private void shootFireball()
    {
        playShootSound();
        GameObject spawned = Instantiate(fb, proj_spawn.position, proj_spawn.rotation);
        Physics.IgnoreCollision(spawned.GetComponent<Collider>(), GetComponent<Collider>());

        UnitySceneManager.MoveGameObjectToScene(spawned.gameObject, gameObject.scene);
        base.Spawn(spawned);
        
        Projectile proj = spawned.GetComponent<Projectile>();
        proj.Initialize(base.TimeManager.GetPreciseTick(TickType.Tick), proj_spawn.forward * proj_force, base.Owner.ClientId);
    }

    [ServerRpc]
    private void fireLightning()
    {
        GameObject spawned = Instantiate(ls, proj_spawn.position, proj_spawn.rotation);

        UnitySceneManager.MoveGameObjectToScene(spawned.gameObject, gameObject.scene);
        base.Spawn(spawned);

        spawned.GetComponent<LightningStrike>().projSpawn = proj_spawn;
    }

    [ServerRpc]
    private void setDashing()
    {
        Movement mv = GetComponent<Movement>();
        //mv.h_dashing = true;
        mv.dashModifier = dashForce;
        mv.dashDuration = dashDur;
    }

    private void UpdateUI()
    {
        for(int i = 0; i < 3; i++)
        {
            Fireball[i].gameObject.SetActive(fb_charges > i);
        }
        Wind.fillAmount = 1 - (dash_offcd - Time.time) / dash_cd;
        Lightning.fillAmount = 1 - (ls_offcd - Time.time) / ls_cd;
    }
}
