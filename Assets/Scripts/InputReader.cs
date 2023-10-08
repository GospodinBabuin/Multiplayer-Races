using UnityEngine;
using UnityEngine.InputSystem;
using static PlayerInputActions;

[CreateAssetMenu(fileName = "InputReader", menuName = "Car/Input Reader")]
public class InputReader : ScriptableObject, IPlayerActions, IDrive
{
    public Vector2 Move => _inputActions.Player.Move.ReadValue<Vector2>();
    public Vector2 Look => _inputActions.Player.Look.ReadValue<Vector2>();
    public bool IsBraking => _inputActions.Player.Brake.ReadValue<float>() > 0;

    private PlayerInputActions _inputActions;
    
    private void OnEnable()
    {
        if (_inputActions != null) return;
        
        _inputActions = new PlayerInputActions();
        _inputActions.Player.SetCallbacks(this);
    }

    public void Enable()
    {
        _inputActions.Enable();
    }
    
    public void OnMove(InputAction.CallbackContext context)
    {
        // notImplemented
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // notImplemented
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        // notImplemented
    }

    public void OnBrake(InputAction.CallbackContext context)
    {
        // notImplemented
    }
}
