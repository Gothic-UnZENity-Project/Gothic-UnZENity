﻿namespace GUZ.Core.Npc.Actions
{
    public interface IAnimationCallbacks
    {
        public void AnimationCallback(string eventTagDataParam);
        public void AnimationSfxCallback(string eventSfxDataParam);
        public void AnimationMorphCallback(string eventMorphDataParam);
        public void AnimationBlendOutCallback(string eventBlendOutParam);
        public void AnimationEndCallback(string eventEndSignalParam);
    }
}
