using GUZ.Core.Globals;
using ZenKit.Daedalus;
using ZenKit.Vobs;

namespace GUZ.Core.Data
{
    public class NpcVob : ZenKit.Vobs.Npc
    {
        public NpcVob(int npcIndex)
        {
            if (npcIndex >= 0)
            {
                Name = GameData.GothicVm.GetSymbolByIndex(npcIndex)!.Name;
                NpcInstance = Name;
            }
    
            Ai = new AiHuman();
            EventManager = new EventManager();
            
            AddSlot().Name = Constants.SlotRightHand;
            AddSlot().Name = Constants.SlotLeftHand;
            AddSlot().Name = Constants.SlotSword;
            AddSlot().Name = Constants.SlotLongsword;
            AddSlot().Name = Constants.SlotBow;
            AddSlot().Name = Constants.SlotCrossbow;
            AddSlot().Name = Constants.SlotHelmet;
            AddSlot().Name = Constants.SlotTorso;
        }

        /// <summary>
        /// If we initialized the NPC for the first time, we need to copy some data into VOB for use at runtime and saving.
        /// </summary>
        public void CopyInstanceData(NpcInstance instance)
        {
            
        }
    }
}
