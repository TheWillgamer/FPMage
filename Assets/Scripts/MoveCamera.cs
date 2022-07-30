using FishNet.Object;
using UnityEngine;

public class MoveCamera : NetworkBehaviour
{
    [SerializeField]
    private Transform playerHead;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            gameObject.SetActive(true);
        }
    }
    //void Update()
    //{
    //    if (base.IsOwner)
    //    {
    //        transform.position = playerHead.transform.position;
    //    }
    //}
}