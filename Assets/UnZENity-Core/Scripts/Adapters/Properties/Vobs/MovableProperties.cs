using GUZ.Core.Const;
using ZenKit.Vobs;

namespace GUZ.Core.Adapters.Properties.Vobs
{
    public class MovableProperties : VobProperties2
    {
        public MovableProperties(IVirtualObject vob) : base(vob)
        { }
        
        public override string GetFocusName()
        {
            var nameSymbol = GameData.GothicVm.GetSymbolByName($"MOBNAME_{VobAs<IMovableObject>()?.FocusName}");

            if (nameSymbol == null)
                return string.Empty;
            else
                return nameSymbol.GetString(0);
        }
    }
}
