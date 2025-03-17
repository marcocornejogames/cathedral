using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Serialization;

//Cell data
public class EnvironCellData
{ 
    public EnvironCellData(int cellIndex, float temperature, float density, float conductivity)
    {
        Index = cellIndex;
        Temperature = temperature;
        Density = density;
        Conductivity = conductivity;
    }
    public int Index { get; private set; }
    public float Temperature {get; private set;}
    public float Density {get; private set;}
    public float Conductivity {get; private set;}
    
    public float ConductionExchange(float foreignEntityTemperature, float conductivity)
    {
        //Returns the temp change to other entity or environment
        //Entity with higher temperature should call method
        if (foreignEntityTemperature <= Temperature)
        {
            Debug.Log("Conduction Heat Exchange called by cooler entity.");
            return 0f;
        }
        
        var averageConductivity = (conductivity + Conductivity)/2;
        var tempDiff = foreignEntityTemperature - Temperature;
        var heatExchanged = tempDiff * averageConductivity;
        

        Temperature += heatExchanged;
        Index++;
        return heatExchanged;
    }

    public void CallForExchange(PhysicsEntity entity)
    {
        Debug.Log("Test");
        Temperature -= entity.ConductionExchange(Temperature, Conductivity);
    }
}

[RequireComponent(typeof(Collider))]
public class EnvironmentVolume : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider _collider;
    [SerializeField] protected List<PhysicsEntity> Entities = new();
    
    [Header("Grid Values")]
    [SerializeField] private int _gridCellSize = 1;
    
    [Header("Environment Values")]
    [SerializeField] private float _startingTemperature;
    [SerializeField] private float _maxTemperature;
    [SerializeField] private float _conductivity;
    [SerializeField] private float _startingDensity;
    [SerializeField] private float _environmentalDrag;

    [Header("Data")] 
    public Dictionary<Vector3Int, EnvironCellData> Cells { get; private set; } = new();
    
    private void Awake()
    {
        //Get Components
        _collider = GetComponent<Collider>();
        SetUpCells();
    }

    private void Update()
    {
    }

    #region Entity Management
    private void OnTriggerStay(Collider other)
    {
        if (!other.TryGetComponent(out PhysicsEntity entity)) return;
        entity.EnvironmentData = Cells.GetValueOrDefault(WorldPosToCell(entity.transform.position));
        
        if (Entities.Contains(entity)) return;
        entity.EnvironmentalDrag(_environmentalDrag);
        Entities.Add(entity);
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out PhysicsEntity entity)) return;
        if (!Entities.Contains(entity)) return;
        
        entity.EnvironmentData = null;
        entity.EnvironmentalDrag(0);
        Entities.Remove(entity);
        Entities.RemoveAll(x => !x);
    }
    

    #endregion
    
    #region TOOLS

    protected Vector3Int WorldPosToCell(Vector3 pos)
    {
        foreach (var cell in Cells.Keys)
        {
            //See if position is within bounds of cell
            var cellPos = (Vector3)cell;
            var cellExtent = (float)_gridCellSize;

            if (pos.x >= cellPos.x - cellExtent && pos.x <= cellPos.x + cellExtent &&
                pos.y >= cellPos.y - cellExtent && pos.y <= cellPos.y + cellExtent &&
                pos.z >= cellPos.z - cellExtent && pos.z <= cellPos.z + cellExtent) return cell;
        }
        
        return Vector3Int.zero;
    }
    #endregion
    
    #region SETUP
    private void SetUpCells()
    {
        var boundsMax = Vector3Int.FloorToInt(_collider.bounds.max);
        var boundsMin = Vector3Int.FloorToInt(_collider.bounds.min);

        var index = 0;
        
        
        
        for (int z = boundsMin.z; z < boundsMax.z; z = z + _gridCellSize)
        {
            for (int y = boundsMin.y; y < boundsMax.y; y = y + _gridCellSize)
            {
                for (int x = boundsMin.x; x < boundsMax.x; x = x + _gridCellSize)
                {
                    var cellCoordinates = new Vector3Int(x, y, z);
                    var cellData = new EnvironCellData(index, _startingTemperature, _startingDensity, _conductivity);
                    
                    Cells.Add(cellCoordinates, cellData);
                    index++;
                }
            }
        }
    }
    #endregion
}
