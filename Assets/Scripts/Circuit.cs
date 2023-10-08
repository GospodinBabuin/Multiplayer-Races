using UnityEngine;

[CreateAssetMenu(fileName = "CircuitData", menuName = "Car/CircuitData")]
public class Circuit : ScriptableObject
{
    public Transform[] waypoints;
    public Transform[] spawnPoints;
}