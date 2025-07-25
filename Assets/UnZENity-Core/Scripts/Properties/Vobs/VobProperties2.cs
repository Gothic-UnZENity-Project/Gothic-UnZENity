using System.Linq;
using ZenKit.Vobs;

namespace GUZ.Core.Properties.Vobs
{
    public class VobProperties2
    {
        protected readonly IVirtualObject Vob;

        public VobProperties2(IVirtualObject vob)
        {
            Vob = vob;
        }

        /// <summary>
        /// It's some hidden magic. Created based on IVirtualObject.Visual by extracting the first part.
        /// Because within Daedalus there are functions requesting it. e.g. Wld_IsMobAvailable (self,"BED")
        /// </summary>
        public string GetVisualScheme()
        {
            return Vob.Visual?.Name.Split('_').First(); // e.g. BED_1_OC.ASC => BED
        }
        
        public virtual string GetFocusName()
        {
            return Vob?.Name;
        }
        
        /// <summary>
        /// Shorthand function.
        /// </summary>
        protected T VobAs<T>() where T : IVirtualObject
        {
            return (T)Vob;
        }
    }
}
