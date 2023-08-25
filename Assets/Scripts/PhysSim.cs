using FishNet;
using FishNet.Managing.Timing;
using UnityEngine;

/// <summary>
/// Simulates phyics for the current scene.
/// </summary>
public class PhysSim : MonoBehaviour
{
    /// <summary>
    /// PhysicsScene this object is in. Required for scene stacking.
    /// </summary>
    public PhysicsScene _physicsScene;
    /// <summary>
    /// TimeManager subscribed to.
    /// </summary>
    private TimeManager _tm;

    private void Awake()
    {
        _tm = InstanceFinder.TimeManager;
        _tm.OnPostPhysicsSimulation += TimeManager_OnPhysicsSimulation;
        _physicsScene = gameObject.scene.GetPhysicsScene();

        //Let this script simulate physics.
        Physics.autoSimulation = false;
#if !UNITY_2020_2_OR_NEWER
            Physics2D.autoSimulation = false;
#else
        Physics2D.simulationMode = SimulationMode2D.Script;
#endif
    }

    private void OnDestroy()
    {
        if (_tm != null)
            _tm.OnPostPhysicsSimulation -= TimeManager_OnPhysicsSimulation;
    }

    private void TimeManager_OnPhysicsSimulation(float delta)
    {
        _physicsScene.Simulate(delta);
    }

}
