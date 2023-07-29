using FishNet;
using FishNet.Object;
using FishNet.Object.Prediction;
using UnityEngine;
using System;

/*
* 
* See TransformPrediction.cs for more detailed notes.
* 
*/

public class Movement : NetworkBehaviour
{
    #region Types.
    public struct MoveData
    {
        public bool Jump;
        public float Horizontal;
        public float Vertical;
        public bool Hdash;
        public MoveData(bool jump, float horizontal, float vertical, bool hdash)
        {
            Jump = jump;
            Horizontal = horizontal;
            Vertical = vertical;
            Hdash = hdash;
        }
    }
    public struct ReconcileData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public ReconcileData(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
        }
    }
    #endregion
    
    #region Serialized.
    [SerializeField]
    private Transform cam;
    [SerializeField]
    private float jumpForce = 15f;
    [SerializeField]
    private float secondJumpForce = 15f;
    [SerializeField]
    private float wallJumpUpModifier = 1.5f;
    [SerializeField]
    private float wallJumpModifier = 1.5f;
    [SerializeField]
    private float coyoteTime = 3f;

    [SerializeField]
    private float moveSpeed = 4500f;
    public float maxSpeed = 22;
    [SerializeField]
    private bool grounded;
    [SerializeField]
    private float counterMovement = 0.175f;
    [SerializeField]
    private float airReduceAmt = 0.1f;
    [SerializeField]
    private float airMovementMultiplier = 0.5f;
    [SerializeField]
    private float maxSlopeAngle = 35f;
    #endregion

    #region Private.
    /// <summary>
    /// Rigidbody on this object.
    /// </summary>
    private Rigidbody _rigidbody;

    /// <summary>
    /// Normal to ground.
    /// </summary>
    private Vector3 normalVector = Vector3.up;

    /// <summary>
    /// Mouse control
    /// </summary>
    private float xRotation;
    private float sensitivity = 5f;
    private float sensMultiplier = 1f;

    /// <summary>
    /// Jumping
    /// </summary>
    private bool jumping = false;
    private bool readyToJump = true;
    private int jumpCharge = 1;
    private float jumpCooldown = 0.2f;
    private bool canGroundJump;

    private float threshold = 0.01f;
    private Vector2 mag;
    #endregion

    /// <summary>
    /// Dashing
    /// </summary>
    public bool h_dashing = false;          // tells movement system to dash horizontally
    public float dashModifier = 0f;         // speed of dash
    public float dashDuration = 0f;         // duration of dash

    public bool disableMV;      //Disable movement
    public bool disableCM;      //Disable counter-movement
    public bool disableAR;      //Disable air-reduction

    private bool paused;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            paused = false;
        }
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        InstanceFinder.TimeManager.OnTick += TimeManager_OnTick;
        InstanceFinder.TimeManager.OnPostTick += TimeManager_OnPostTick;
        disableMV = false;
        disableCM = false;
        disableAR = false;
        mag = new Vector2(0f, 0f);
    }

    private void OnDestroy()
    {
        if (InstanceFinder.TimeManager != null)
        {
            InstanceFinder.TimeManager.OnTick -= TimeManager_OnTick;
            InstanceFinder.TimeManager.OnPostTick -= TimeManager_OnPostTick;
        }
    }

    private void Update()
    {
        if (base.IsOwner)
        {
            if (Input.GetButtonDown("Jump") && readyToJump && jumpCharge > 0)
            {
                jumping = true;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (paused)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    paused = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    paused = true;
                }
            }
        }
    }

    private void TimeManager_OnTick()
    {
        transform.rotation = Quaternion.Euler(0.0f, cam.eulerAngles.y, 0.0f);
        mag = FindVelRelativeToLook();
        if (base.IsOwner)
        {
            Reconciliation(default, false);
            CheckInput(out MoveData md);
            Move(md, false);
        }
        if (base.IsServer)
        {
            Move(default, true);
        }
    }


    private void TimeManager_OnPostTick()
    {
        if (base.IsServer)
        {
            ReconcileData rd = new ReconcileData(transform.position, transform.rotation, _rigidbody.velocity);
            Reconciliation(rd, true);
        }
    }

    private void CheckInput(out MoveData md)
    {
        md = default;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (horizontal == 0 && vertical == 0 && !jumping && !h_dashing)
            return;

        md = new MoveData(jumping, horizontal, vertical, h_dashing);
        jumping = false;
        h_dashing = false;
    }

    [Replicate]
    private void Move(MoveData md, bool asServer, bool replaying = false)
    {
        if (disableMV)
        {
            md.Horizontal = 0;
            md.Vertical = 0;
        }

        if (md.Hdash)
        {
            _rigidbody.velocity = new Vector3(0, 0, 0);
            if(md.Horizontal == 0 && md.Vertical == 0)
                _rigidbody.AddForce(transform.up * dashModifier * 2/3);
            else
                _rigidbody.AddForce((transform.forward * md.Vertical + transform.right * md.Horizontal).normalized * dashModifier);
            disableAR = true;
            disableCM = true;
            Invoke(nameof(EndDash), dashDuration);
        }

        if (md.Jump)
            Jump(md.Horizontal, md.Vertical);

        //Extra gravity
        _rigidbody.AddForce(Vector3.down * 30);

        //Find actual velocity relative to where player is looking
        float xMag = mag.x, yMag = mag.y;

        //Counteract sliding and sloppy movement
        CounterMovement(md.Horizontal, md.Vertical, mag);

        //Make it easier to change trajectory in air
        AirReduction(md.Horizontal, md.Vertical, mag);

        //Set max speed
        float maxSpeed = this.maxSpeed;

        //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (md.Horizontal > 0 && xMag > maxSpeed) md.Horizontal = 0;
        if (md.Horizontal < 0 && xMag < -maxSpeed) md.Horizontal = 0;
        if (md.Vertical > 0 && yMag > maxSpeed) md.Vertical = 0;
        if (md.Vertical < 0 && yMag < -maxSpeed) md.Vertical = 0;

        //Some multipliers
        float multiplier = 1f, multiplierV = 1f;

        // Movement in air
        if (!grounded)
        {
            multiplier = airMovementMultiplier;
            multiplierV = 0.5f;
        }

        //Apply forces to move player
        _rigidbody.AddForce(transform.forward * md.Vertical * moveSpeed * multiplier * multiplierV);
        _rigidbody.AddForce(transform.right * md.Horizontal * moveSpeed * multiplier);
    }

    private void Jump(float x, float y)
    {
        readyToJump = false;

        if (canGroundJump)
        {
            _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);
            _rigidbody.AddForce(Vector2.up * jumpForce * wallJumpUpModifier);
            if (Vector3.Angle(Vector2.up, normalVector) > 35)
                _rigidbody.AddForce(normalVector * jumpForce * wallJumpModifier);
            else
                _rigidbody.AddForce(normalVector * jumpForce * (2f - wallJumpUpModifier));
        }
        else
        {
            _rigidbody.AddForce(Vector2.up * secondJumpForce * 2f);
            jumpCharge--;
        }

        //If jumping while falling, reset y velocity.
        Vector3 vel = _rigidbody.velocity;
        if (_rigidbody.velocity.y < 0.5f)
            _rigidbody.velocity = new Vector3(vel.x, 0, vel.z);
        else if (_rigidbody.velocity.y > 0)
            _rigidbody.velocity = new Vector3(vel.x, vel.y / 2, vel.z);

        Invoke(nameof(ResetJump), jumpCooldown);

    }

    //reduces velocity by a factor of the parameter
    private void EndDash()
    {
        Vector3 vel = _rigidbody.velocity;
        _rigidbody.velocity = new Vector3(vel.x/3, vel.y/3, vel.z/3);
        disableAR = false;
        disableCM = false;
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!grounded || !readyToJump || disableCM) return;

        //Counter movement
        if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
        {
            _rigidbody.AddForce(moveSpeed * transform.right * -mag.x * counterMovement);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
        {
            _rigidbody.AddForce(moveSpeed * transform.forward * -mag.y * counterMovement);
        }

        //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
        if (Mathf.Sqrt((Mathf.Pow(_rigidbody.velocity.x, 2) + Mathf.Pow(_rigidbody.velocity.z, 2))) > maxSpeed)
        {
            float fallspeed = _rigidbody.velocity.y;
            Vector3 n = _rigidbody.velocity.normalized * maxSpeed;
            _rigidbody.velocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    private void AirReduction(float x, float y, Vector2 mag)
    {
        if (grounded || disableAR) return;

        //Counter movement
        if ((x > 0 && mag.x < 0) || (x < 0 && mag.x > 0))
        {
            _rigidbody.AddForce(moveSpeed * transform.right * x * airReduceAmt);
        }
        if ((y > 0 && mag.y < 0) || (y < 0 && mag.y > 0))
        {
            _rigidbody.AddForce(moveSpeed * transform.forward * y * airReduceAmt);
        }

        //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
        if (Mathf.Sqrt((Mathf.Pow(_rigidbody.velocity.x, 2) + Mathf.Pow(_rigidbody.velocity.z, 2))) > maxSpeed)
        {
            float fallspeed = _rigidbody.velocity.y;
            Vector3 n = _rigidbody.velocity.normalized * maxSpeed;
            _rigidbody.velocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    // <summary>
    // Find the velocity relative to where the player is looking
    // Useful for vectors calculations regarding movement and limiting movement
    // </summary>
    // <returns></returns>
    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(_rigidbody.velocity.x, _rigidbody.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = _rigidbody.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private bool IsFloor(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < 95;
    }

    void CancelCoyoteTime()
    {
        canGroundJump = false;
    }

    private void OnCollisionExit(Collision other)
    {
        grounded = false;
        Invoke(nameof(CancelCoyoteTime), coyoteTime);
    }

    // Allows player to jump away from wall
    private void OnCollisionStay(Collision other)
    {
        if (other.gameObject.tag == "Ground" || other.gameObject.tag == "Enemy")
        {
            for (int i = 0; i < other.contactCount; i++)
            {
                Vector3 normal = other.contacts[i].normal;
                //FLOOR
                if (IsFloor(normal))
                {
                    grounded = true;
                    canGroundJump = true;
                    normalVector = normal;
                    jumpCharge = 1;
                }
            }

        }
    }

    //private void OnTriggerStay(Collider other)
    //{
    //    grounded = true;
    //    jumpCharge = 1;
    //}

    //private void OnTriggerExit(Collider other)
    //{
    //    grounded = false;
    //}

    [Reconcile]
    private void Reconciliation(ReconcileData rd, bool asServer)
    {
        transform.position = rd.Position;
        transform.rotation = rd.Rotation;
        _rigidbody.velocity = rd.Velocity;
    }
}