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
        public MoveData(bool jump, float horizontal, float vertical)
        {
            Jump = jump;
            Horizontal = horizontal;
            Vertical = vertical;
        }
    }
    public struct ReconcileData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
        public ReconcileData(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
        {
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            AngularVelocity = angularVelocity;
        }
    }
    #endregion
    
    #region Serialized.
    [SerializeField]
    private Transform orientation;
    [SerializeField]
    private float jumpForce = 15f;
    [SerializeField]
    private float moveSpeed = 4500f;
    [SerializeField]
    private float maxSpeed = 20;
    [SerializeField]
    private bool grounded;
    [SerializeField]
    private float counterMovement = 0.175f;
    [SerializeField]
    private LayerMask whatIsGround;
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

    private float threshold = 0.01f;
    #endregion

    public bool disableCM;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        InstanceFinder.TimeManager.OnTick += TimeManager_OnTick;
        InstanceFinder.TimeManager.OnPostTick += TimeManager_OnPostTick;
        disableCM = false;
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
            if (Input.GetButton("Jump") && readyToJump && jumpCharge > 0)
            {
                jumping = true;
            }
        }
    }

    private void TimeManager_OnTick()
    {
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
            ReconcileData rd = new ReconcileData(transform.position, transform.rotation, _rigidbody.velocity, _rigidbody.angularVelocity);
            Reconciliation(rd, true);
        }
    }

    private void CheckInput(out MoveData md)
    {
        md = default;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (horizontal == 0f && vertical == 0f && !jumping)
            return;

        md = new MoveData(jumping, horizontal, vertical);
        jumping = false;
    }

    [Replicate]
    private void Move(MoveData md, bool asServer, bool replaying = false)
    {
        if (md.Jump)
            Jump();

        //Extra gravity
        _rigidbody.AddForce(Vector3.down * 30);

        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        //Counteract sliding and sloppy movement
        CounterMovement(md.Horizontal, md.Vertical, mag);

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
        _rigidbody.AddForce(orientation.transform.forward * md.Vertical * moveSpeed * multiplier * multiplierV);
        _rigidbody.AddForce(orientation.transform.right * md.Horizontal * moveSpeed * multiplier);
    }

    private void Jump()
    {
        readyToJump = false;
        if(!grounded)
            jumpCharge--;

        //Add jump forces
        _rigidbody.AddForce(new Vector3(0f, jumpForce, 0f), ForceMode.Impulse);

        //If jumping while falling, reset y velocity.
        Vector3 vel = _rigidbody.velocity;
        if (_rigidbody.velocity.y < 0.5f)
            _rigidbody.velocity = new Vector3(vel.x, 0, vel.z);
        else if (_rigidbody.velocity.y > 0)
            _rigidbody.velocity = new Vector3(vel.x, vel.y / 2, vel.z);

        Invoke(nameof(ResetJump), jumpCooldown);
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
            _rigidbody.AddForce(moveSpeed * orientation.transform.right * -mag.x * counterMovement);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
        {
            _rigidbody.AddForce(moveSpeed * orientation.transform.forward * -mag.y * counterMovement);
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
        float lookAngle = orientation.transform.eulerAngles.y;
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
        return angle < maxSlopeAngle;
    }

    private bool cancellingGrounded;

    /// <summary>
    /// Handle ground detection
    /// </summary>
    private void OnCollisionStay(Collision other)
    {
        //Make sure we are only checking for walkable layers
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        //Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
            //FLOOR
            if (IsFloor(normal))
            {
                grounded = true;
                jumpCharge = 1;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
        }

        //Invoke ground cancel, since we can't check normals with CollisionExit
        float delay = .1f;
        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), delay);
        }
    }

    private void StopGrounded()
    {
        grounded = false;
    }

    [Reconcile]
    private void Reconciliation(ReconcileData rd, bool asServer)
    {
        transform.position = rd.Position;
        transform.rotation = rd.Rotation;
        _rigidbody.velocity = rd.Velocity;
        _rigidbody.angularVelocity = rd.AngularVelocity;
    }
}