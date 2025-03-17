using System;
using System.Collections.Generic;
using UnityEngine;

public class WaterVolume : EnvironmentVolume
{
    private void FixedUpdate()
    {
        //Apply buoyancy based on environemnt and entity density
        if (Entities.Count == 0) return;
        foreach (var floatingEntity in Entities)
        {
            var volumeCell = WorldPosToCell(floatingEntity.transform.position); 
            if(!Cells.TryGetValue(volumeCell, out var cell)) continue;
            var densityFraction = cell.Density / floatingEntity.Density;
            var gravityModifier = Mathf.Clamp(densityFraction, 0.5f, 2f);
            var buoyancy = Vector3.Scale(Vector3.up, -Physics.gravity * gravityModifier);
            floatingEntity.ApplyExternalForce(buoyancy);
        }
    }
}
