using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Creator;
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class Output : AbstractAnimationAction
    {
        protected virtual string OutputName => Action.String0;

        private bool _isHeroSpeaking => Action.Int0 == 0;
        private float _audioPlaySeconds;


        public Output(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        { }

        public override void Start()
        {
            var x = LookupCache.NpcCache;

            var soundData = ResourceLoader.TryGetSound(OutputName);
            var audioClip = SoundCreator.ToAudioClip(soundData);
            _audioPlaySeconds = audioClip.length;

            // Hero
            if (_isHeroSpeaking)
            {
                // If NPC talked before, we stop it immediately (As some audio samples are shorter than the actual animation)
                AnimationCreator.StopAnimation(NpcGo);

                NpcHelper.GetHeroGameObject().GetComponent<AudioSource>().PlayOneShot(audioClip);
                // FIXME - Show subtitles somewhere next to Hero (== ourself/main camera)
            }
            // NPC
            else
            {
                var gestureCount = GetDialogGestureCount();
                var randomId = Random.Range(1, gestureCount + 1);

                AnimationCreator.PlayAnimation(Props.MdsNames, $"T_DIALOGGESTURE_{randomId:00}", NpcGo);
                AnimationCreator.PlayHeadMorphAnimation(Props, HeadMorph.HeadMorphType.Viseme);

                Props.NpcSound.PlayOneShot(audioClip);

                // FIXME - Show subtitles above NPC
            }
        }

        /// <summary>
        /// Gothic1 and Gothic 2 have different amount of Gestures. As we cached all animation names, we iterate through them once and return its number.
        /// </summary>
        private int GetDialogGestureCount()
        {
            if (GameData.Dialogs.GestureCount == 0)
            {
                // FIXME - We might need to check overlayMds and baseMds
                // FIXME - We might need to save amount of gestures based on mds names (if they differ for e.g. humans and orcs)
                var mds = ResourceLoader.TryGetModelScript(Props.BaseMdsName);

                GameData.Dialogs.GestureCount = mds.Animations
                    .Count(anim => anim.Name.StartsWithIgnoreCase("T_DIALOGGESTURE_"));
            }

            return GameData.Dialogs.GestureCount;
        }

        public override void StopImmediately()
        {
            _audioPlaySeconds = 0f;

            if (_isHeroSpeaking)
            {
                NpcHelper.GetHeroGameObject().GetComponent<AudioSource>().Stop();
            }
            // NPC
            else
            {
                Props.NpcSound.Stop();
                AnimationCreator.StopAnimation(NpcGo);
            }
        }

        public override bool IsFinished()
        {
            _audioPlaySeconds -= Time.deltaTime;

            if (_audioPlaySeconds <= 0f)
            {
                // NPC
                if (!_isHeroSpeaking)
                {
                    AnimationCreator.StopHeadMorphAnimation(Props, HeadMorph.HeadMorphType.Viseme);
                }

                return true;
            }

            return false;
        }
    }
}
