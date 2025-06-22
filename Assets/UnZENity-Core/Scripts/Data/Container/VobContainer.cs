using System;
using GUZ.Core.Properties.Vobs;
using UnityEngine;
using ZenKit.Vobs;

namespace GUZ.Core.Data.Container
{
    public class VobContainer
    {
        public IVirtualObject Vob;

        /// <summary>
        /// Unity Properties which are needed at runtime, but aren't handled within ZenKit's VOB data (e.g., non-save relevant).
        /// </summary>
        public readonly VobProperties2 Props;
        public GameObject Go;


        public VobContainer(IVirtualObject vob)
        {
            Vob = vob;
            
            switch (vob.Type)
            {
                case VirtualObjectType.oCMobLadder:
                    Props = new LadderProperties(vob);
                    break;
                case VirtualObjectType.oCItem:
                    Props = new VobItemProperties2(vob);
                    break;
                case VirtualObjectType.oCMobFire:
                case VirtualObjectType.oCMobInter:
                    Props = new InteractiveProperties(vob);
                    break;
                case VirtualObjectType.oCMobBed:
                    Props = new BedProperties(vob);
                    break;
                case VirtualObjectType.oCMobDoor:
                    Props = new BedProperties(vob);
                    break;
                case VirtualObjectType.oCMobContainer:
                case VirtualObjectType.oCMobSwitch:
                    Props = new SwitchProperties(vob);
                    break;
                case VirtualObjectType.oCMobWheel:
                    Props = new WheelProperties(vob);
                    break;
                default:
                    Props = new VobProperties2(vob);
                    break;
            }
        }
        
        /// <summary>
        /// Shorthand function.
        /// </summary>
        public T VobAs<T>() where T : IVirtualObject
        {
            return (T)Vob;
        }

        /// <summary>
        /// Shorthand function.
        /// </summary>
        public T PropsAs<T>() where T : VobProperties2
        {
            return (T)Props;
        }
    }
}
