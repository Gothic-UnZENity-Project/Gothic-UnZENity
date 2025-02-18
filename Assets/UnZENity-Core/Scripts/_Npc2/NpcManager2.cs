using System.Threading.Tasks;
using GUZ.Core.Manager;

namespace GUZ.Core._Npc2
{
    /// <summary>
    /// Manage all NPC related calls a(Ext* engine calls and e.g. load Npcs at WorldSceneManager time)
    /// </summary>
    public class NpcManager2
    {
        // Supporter class where the whole Init() logic is outsourced for better readability.
        private NpcInitializer2 _initializer = new ();

        public async Task CreateWorldNpcs(LoadingManager loading)
        {
            await _initializer.InitNPCsNewGame(loading);
        }

        public void ExtWldInsertNpc(int npcInstanceIndex, string spawnPoint)
        {
            _initializer.ExtWldInsertNpc(npcInstanceIndex, spawnPoint);
        }

    }
}
