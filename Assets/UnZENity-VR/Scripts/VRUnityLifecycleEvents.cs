#if GUZ_HVR_INSTALLED
using System;
using GUZ.Core;
using GUZ.Core.UnZENity_Core.Scripts.Services.Context;
using GUZ.VR.Adapter;
using GUZ.VR.Services;
using Reflex.Attributes;
using UnityEngine;

namespace GUZ.VR
{
    /// <summary>
    /// Each Service can be added to leverage Unity lifecycle events.
    /// This ensures central overview of usage.
    /// </summary>
    public class VRUnityLifecycleEvents : MonoBehaviour
    {
        [Inject] private readonly ContextInteractionService _contextInteractionService;
        [Inject] private readonly VRWeaponService _vrWeaponService;

        private void Awake()
        {
            // We need to set our VR service now, as the Player.scene loading time (basically this Awake() call) is the first time,
            // when we can set our VR service. But the [Inject] resolves are done already at frame 0 at Bootstrap.scene.
            // Therefore, we need to set the VR service via new() and as proxy implementation.
            _contextInteractionService.SetImpl(new VRContextInteractionService());
        }

        private void FixedUpdate()
        {
            _vrWeaponService.FixedUpdate();
        }
    }
}
#endif
