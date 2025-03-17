using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsEntity : MonoBehaviour
{
    [Header("Temperature")] 
    [SerializeField] private float _temperature;
    public float EntityTemperature { get => _temperature; private set => _temperature = value; }
    [SerializeField] private float _conductivity;
    
    [Header("Density")]
    [SerializeField] private float _density;
    public float Density { get => _density; private set => _density = value; }
    
    [Header("Feedback")] 
    [SerializeField] private float _environmentTemperature;
    
    [Header("References")]
    [SerializeField] private Rigidbody _rigidbody; 
    public EnvironCellData EnvironmentData;


    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }


    private void Update()
    {
        //Managing heat exchange
        if (EnvironmentData == null) return;
        _environmentTemperature = EnvironmentData.Temperature;
        if(_temperature > _environmentTemperature) _temperature -= EnvironmentData.ConductionExchange(_temperature, _conductivity);
        else if (_temperature < _environmentTemperature) EnvironmentData.CallForExchange(this);
    }
    
    public float ConductionExchange(float foreignEntityTemperature, float conductivity)
    {
        //Returns the temp change to other entity or environment
        //Entity with higher tempeture should call method
        if (foreignEntityTemperature <= _temperature)
        {
            Debug.Log("Conduction Heat Exchange called by cooler entity.");
            return 0f;
        }
        
        var averageConductivity = (conductivity + _conductivity)/2;
        var tempDiff = foreignEntityTemperature - _temperature;
        var heatExchanged = tempDiff * averageConductivity;
        
        _temperature += heatExchanged;
        return heatExchanged;
    }
    
    public void ApplyExternalForce(Vector3 force)
    {
        _rigidbody.AddForce(force);
    }

    public void EnvironmentalDrag(float drag)
    {
        _rigidbody.linearDamping = drag;
    }
}
