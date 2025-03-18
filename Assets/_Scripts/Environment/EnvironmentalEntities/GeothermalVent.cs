using UnityEngine;

[RequireComponent(typeof(PhysicsEntity))]
public class GeothermalVent : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PhysicsEntity _physics;
    
    [Header("Values")]
    [SerializeField] float _ventTemperature = 100f;
    [SerializeField] float _heatRate = 0.5f;
    private void Awake()
    {
        _physics = GetComponent<PhysicsEntity>();
    }

    private void Update()
    {
        if(_physics.EntityTemperature < _ventTemperature) _physics.IncreaseInternalTemperature(_heatRate);
    }
}
