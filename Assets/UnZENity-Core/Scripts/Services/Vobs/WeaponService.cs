using GUZ.Core.Adapters.Vob.Item;
using GUZ.Core.Models.Container;

namespace GUZ.Core.Services.Vobs
{
    public class WeaponService
    {
        private WeaponService()
        {
            GlobalEventDispatcher.FightWindowInitial.AddListener((vobContainer, _) => ChangeTrail(vobContainer, true));
            GlobalEventDispatcher.FightWindowAttack.AddListener((vobContainer, _) => ChangeTrail(vobContainer, false));
        }

        private void ChangeTrail(VobContainer vobContainer, bool enable)
        {
            if (enable)
                vobContainer.Go.GetComponentInChildren<WeaponAdapter>()?.StartTrail();
            else
                vobContainer.Go.GetComponentInChildren<WeaponAdapter>()?.EndTrail();
        }
    }
}
