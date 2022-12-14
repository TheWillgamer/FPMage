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
    private float _movingTime = 0f;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            transform.GetChild(0).gameObject.SetActive(true);
        }
    }

    private void LateUpdate()
    {
        if (base.IsServer && !base.IsOwner)
            transform.position = playerLoc.position;

        if (!base.IsOwner)
            return;

        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        //Find current look rotation
        Vector3 rot = transform.localRotation.eulerAngles;
        float desiredX = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Perform the rotations
        transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0);

        //Follows the player
        float distance = Mathf.Max(0.1f, Vector3.Distance(transform.position, playerLoc.position));
        if (transform.position != playerLoc.position)
        {
            _movingTime += Time.deltaTime;
            float smoothingPercent = (_movingTime / 0.75f);
            float smoothingRate = Mathf.Lerp(30f, 20f, smoothingPercent);
            transform.position = Vector3.MoveTowards(transform.position, playerLoc.position, smoothingRate * distance * Time.deltaTime);
        }
        else
        {
            _movingTime = 0f;
        }
    }
}