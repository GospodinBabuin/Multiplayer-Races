using System;
using System.Linq;
using UnityEngine;
using Utils;

[Serializable] 
public class AxleInfo 
{
    public WheelCollider leftWheel; 
    public WheelCollider rightWheel;
    public bool motor;
    public bool steering;
    public WheelFrictionCurve originalForwardFriction; 
    public WheelFrictionCurve originalSidewaysFriction;
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SkidMarkHandler))]
public class CarController : MonoBehaviour
{
    [Header("Axle Information")]
    [SerializeField] private AxleInfo[] axleInfos;

    [Header("Motor Attributes")] 
    [SerializeField] private float maxMotorTorque = 3000f;
    [SerializeField] private float maxSpeed;
    [SerializeField] [Range(0.1f, 10f)] private float velocityChangeSpeed;
    
    [Header("Steering Attributes")]
    [SerializeField] private float maxSteeringAngle = 30f;
    [SerializeField] private AnimationCurve turnCurve;
    [SerializeField] private float turnStrength = 100f;

    [Header("Breaking and Drifting")]
    [SerializeField] private float brakeTorque = 10000f;
    [SerializeField] private float driftSteerMultiplier = 1.5f;

    [Header("Physics")]
    [SerializeField] private Transform centerOfMass;
    [SerializeField] private float downForce = 10f;
    [SerializeField] private float gravity = Physics.gravity.y;
    [SerializeField] private float lateralGScale = 10f;

    [Header("Banking")]
    [SerializeField] private float maxBankAngle = 5f;
    [SerializeField] private float bankSpeed = 2f;

    [Header("Refs")] 
    [SerializeField] private InputReader playerInput;
    [SerializeField] private Circuit circuit;
    [SerializeField] private AIDriverData driverData;
    
    private IDrive _input;
    private Rigidbody _rigidbody;

    private Vector3 _carVelocity;
    private float _brakeVelocity;
    private float _driftVelocity;

    private RaycastHit _hit;
    
    private const float thresholdSpeed = 10f;
    private const float centerOfMassOffset = 0.5f;
    private Vector3 _originalCenterOfMass;

    public bool IsGrounded = true;
    public Vector3 Velocity => _carVelocity;
    public float MaxSpeed => maxSpeed;

    private void Awake()
    {
        if (playerInput is IDrive driveInput)
        {
            _input = driveInput;
        }
    }

    public void SetInput(IDrive input)
    {
        this._input = input;
    }
    
    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _input.Enable();

        _rigidbody.centerOfMass = centerOfMass.localPosition;
        _originalCenterOfMass = centerOfMass.localPosition;

        foreach (AxleInfo axleInfo in axleInfos)
        {
            axleInfo.originalForwardFriction = axleInfo.leftWheel.forwardFriction;
            axleInfo.originalSidewaysFriction = axleInfo.leftWheel.sidewaysFriction;
        }
    }

    private void FixedUpdate()
    {
        float verticalInput = AdjustInput(_input.Move.y);
        float horizontalInput = AdjustInput(_input.Move.x);

        float motor = maxMotorTorque * verticalInput;
        float steering = maxSteeringAngle * horizontalInput;
        
        UpdateAxles(motor, steering);
        UpdateBanking(horizontalInput);

        _carVelocity = transform.InverseTransformDirection(_rigidbody.velocity);

        if (IsGrounded)
        {
            HandleGroundedMovement(verticalInput, horizontalInput);
        }
        else
        {
            HandleAirborneMovement(verticalInput, horizontalInput);
        }
    }

    private void HandleGroundedMovement(float verticalInput, float horizontalInput)
    {
        if (Mathf.Abs(verticalInput) > 0.1f || Mathf.Abs(_carVelocity.z) > 1)
        {
            float turnMultiplier = Mathf.Clamp01(turnCurve.Evaluate(_carVelocity.magnitude / maxSpeed));
            _rigidbody.AddTorque(Vector3.up * (horizontalInput * Mathf.Sign(_carVelocity.z) * turnStrength * 100f * turnMultiplier));
        }

        if (!_input.IsBraking)
        {
            float targetSpeed = verticalInput * maxSpeed;
            Vector3 forwardWithoutY = transform.forward.With(y: 0).normalized;
            _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, forwardWithoutY * targetSpeed, Time.deltaTime * velocityChangeSpeed);
        }

        float speedFactor = Mathf.Clamp01(_rigidbody.velocity.magnitude / maxSpeed);
        float lateralG = Mathf.Abs(Vector3.Dot(_rigidbody.velocity, transform.right));
        float downForceFactor = Mathf.Max(speedFactor, lateralG / lateralGScale);
        _rigidbody.AddForce(-transform.up * (downForce * _rigidbody.mass * downForceFactor));

        float speed = _rigidbody.velocity.magnitude;
        Vector3 centerOfMassAdjustment = (speed > thresholdSpeed)
            ? new Vector3(0f, 0f, Mathf.Abs(verticalInput) > 0.1f ? Mathf.Sign(verticalInput) * centerOfMassOffset : 0f)
            : Vector3.zero;

        _rigidbody.centerOfMass = _originalCenterOfMass + centerOfMassAdjustment;
    }

    private void UpdateBanking(float horizontalInput)
    {
        if (_rigidbody.velocity.magnitude < thresholdSpeed) return;
        
        float targetBankAngle = horizontalInput * -maxBankAngle;
        Vector3 currentEuler = transform.localEulerAngles;
        currentEuler.z = Mathf.LerpAngle(currentEuler.z, targetBankAngle, Time.deltaTime * bankSpeed);
        transform.localEulerAngles = currentEuler;
    }

    private void HandleAirborneMovement(float verticalInput, float horizontalInput)
    {
        _rigidbody.velocity =
            Vector3.Lerp(_rigidbody.velocity, _rigidbody.velocity + Vector3.down * gravity, Time.deltaTime * gravity);
    }

    private void UpdateAxles(float motor, float steering)
    {
        foreach (AxleInfo axleInfo in axleInfos)
        {
            HandleSteering(axleInfo, steering);
            HandleMotor(axleInfo, motor);
            HandleBrakesAndDrift(axleInfo);
            UpdateWheelVisuals(axleInfo.leftWheel);
            UpdateWheelVisuals(axleInfo.rightWheel);
        }
    }

    private void UpdateWheelVisuals(WheelCollider collider)
    {
        if (collider.transform.childCount == 0) return;

        Transform visualWheel = collider.transform.GetChild(0);

        Vector3 position;
        Quaternion rotation;
        collider.GetWorldPose(out position, out rotation);

        visualWheel.transform.position = position;
        visualWheel.transform.rotation = rotation;
    }

    private void HandleBrakesAndDrift(AxleInfo axleInfo)
    {
        if (axleInfo.motor)
        {
            if (_input.IsBraking)
            {
                _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX;

                float newZ = Mathf.SmoothDamp(_rigidbody.velocity.z, 0f, ref _brakeVelocity, 1f);
                _rigidbody.velocity = _rigidbody.velocity.With(z: newZ);

                axleInfo.leftWheel.brakeTorque = brakeTorque;
                axleInfo.rightWheel.brakeTorque = brakeTorque;

                ApplyDriftFriction(axleInfo.leftWheel);
                ApplyDriftFriction(axleInfo.rightWheel);
            }
            else
            {
                _rigidbody.constraints = RigidbodyConstraints.None;
                
                axleInfo.leftWheel.brakeTorque = 0f;
                axleInfo.rightWheel.brakeTorque = 0f;
                
                ResetDriftFriction(axleInfo.leftWheel);
                ResetDriftFriction(axleInfo.rightWheel);
            }
        }
    }

    private void ResetDriftFriction(WheelCollider wheel)
    {
        AxleInfo axleInfo = axleInfos.FirstOrDefault(axle => axle.leftWheel == wheel || axle.rightWheel == wheel);
        if (axleInfo == null) return;

        wheel.forwardFriction = axleInfo.originalForwardFriction;
        wheel.sidewaysFriction = axleInfo.originalSidewaysFriction;
    }

    private void ApplyDriftFriction(WheelCollider wheel)
    {
        if (wheel.GetGroundHit(out var hit))
        {
            wheel.forwardFriction = UpdateFriction(wheel.forwardFriction);
            wheel.forwardFriction = UpdateFriction(wheel.sidewaysFriction);
            IsGrounded = true;
        }
    }

    private WheelFrictionCurve UpdateFriction(WheelFrictionCurve friction)
    {
        friction.stiffness = _input.IsBraking
            ? Mathf.SmoothDamp(friction.stiffness, 0.5f, ref _driftVelocity, Time.deltaTime * 2f)
            : 1f;

        return friction;
    }

    private void HandleMotor(AxleInfo axleInfo, float motor)
    {
        if (axleInfo.motor)
        {
            axleInfo.leftWheel.motorTorque = motor;
            axleInfo.rightWheel.motorTorque = motor;
        }
    }

    private void HandleSteering(AxleInfo axleInfo, float steering)
    {
        if (axleInfo.steering)
        {
            float steeringMultiplier = _input.IsBraking ? driftSteerMultiplier : 1f;
            axleInfo.leftWheel.steerAngle = steering * steeringMultiplier;
            axleInfo.rightWheel.steerAngle = steering * steeringMultiplier;
        }
    }


    private float AdjustInput(float input)
    {
        return input switch
        {
            >= .7f => 1f,
            <= -.7f => -1f,
            _ => input
        };
    }
}
