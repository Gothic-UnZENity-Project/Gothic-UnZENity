using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GUZ.Core._Npc2;
using GUZ.Core.Caches;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Extensions;
using GUZ.Core.Npc;
using GUZ.Core.Npc.Actions;
using MyBox;
using UnityEngine;
using ZenKit;
using Animation = UnityEngine.Animation;

namespace GUZ.Core.Creator
{
    [Obsolete("Use GameGlobals.Animations.* instead.")]
    public static class AnimationCreator
    {
        public static void StartBlendOutAnimation(string currentAnimName, float blendOutTime, GameObject go)
        {
            var anim = go.GetComponentInChildren<Animation>();
            anim.Blend(currentAnimName, 0, blendOutTime);
        }

        public static void StopAnimation(GameObject go)
        {
            var animationComp = go.GetComponent<Animation>();

            if (!animationComp || !animationComp.isPlaying)
            {
                return;
            }

            // Rewind workaround to actually set NPC to first frame of the animation.
            // @see: https://forum.unity.com/threads/animation-rewind-not-working.4756/
            animationComp.Rewind();
            animationComp.Play();
            animationComp.Sample();
            animationComp.Stop();
        }

        public static void PlayHeadMorphAnimation(NpcContainer2 npcContainer, HeadMorph.HeadMorphType type)
        {
            npcContainer.PrefabProps.HeadMorph.StartAnimation(npcContainer.Props.BodyData.Head, type);
        }

        public static void StopHeadMorphAnimation(NpcContainer2 npcContainer, HeadMorph.HeadMorphType type)
        {
            npcContainer.PrefabProps.HeadMorph.StopAnimation(type);
        }
    }
}
