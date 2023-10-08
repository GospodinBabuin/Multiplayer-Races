using UnityEngine;

[CreateAssetMenu(fileName = "AIDriverData", menuName = "Car/AIDriverData")]
public class AIDriverData : ScriptableObject
{
    public float proximityThreshold = 20f;
    public float updateCornerRange = 50f;
    public float brakeRange = 80f;
    public float spinThreshold = 100f;
    public float speedWhileDrifting = 0.5f;
    public float timeToDrift = 0.5f;
}