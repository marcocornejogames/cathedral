using System;
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
    public Vector3 Current;
    
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

    [Header("Temperature")] 
    [SerializeField] private bool _passiveCooling;
    [SerializeField] private float _coolingRate = 0.01f;
    [SerializeField] private float _startingTemperature;
    [SerializeField] private float _maxTemperature;
    [SerializeField] private float _minTemperature;
    [SerializeField] private float _conductivity;
    
    [Header("Fluidity")]
    [SerializeField] private bool _useFluidMechanics = true;

    [SerializeField] private float _convectionRate = 0.5f;
    [SerializeField] private float _environmentalDrag;
    
    [Header("Density")]
    [SerializeField] private float _startingDensity;

    [Header("Data")] 
    public Dictionary<Vector3Int, EnvironCellData> Cells { get; private set; } = new();
    private Vector3Int _cellRangeMin;
    private Vector3Int _cellRangeMax;
    
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

    protected void FixedUpdate()
    {
        Debug.Log($"Fluid mechanics enabled: {_useFluidMechanics}; Entity Count: {Entities.Count}; Environment name: {name}");
        if (!_useFluidMechanics || Entities.Count <= 0 ) return;
        Debug.Log("Calling Calculate Currents");
        CalculateCurrents();
        foreach (var entity in Entities)
        {
            if (!entity) continue;
            entity.ApplyExternalForce(entity.EnvironmentData.Current);
        }
    }

    #endregion

    #region PHYSICS MECHANICS
    private void TransferHeat()
    {
        foreach (var cell in Cells)
        {
            //Get cell coordinates
            var cellData = cell.Value;
            
            //Neighbours inside same volume
            var surroundingCells = GetSurroundingCells(cell.Key);
            foreach (var neighbour in surroundingCells)
            {
                if (neighbour.Temperature < cellData.Temperature)
                {
                    cellData.CallForExchange(neighbour);
                }
            }

            //Neighbour cells in foreign volumes
            if(cell.Key.x == _cellRangeMax.x) 
                TryHeatNextVolume(CastForNeighbourVolume(cell.Key, Vector3.right), cell.Value);
            if(cell.Key.x == _cellRangeMin.x) 
                TryHeatNextVolume(CastForNeighbourVolume(cell.Key, Vector3.left), cell.Value);
            if(cell.Key.y == _cellRangeMax.y) 
                TryHeatNextVolume(CastForNeighbourVolume(cell.Key, Vector3.up), cell.Value);
            if(cell.Key.y == _cellRangeMin.y) 
                TryHeatNextVolume(CastForNeighbourVolume(cell.Key, Vector3.down), cell.Value);
            if(cell.Key.z == _cellRangeMax.z) 
                TryHeatNextVolume(CastForNeighbourVolume(cell.Key, Vector3.forward), cell.Value);
            if(cell.Key.z == _cellRangeMin.z) 
                TryHeatNextVolume(CastForNeighbourVolume(cell.Key, Vector3.back), cell.Value);
        }
    }

    private void CalculateCurrents()
    {
        foreach (var cell in Cells)
        {
            //Debugging
            var current = new Vector3(0, 0, 0);
            var neighbours = GetSurroundingKeys(cell.Key);
            int convectionAxes = 0;
            foreach (var neighbour in neighbours)
            {
                float neighbourTemp = Cells.GetValueOrDefault(neighbour).Temperature;
                if (cell.Value.Temperature > neighbourTemp)
                {
                    Vector3 direction = (neighbour - cell.Key);
                    direction.Normalize();
                    
                    direction *= (cell.Value.Temperature - neighbourTemp)*_convectionRate;
                    
                    current += direction;
                    convectionAxes++;
                }
            }

            if (convectionAxes > 0)
            {
                current /= convectionAxes;
            }

            cell.Value.Current = current;
        }
    }

    private void TryHeatNextVolume(EnvironCellData foreignCell, EnvironCellData homeCell)
    {
        if (foreignCell == null) return;
        if(foreignCell.Temperature < homeCell.Temperature) homeCell.CallForExchange(foreignCell);
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
    protected List<Vector3Int> GetSurroundingKeys(Vector3Int cellCoordinate)
    {
        var neighbourKeys = new List<Vector3Int>();
        for (int cx = cellCoordinate.x - _gridCellSize; cx <= cellCoordinate.x + _gridCellSize; cx += _gridCellSize)
        {
            for (int cy = cellCoordinate.y - _gridCellSize; cy <= cellCoordinate.y + _gridCellSize; cy += _gridCellSize)
            {
                for (int cz = cellCoordinate.z - _gridCellSize; cz <= cellCoordinate.z+ _gridCellSize; cz += _gridCellSize)
                {
                    var neighbourCellKey = new Vector3Int(cx, cy, cz);
                    
                    if (!Cells.ContainsKey(neighbourCellKey)) continue;
    
                    neighbourKeys.Add(neighbourCellKey);
                }
            }
        }

        neighbourKeys.Remove(cellCoordinate); //Remove center cell, only surroundings
        return neighbourKeys;
        
    }

    protected EnvironCellData CastForNeighbourVolume(Vector3 startingPos, Vector3 direction)
    {
        var cellExtent = (float)_gridCellSize;
        startingPos = new Vector3(startingPos.x + cellExtent/2, startingPos.y + cellExtent/2, startingPos.z + cellExtent/2);
        
        RaycastHit[] hits;
        hits = (Physics.RaycastAll(startingPos, direction, _gridCellSize + 1));
        foreach (var hit in hits)
        {
            if (hit.collider == _collider) continue;
            if(hit.collider.TryGetComponent<EnvironmentVolume>(out var volume)) 
                return volume.Cells.GetValueOrDefault(volume.WorldPosToCell(hit.point));
        }

        return null;
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
                    CellKeyMinMax(cellCoordinates);
                    
                    Cells.Add(cellCoordinates, cellData);
                    index++;
                }
            }
        }
    }

    private void CellKeyMinMax(Vector3Int cellCoordinate)
    {
        //Min
        if(cellCoordinate.x < _cellRangeMin.x) _cellRangeMin.x = cellCoordinate.x;
        if(cellCoordinate.y < _cellRangeMin.y) _cellRangeMin.y = cellCoordinate.y;
        if(cellCoordinate.z < _cellRangeMin.z) _cellRangeMin.z = cellCoordinate.z;
        
        //Max
        if(cellCoordinate.x > _cellRangeMax.x) _cellRangeMax.x = cellCoordinate.x;
        if(cellCoordinate.y > _cellRangeMax.y) _cellRangeMax.y = cellCoordinate.y;
        if(cellCoordinate.z > _cellRangeMax.z) _cellRangeMax.z = cellCoordinate.z;
    }
    #endregion
}
