using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnvironmentVolume))]
public class EnvironmentalVisualization : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private bool _toggleVisualization;
    [SerializeField] private VisualizationType _visualizationType = VisualizationType.Temperature;
    
    [Header("Visualization")]
    [SerializeField] private Vector2 _temperatureRange = new Vector2(0f, 100f);
    [SerializeField] private Gradient _temperatureGradient;
    [SerializeField] private Gradient _densityGradient;
    private bool _isVisualizing;
    public enum VisualizationType
    {
        Temperature,
        Density
    }
    
    [Header("References")] 
    [SerializeField] private SpriteRenderer _environmentalMarker;
    [SerializeField] private EnvironmentVolume _volume;

    [Header("Values")] 
    private List<SpriteRenderer> _markers = new();

    private void Awake()
    {
        _volume = GetComponent<EnvironmentVolume>();
    }

    private void Start()
    {
        foreach (var cell in _volume.Cells)
        {
            var newMarker = Instantiate(_environmentalMarker, transform);
            _markers.Add(newMarker);
            newMarker.transform.position = _volume.CellToWorldPos(cell.Key);
        }
        
        ToggleMarkerVisibility(_toggleVisualization);
        
    }

    private void Update()
    {
        //Toggle visualization on and off
        if (_isVisualizing != _toggleVisualization)
        {
            ToggleMarkerVisibility(_toggleVisualization);
        }
        
        if (_toggleVisualization)
        {
            switch (_visualizationType)
            {
                case VisualizationType.Temperature:
                    UpdateTempInfo();
                    break;
                case VisualizationType.Density:
                    break;
            }
        }
    }

    private void UpdateTempInfo()
    {
        foreach (var marker in _markers)
        {
            var cellKey = _volume.WorldPosToCell(marker.transform.position);
            var cell = _volume.Cells.GetValueOrDefault(cellKey);
            var cellTemp = Mathf.Clamp(cell.Temperature, _temperatureRange.x, _temperatureRange.y);
            cellTemp = MathTools.Remap(cellTemp, _temperatureRange.x, _temperatureRange.y, 0f, 1f);
            marker.material.color = _temperatureGradient.Evaluate(cellTemp);
        }
        
    }

    public void ToggleMarkerVisibility(bool visibility)
    {
        _isVisualizing = visibility;
        _toggleVisualization = visibility;
        foreach (var marker in _markers) marker.enabled = visibility;
    }
}
