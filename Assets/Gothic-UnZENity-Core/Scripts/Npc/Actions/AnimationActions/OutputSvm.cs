using GUZ.Core.Extensions;
using GUZ.Core.Vm;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class OutputSvm : Output
    {
        private string _preparedSvmFileName;

        // Overwriting this lookup as it let's us reuse the inherited Output class.
        protected override string OutputName => _preparedSvmFileName;

        public OutputSvm(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            var svm = VmInstanceManager.TryGetSvmData(Props.NpcInstance.Voice);
            _preparedSvmFileName = svm.GetAudioName(Action.String0);

            base.Start();
        }
    }
}
