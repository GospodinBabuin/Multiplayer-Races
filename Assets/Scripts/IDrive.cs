using UnityEngine;

public interface IDrive
{
    public Vector2 Move { get; }
    public bool IsBraking { get; }
    public void Enable();
}
