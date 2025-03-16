using System;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
public class WaterPhysicsOverride : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] public WaterVolume Volume;
    
    [Header("Values")]
    [SerializeField] private float _density = 1f;
    [SerializeField] private float _inWaterDamping = 0.05f;
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        
    }

    public void TogglePhysicsOverride(bool isToggled)
    {
        if (isToggled)
        {
            _rigidbody.linearDamping = _inWaterDamping;
        }
        else
        {
            _rigidbody.linearDamping = 0f;
        }
    }
    private void FixedUpdate()
    {
        if (!Volume) return;
        //Offset gravity by density difference
        var densityFraction = Volume.Density / _density;
        var gravityModifier = Mathf.Clamp(densityFraction, 0.5f, 2f);
        var bouyancy = Vector3.Scale(Vector3.up, -Physics.gravity * gravityModifier);
        _rigidbody.AddForce(bouyancy);

    }


}
