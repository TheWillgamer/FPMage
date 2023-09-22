using FishNet.Object;
using UnityEngine;

public class MoveCamera : NetworkBehaviour
{
    private float xRotation;
    [SerializeField]
    private float sensitivity = 50f;
    private float sensMultiplier = 1f;
    [SerializeField]
    private Transform playerLoc;
    [SerializeField]
    private float smoothTime;
    private Vector3 velocity = Vector3.zero;
    private float _movingTime = 0f;
    public bool playerSpawned;
    public bool activated;

    [SerializeField]
    private Animator animator;
    [SerializeField]
    private Transform shadow;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            //transform.GetChild(0).gameObject.SetActive(true);
            GameObject cam = GameObject.FindWithTag("MainCamera");
            cam.transform.parent = transform;
            cam.transform.rotation = transform.rotation;
            cam.transform.localPosition = Vector3.zero;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        playerSpawned = true;
        activated = true;
    }

    private void LateUpdate()
    {
        //Follows the player
        if (activated && base.IsServer && !base.IsOwner)
            transform.position = playerLoc.position;

        if (!base.IsOwner || !activated)
            return;

        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        //Find current look rotation
        Vector3 rot = transform.localRotation.eulerAngles;
        float desiredX = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -89f, 89f);
        animator.SetFloat("aim", 1f - (xRotation + 89f) / 178f);

        //Perform the rotations
        transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0);

        shadow.rotation = Quaternion.Euler(0.0f, transform.eulerAngles.y, 0.0f);

        if (!playerSpawned)
            return;

        float distance = Mathf.Max(0.1f, Vector3.Distance(transform.position, playerLoc.position));
        if (transform.position != playerLoc.position)
        {
            //_movingTime += Time.deltaTime;
            //float smoothingPercent = (_movingTime / 0.75f);
            //float smoothingRate = Mathf.Lerp(50f, 40f, smoothingPercent);
            //transform.position = Vector3.MoveTowards(transform.position, playerLoc.position, smoothingRate * distance * Time.deltaTime);

            transform.position = Vector3.SmoothDamp(transform.position, playerLoc.position, ref velocity, smoothTime);
        }
        else
        {
            _movingTime = 0f;
        }
    }
}