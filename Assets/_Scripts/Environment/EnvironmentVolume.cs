using System.Collections.Generic;
using UnityEngine;

#region ENVIRONMENTAL CELL DATA
public class EnvironCellData
{ 
    public EnvironCellData(int cellIndex, float temperature, float maxTemperature, float minTemperature, float density, float conductivity)
    {
        Index = cellIndex;
        
        Temperature = temperature;
        _maxTemp = maxTemperature;
        _minTemp = minTemperature;
        
        Density = density;
        
        Conductivity = conductivity;
    }
    public int Index { get; private set; }
    public float Temperature {get; private set;}
    private readonly float _maxTemp;
    private readonly float _minTemp;
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
        

        Temperature = Mathf.Clamp(Temperature += heatExchanged, _minTemp, _maxTemp);
        Index++;
        return heatExchanged;
    }

    public void CallForExchange(PhysicsEntity entity)
    {
        Temperature = Mathf.Clamp(Temperature -= entity.ConductionExchange(Temperature, Conductivity), _minTemp, _maxTemp);
    }
    public void CallForExchange(EnvironCellData entity)
    {
        Temperature -= entity.ConductionExchange(Temperature, Conductivity);
    }

    public void PassiveCooling(float coolingRate)
    {
        Temperature = Mathf.Clamp(Temperature -= coolingRate, _minTemp, _maxTemp);
    }

}

#endregion

[RequireComponent(typeof(Collider))]
public class EnvironmentVolume : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider _collider;
    [SerializeField] protected List<PhysicsEntity> Entities = new();
    
    [Header("Grid Values")]
    [SerializeField] private int _gridCellSize = 1;

    [Header("Environment Values")] 
    [SerializeField] private bool _passiveCooling;
    [SerializeField] private float _coolingRate = 0.01f;
    [SerializeField] private bool _useFluidMechanics = true;
    [SerializeField] private float _startingTemperature;
    [SerializeField] private float _maxTemperature;
    [SerializeField] private float _minTemperature;
    [SerializeField] private float _conductivity;
    [SerializeField] private float _startingDensity;
    [SerializeField] private float _environmentalDrag;

    [Header("Data")] 
    public Dictionary<Vector3Int, EnvironCellData> Cells { get; private set; } = new();

    #region UNITY METHODS
    private void Awake()
    {
        //Get Components
        _collider = GetComponent<Collider>();
        SetUpCells();
    }
    
    private void Update()
    {
        TransferHeat();
        if(_passiveCooling) PassiveCooling();
    }
    

    #endregion

    #region PHYSICS MECHANICS
    private void TransferHeat()
    {
        foreach (var cell in Cells)
        {
            //Get cell coordinates
            var cellData = cell.Value;
            var surroundingCells = GetSurroundingCells(cell.Key);
            foreach (var neighbour in surroundingCells)
            {
                if (neighbour.Temperature < cellData.Temperature)
                {
                    cellData.CallForExchange(neighbour);
                }

            }
            
        }
    }

    private void PassiveCooling()
    {
        foreach (var cell in Cells) cell.Value.PassiveCooling(_coolingRate);
    }

    #endregion

    #region ENTITY MANAGEMENT
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

    public Vector3 CellToWorldPos(Vector3Int cell)
    {
        var cellExtent = (float)_gridCellSize;
        return new Vector3(cell.x + cellExtent/2, cell.y + cellExtent/2, cell.z + cellExtent/2);
    }
    public Vector3Int WorldPosToCell(Vector3 pos)
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

    protected List<EnvironCellData> GetSurroundingCells(Vector3Int cellCoordinate)
    {
        var neighbourCells = new List<EnvironCellData>();
        for (int cx = cellCoordinate.x - _gridCellSize; cx <= cellCoordinate.x + _gridCellSize; cx += _gridCellSize)
        {
            for (int cy = cellCoordinate.y - _gridCellSize; cy <= cellCoordinate.y + _gridCellSize; cy += _gridCellSize)
            {
                for (int cz = cellCoordinate.z - _gridCellSize; cz <= cellCoordinate.z+ _gridCellSize; cz += _gridCellSize)
                {
                    var neighbourCellKey = new Vector3Int(cx, cy, cz);
                    
                    if (!Cells.ContainsKey(neighbourCellKey)) continue;
    
                    neighbourCells.Add(Cells.GetValueOrDefault(neighbourCellKey));
                }
            }
        }

        neighbourCells.Remove(Cells.GetValueOrDefault(cellCoordinate)); //Remove center cell, only surroundings
        return neighbourCells;
        
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
                    var cellData = new EnvironCellData(index, _startingTemperature, _maxTemperature, _minTemperature, _startingDensity, _conductivity);
                    
                    Cells.Add(cellCoordinates, cellData);
                    index++;
                }
            }
        }
    }
    #endregion
}
