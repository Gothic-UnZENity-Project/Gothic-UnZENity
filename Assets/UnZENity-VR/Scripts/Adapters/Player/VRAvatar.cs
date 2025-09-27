using GUZ.Core;
using GUZ.Core.Extensions;
using GUZ.Core.Models.Vm;
using GUZ.Core.Services.Meshes;
using Reflex.Attributes;
using UnityEngine;

namespace GUZ.VR.Adapters.Player
{
    public class VRAvatar : MonoBehaviour
    {
        [SerializeField] private GameObject _leftHand;
        [SerializeField] private GameObject _rightHand;

        [Inject] private MeshService _meshService;

        private void Awake()
        {
            GlobalEventDispatcher.ZenKitBootstrapped.AddListener(Init);
        }

        private void Init()
        {
            var body = new ExtSetVisualBodyData()
            {
                Body = "hum_body_Naked0",
                BodyTexNr = 4,
                BodyTexColor = 1,
                Head = "", // intentionally left blank
                HeadTexNr = 9,
                TeethTexNr = 0,
                Armor = -1
            };

            var heroMesh = _meshService.CreateNpc("PC_Hero", "hum_body_Naked0", "HUMANS.MDS", body, parent: this.gameObject);

            var leftForearm = heroMesh.FindChildRecursively("BIP01 L FOREARM");
            var rightForearm = heroMesh.FindChildRecursively("BIP01 R FOREARM");

            leftForearm.SetParent(_leftHand, true, true);
            rightForearm.SetParent(_rightHand, worldPositionStays: true);
        }
    }
}
