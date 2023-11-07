using UnityEngine;
using FishNet.Object;

public class MakeChildrenAudio2D : NetworkBehaviour
{
    [SerializeField] private Transform parent;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            foreach (Transform child in parent.transform)
            {
                child.GetComponent<AudioSource>().spatialBlend = 0f;
            }
        }
    }
}