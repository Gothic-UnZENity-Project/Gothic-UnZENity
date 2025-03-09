using UnityEngine;
using ZenKit;

namespace GUZ.Core.Data.Container
{
    public class AnimationContainer
    {
        // Combination of "MDS-AnimName"
        public string FullName;
        public IAnimation Animation;
        public AnimationClip Clip;

        // e.g. S_WALKL idle animation is looping.
        public bool IsLooping => Animation.Name == Animation.Next;

        public bool IsMoving;
        public Vector3 MovementSpeed;


        public float Length => Clip.length;
        public float BlendIn => Animation.BlendIn;
        public float BlendOut => Animation.BlendOut;
        public string Next => Animation.Next;
    }
}
