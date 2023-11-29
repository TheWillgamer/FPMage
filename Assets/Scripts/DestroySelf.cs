using UnityEngine;

public class DestroySelf : MonoBehaviour
{
    [SerializeField] private float delay;

    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, delay);
    }
}
