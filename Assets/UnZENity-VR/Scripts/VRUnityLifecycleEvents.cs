#if GUZ_HVR_INSTALLED
using GUZ.Core.Services.Context;
using GUZ.VR.Adapter;
using GUZ.VR.Services;
using GUZ.VR.Services.Context;
using Reflex.Attributes;
using UnityEngine;

namespace GUZ.VR
{
    /// <summary>
    /// Each Service can be added to leverage Unity lifecycle events.
    /// This ensures a central overview of usage.
    /// </summary>
    public class VRUnityLifecycleEvents : MonoBehaviour
    {
        [Inject] private readonly ContextInteractionService _contextInteractionService;
        [Inject] private readonly ContextMenuService _contextMenuService;
        [Inject] private readonly ContextDialogService _contextDialogService;

        [Inject] private readonly VRWeaponService _vrWeaponService;

        private void Awake()
        {
            // We need to set our VR service now, as the Player.scene loading time (basically this Awake() call) is the first time,
            // when we can set our VR service. But the [Inject] resolves are done already at frame 0 at Bootstrap.scene.
            // Therefore, we need to set the VR service via new() and as proxy implementation.
            _contextInteractionService.SetImpl(new VRContextInteractionService());
            _contextMenuService.SetImpl(new VRContextMenuService());
            _contextDialogService.SetImpl(new VRContextDialogService());
        }

        private void FixedUpdate()
        {
            _vrWeaponService.FixedUpdate();
        }
    }
}
#endif
