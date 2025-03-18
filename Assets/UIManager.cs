using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private EnvironmentalVisualization[] _visualizationComponents;
    private bool _isVisualizing;
    
    //Quick and  dirty!
    public void ToggleVisualization()
    {
        _isVisualizing = !_isVisualizing;
        foreach(var component in _visualizationComponents) component.ToggleMarkerVisibility(_isVisualizing);
    }
}
