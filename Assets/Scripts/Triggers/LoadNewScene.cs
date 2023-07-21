using FishNet;
using FishNet.Managing.Logging;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;

public class LoadNewScene : MonoBehaviour
{
    [SerializeField] private string scene;

    [Server(Logging = LoggingType.Off)]
    void OnTriggerEnter(Collider other)
    {
        if (other.tag != "Player")
            return;

        NetworkObject nob = other.GetComponent<NetworkObject>();
        Debug.Log(nob.Owner.IsActive);
        if (nob != null)
            LoadScene(nob);
    }

    private void LoadScene(NetworkObject nob)
    {
        SceneLoadData sld = new SceneLoadData(scene);
        sld.MovedNetworkObjects = new NetworkObject[] { nob };
        sld.ReplaceScenes = ReplaceOption.All;
        InstanceFinder.SceneManager.LoadConnectionScenes(nob.Owner, sld);
    }
}
