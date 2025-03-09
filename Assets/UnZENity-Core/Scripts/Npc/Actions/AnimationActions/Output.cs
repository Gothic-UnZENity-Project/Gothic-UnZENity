using System.Linq;
using GUZ.Core._Npc2;
using GUZ.Core.Creator;
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using UnityEngine;
using ZenKit.Daedalus;
using Random = UnityEngine.Random;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class Output : AbstractAnimationAction
    {
        protected virtual string OutputName => Action.String0;

        private bool _isHeroSpeaking => Action.Int0 == 0;
        private float _audioPlaySeconds;


        public Output(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
        { }

        public override void Start()
        {
            AudioClip audioClip = SoundCreator.ToAudioClip(OutputName);
            _audioPlaySeconds = audioClip.length;

            // Hero
            if (_isHeroSpeaking)
            {
                // If NPC talked before, we stop it immediately (As some audio samples are shorter than the actual animation)
                AnimationCreator.StopAnimation(NpcGo);

                GameGlobals.Npcs.GetHeroGameObject().GetComponent<AudioSource>().PlayOneShot(audioClip);

                PrintDialog();
            }
            // NPC
            else
            {
                var gestureCount = GetDialogGestureCount();
                var randomId = Random.Range(1, gestureCount + 1);

                PrefabProps.AnimationHandler.PlayAnimation($"T_DIALOGGESTURE_{randomId:00}");
                AnimationCreator.PlayHeadMorphAnimation(NpcContainer, HeadMorph.HeadMorphType.Viseme);

                PrefabProps.NpcSound.PlayOneShot(audioClip);

                PrintDialog();
            }
        }

        private void PrintDialog()
        {
            var currentMessage = GameData.Dialogs.CutsceneLibrary.Blocks.Find(x => x.Name == OutputName).Message;
            var globalHero = (NpcInstance)GameData.GothicVm.GlobalHero!;

            if (_isHeroSpeaking)
            {
                // TODO - We could also show subtitles somewhere next to Hero (== ourself/main camera)
                GameContext.SubtitlesAdapter.FillSubtitles(globalHero.GetName(NpcNameSlot.Slot0), currentMessage.Text);
            }
            else
            {
                GameContext.SubtitlesAdapter.FillSubtitles(NpcInstance.GetName(NpcNameSlot.Slot0), currentMessage.Text);
            }

            GameContext.SubtitlesAdapter.ShowSubtitles(NpcGo);
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
                var mds = ResourceLoader.TryGetModelScript(Props.MdsNameBase);

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
                GameGlobals.Npcs.GetHeroGameObject().GetComponent<AudioSource>().Stop();
            }
            // NPC
            else
            {
                PrefabProps.NpcSound.Stop();
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
                    AnimationCreator.StopHeadMorphAnimation(NpcContainer, HeadMorph.HeadMorphType.Viseme);
                }

                GameContext.SubtitlesAdapter.HideSubtitles();
                return true;
            }

            return false;
        }
    }
}
