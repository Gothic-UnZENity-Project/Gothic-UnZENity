using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Adapters.Npc;
using GUZ.Core.Extensions;
using GUZ.Core.Models.Adapter.Vobs;
using GUZ.Core.Models.Container;
using GUZ.Core.Services.Caches;
using GUZ.Core.Services.Npc;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Lab.Handler
{
    public class LabMonsterHandler : AbstractLabHandler
    {
        [SerializeField] private TMP_Dropdown _monsterSelector;
        [SerializeField] private GameObject _spawnPoint;

        [Inject] private readonly MultiTypeCacheService _multiTypeCacheService;
        [Inject] private readonly NpcService _npcService;

        private List<NpcContainer> _usableMonsters = new();


        public override void Bootstrap()
        {
            var allNames = GameStateService.GothicVm.GetInstanceSymbols("C_Npc").Select(i => i.Name).ToList();

            foreach (var instanceName in allNames)
            {
                var instance = GameStateService.GothicVm.AllocInstance<NpcInstance>(instanceName);

                var monsterLoadingGo = new GameObject($"{instanceName} (Loader)");
                monsterLoadingGo.SetParent(_spawnPoint);
                var loaderGoComp = monsterLoadingGo.AddComponent<NpcLoader>();
                loaderGoComp.Npc = instance;

                var npcData = new NpcContainer
                {
                    Go = loaderGoComp.gameObject,
                    Instance = instance,
                    Vob =  new NpcAdapter(instance.Index),
                    Props = new(),
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

            _monsterSelector.options = _usableMonsters.Select(i => new TMP_Dropdown.OptionData(i.Vob.Name)).ToList();
        }

        public void MonsterSpawnClick()
        {
            var monsterToSpawn = _usableMonsters[_monsterSelector.value];

            _npcService.InitNpc(monsterToSpawn.Go, true);

            // FIXME -> Now spawn the monster
        }

        public void MonsterDestroyClick()
        {
            // TODO -> Destroy the monster
            Debug.Log("Reset clicked");
        }
    }
}
