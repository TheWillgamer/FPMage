using UnityEngine;
using FishNet.Object;

public class MakeChildrenAudio2D : NetworkBehaviour
{
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            foreach (Transform child in transform)
            {
                child.GetComponent<AudioSource>().spatialBlend = 0f;
            }
        }
    }
}