using GUZ.Core.Globals;
using ZenKit.Vobs;

namespace GUZ.Core.Properties
{
    public class VobMovableProperties : VobProperties
    {
        public MovableObject MovableProperties => (MovableObject)Properties;

        public override string GetFocusName()
        {
            var nameSymbol = GameData.GothicVm.GetSymbolByName($"MOBNAME_{MovableProperties?.FocusName}");
            return nameSymbol?.GetString(0);
        }
    }
}
