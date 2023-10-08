using UnityEngine;

public class SkidMarkHandler : MonoBehaviour
{ 
    [SerializeField] private float slipThreshold = 0.4f;
    [SerializeField] private Transform skidMarkPrefab;
    
    private CarController _car;

    [SerializeField] private WheelCollider[] _wheelColliders;
    [SerializeField] private Transform[] _skidMarks = new Transform[4];


    private void Start()
    {
        _car = GetComponent<CarController>();
        _wheelColliders = GetComponentsInChildren<WheelCollider>();
    }

    private void Update()
    {
        for (int i = 0; i < _wheelColliders.Length; i++)
        {
            UpdateSkidMarks(i);
        }
    }

    private void UpdateSkidMarks(int i)
    {
        if (!_wheelColliders[i].GetGroundHit(out var hit) || !_car.IsGrounded)
        {
            EndSkid(i);
            return;
        }
        
        if (Mathf.Abs(hit.sidewaysSlip) > slipThreshold || Mathf.Abs(hit.forwardSlip) > slipThreshold)
        {
            StartSkid(i);
        } 
        else 
        {
            EndSkid(i);
        }
    }

    private void StartSkid(int i)
    {
        if (_skidMarks[i] != null) return;
        
        _skidMarks[i] = Instantiate(skidMarkPrefab, _wheelColliders[i].transform);
        _skidMarks[i].localPosition = -Vector3.up * (_wheelColliders[i].radius * 1.3f);
        _skidMarks[i].localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void EndSkid(int i)
    {
        if (_skidMarks[i] == null) return;
        
        Transform holder = _skidMarks[i];
        _skidMarks[i] = null;
        holder.SetParent(null);
        holder.rotation = Quaternion.Euler(90f, 0f, 0f);
        Destroy(holder.gameObject, 5f);
    }
}