using FishNet.Object;
using UnityEngine;

public class RotateWizard : NetworkBehaviour
{
    [SerializeField]
    private Transform playerCam;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.localRotation = Quaternion.Euler(0.0f, playerCam.eulerAngles.y, 0.0f);
    }
}
