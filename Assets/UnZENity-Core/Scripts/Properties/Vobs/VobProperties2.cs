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
