    using System;
    using Unity.Collections;
    using UnityEngine;
    using Utils;

    public class AIInput : MonoBehaviour, IDrive
    {
        public Circuit circuit;
        public AIDriverData driverData;
        
        public Vector2 Move { get; private set; }
        public bool IsBraking { get; private set; }
        public void Enable()
        {
            // not implemented
        }

        private int _currentWaypointIndex;
        private int _currentCornerIndex;

        private CountdownTimer _driftTimer;
        
        private float _previousYaw;

        public void AddDriverData(AIDriverData data) => driverData = data;
        public void AddCircuit(Circuit circuit) => this.circuit = circuit;

        private void Start()
        {
            if (circuit == null || driverData == null)
            {
                throw new ArgumentNullException($"AIInput requires a circuit and driver data to be set.");
            }

            _previousYaw = transform.eulerAngles.y;
            _driftTimer = new CountdownTimer(driverData.timeToDrift);
            _driftTimer.OnTimerStart += () => IsBraking = true;
            _driftTimer.OnTimerStop += () => IsBraking = false;
        }

        private void Update()
        {
            _driftTimer.Tick(Time.deltaTime);
            if (circuit.waypoints.Length == 0)
            {
                return;
            }

            float currentYaw = transform.eulerAngles.y;
            float deltaYaw = Mathf.DeltaAngle(_previousYaw, currentYaw);
            float angularVelocity = deltaYaw / Time.deltaTime;
            _previousYaw = currentYaw;

            Vector3 toNextPoint = circuit.waypoints[_currentWaypointIndex].position - transform.position;
            Vector3 toNextCorner = circuit.waypoints[_currentCornerIndex].position - transform.position;
            var distanceToNextPoint = toNextPoint.magnitude;
            var distanceToNextCorner = toNextCorner.magnitude;

            if (distanceToNextPoint < driverData.proximityThreshold)
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % circuit.waypoints.Length;
            }

            if (distanceToNextCorner < driverData.updateCornerRange)
            {
                _currentCornerIndex = _currentWaypointIndex;
            }

            if (distanceToNextCorner < driverData.brakeRange && !_driftTimer.IsRunning)
            {
                _driftTimer.Start();
            }

            Move = Move.With(y: _driftTimer.IsRunning ? driverData.speedWhileDrifting : 1f);

            
            
            Vector3 desiredForward = toNextPoint.normalized;
            Vector3 currentForward = transform.forward;
            float turnAngle = Vector3.SignedAngle(currentForward, desiredForward, Vector3.up);

            Move = turnAngle switch
            {
                > 5f => Move.With(x: 1f),
                < -5f => Move.With(x: -1f),
                _ => Move.With(x: 0f)
            };

            if (Mathf.Abs(angularVelocity) > driverData.spinThreshold)
            {
                Move = Move.With(x: -Mathf.Sign(angularVelocity));
                IsBraking = true;
            }
            else
            {
                IsBraking = false;
            }
        }
    }