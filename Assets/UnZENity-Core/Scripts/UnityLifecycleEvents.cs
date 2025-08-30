using GUZ.Core.Domain.Culling;
using GUZ.Core.Manager;
using GUZ.Core.Services;
using GUZ.Core.Services.Culling;
using Reflex.Attributes;
using UnityEngine;

namespace GUZ.VR
{
    /// <summary>
    /// Each Service can be added to leverage Unity lifecycle events.
    /// This ensures a central overview of usage.
    /// </summary>
    public class UnityLifecycleEvents : MonoBehaviour
    {
        [Inject] private readonly StationaryLightsManager _lightsManager;
        [Inject] private readonly SkyManager _skyManager;
        [Inject] private readonly BarrierManager _barrierManager;

        [Inject] private readonly NpcMeshCullingService _npcMeshCullingService;
        [Inject] private readonly LoadingManager _loadingManager;
        [Inject] private readonly NpcMeshCullingDomain _npcMeshCullingDomain;
        [Inject] private readonly VobMeshCullingDomain _vobMeshCullingDomain;
        [Inject] private readonly VobSoundCullingDomain _vobSoundCullingDomain;

        [Inject] private readonly SpeechToTextService _speechToTextService;


        private void Update()
        {
            _npcMeshCullingService.Update();
            _loadingManager.Update();
        }

        private void LateUpdate()
        {
            _lightsManager.LateUpdate();
        }

        private void FixedUpdate()
        {
            _barrierManager.FixedUpdate();
        }

        private void OnApplicationQuit()
        {
            Bootstrapper.OnApplicationQuit();

            _npcMeshCullingDomain.OnApplicationQuit();
            _vobMeshCullingDomain.OnApplicationQuit();
            _vobSoundCullingDomain.OnApplicationQuit();

            _speechToTextService.Dispose();
        }


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            _vobMeshCullingDomain.OnDrawGizmos();
        }

        private void OnValidate()
        {
            _skyManager.OnValidate();
        }
#endif
    }
}
