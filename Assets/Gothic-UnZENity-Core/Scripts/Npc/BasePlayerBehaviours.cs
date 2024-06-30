using GUZ.Core.Properties;
using UnityEngine;
using UnityEngine.Serialization;

namespace GUZ.Core.Npc
{
    public abstract class BasePlayerBehaviour : MonoBehaviour
    {
        [FormerlySerializedAs("npcRoot")] public GameObject NpcRoot;
        [FormerlySerializedAs("properties")] public NpcProperties Properties;
    }
}
