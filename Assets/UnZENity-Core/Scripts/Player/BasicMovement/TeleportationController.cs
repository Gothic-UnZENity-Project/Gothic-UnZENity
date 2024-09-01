using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class TeleportationController : MonoBehaviour
{
    [FormerlySerializedAs("baseControllerGameObject")]
    public GameObject BaseControllerGameObject;

    [FormerlySerializedAs("teleportationGameObject")]
    public GameObject TeleportationGameObject;

    [FormerlySerializedAs("player")] public GameObject Player;

    [FormerlySerializedAs("teleportActivationReference")]
    public InputActionReference TeleportActivationReference;

    [FormerlySerializedAs("onTeleportActivate")] [Space]
    public UnityEvent OnTeleportActivate;

    [FormerlySerializedAs("onTeleportCanceled")]
    public UnityEvent OnTeleportCanceled;

    private void Start()
    {
        TeleportActivationReference.action.performed += TeleportModeActivate;
        TeleportActivationReference.action.canceled += TeleportModeCancel;
    }

    private void TeleportModeActivate(InputAction.CallbackContext obj)
    {
        OnTeleportActivate.Invoke();
    }

    private void DeactivateTeleporter()
    {
        OnTeleportCanceled.Invoke();
    }

    private void TeleportModeCancel(InputAction.CallbackContext obj)
    {
        Invoke(nameof(DeactivateTeleporter), .1f);
    }

    private void OnDestroy()
    {
        TeleportActivationReference.action.performed -= TeleportModeActivate;
        TeleportActivationReference.action.canceled -= TeleportModeCancel;
    }
}
