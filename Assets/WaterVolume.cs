using System;
using UnityEngine;

public class WaterVolume : MonoBehaviour
{
    public float Density { get; private set; } = 1f;

    private void OnTriggerStay(Collider other)
    {
        if (!other.TryGetComponent<WaterPhysicsOverride>(out WaterPhysicsOverride floatingObject)) return;
        if (floatingObject.Volume == null || floatingObject.Volume != this)
        {
            floatingObject.TogglePhysicsOverride(true);
            floatingObject.Volume = this;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent<WaterPhysicsOverride>(out WaterPhysicsOverride floatingObject)) return;
        if (floatingObject.Volume == null || floatingObject.Volume != this) return;
        floatingObject.TogglePhysicsOverride(false);
        floatingObject.Volume = null;
    }
    
    
}
