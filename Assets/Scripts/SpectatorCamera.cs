using FishNet.Object;
using UnityEngine;

public class SpectatorCamera : NetworkBehaviour
{
    private float xRotation;
    [SerializeField]
    private float sensitivity = 50f;
    private float sensMultiplier = 1f;
    [SerializeField]
    private float moveRate = 0f;
    private GameObject cam;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            cam = transform.GetChild(0).gameObject;
            cam.SetActive(true);
        }
    }

    private void LateUpdate()
    {
        if (!base.IsOwner)
            return;

        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        //Find current look rotation
        Vector3 rot = cam.transform.localRotation.eulerAngles;
        float desiredX = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Perform the rotations
        cam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0);

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        float up = Input.GetAxisRaw("Jump");
        float down = Input.GetAxisRaw("Fire3");

        cam.transform.position += cam.transform.forward * vertical * moveRate * Time.fixedDeltaTime;
        cam.transform.position += cam.transform.right * horizontal * moveRate * Time.fixedDeltaTime;
        cam.transform.position += Vector3.up * (up-down) * moveRate * Time.fixedDeltaTime;
    }
}
