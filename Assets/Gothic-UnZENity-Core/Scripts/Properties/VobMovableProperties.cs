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

            if (nameSymbol == null)
            {
                return string.Empty;
            }
            else
            {
                return nameSymbol.GetString(0);
            }
        }
    }
}
