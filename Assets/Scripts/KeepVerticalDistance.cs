using UnityEngine;

public class KeepVerticalDistance : MonoBehaviour
{
    [SerializeField] private float distance;
    [SerializeField] private Transform objOfInt;        // object of interest

    // Update is called once per frame
    void Update()
    {
        transform.position = objOfInt.position - new Vector3(0f, distance, 0f);
    }
}
