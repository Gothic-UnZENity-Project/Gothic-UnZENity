using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Models.Adapter.Vobs;
using GUZ.Core.Models.Container;
using GUZ.Core.Services.Caches;
using Reflex.Attributes;
using TMPro;
using ZenKit.Daedalus;

namespace GUZ.Lab.Handler
{
    public class LabMonsterHandler : AbstractLabHandler
    {
        public TMP_Dropdown FileSelector;

        [Inject] private readonly MultiTypeCacheService _multiTypeCacheService;

        private List<NpcContainer> _usableMonsters = new();


        public override void Bootstrap()
        {
            var allNames = GameStateService.GothicVm.GetInstanceSymbols("C_Npc").Select(i => i.Name).ToList();

            foreach (var instanceName in allNames)
            {
                var instance = GameStateService.GothicVm.AllocInstance<NpcInstance>(instanceName);

                var npcData = new NpcContainer
                {
                    Instance = instance,
                    Vob =  new NpcAdapter(instance.Index),
                    Props = new()
                    // All other elements aren't needed for Lab usage.
                };

                instance.UserData = npcData;

                _multiTypeCacheService.NpcCache.Add(npcData);
                GameStateService.GothicVm.InitInstance(instance);

                // Only NPCs have an ID && Monsters have no voice.
                if (instance.Id != 0 || instance.Voice != 0)
                    continue;

                _usableMonsters.Add(npcData);
            }

            FileSelector.options = _usableMonsters.Select(i => new TMP_Dropdown.OptionData(i.Vob.Name)).ToList();
        }

        public void MonsterSpawnClick()
        {
            var monsterToSpawn = _usableMonsters[FileSelector.value];

            // FIXME -> Now spawn the monster
        }

        public void MonsterDestroyClick()
        {
            // TODO -> Destroy the monster
        }
    }
}
