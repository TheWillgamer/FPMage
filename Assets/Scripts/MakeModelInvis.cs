using UnityEngine;
using UnityEngine.Rendering;
using FishNet.Object;


public class MakeModelInvis : NetworkBehaviour
{
    [SerializeField] private GameObject clientModel;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            //clientModel.SetActive(true);
            //gameObject.SetActive(false);
        }
    }
}
