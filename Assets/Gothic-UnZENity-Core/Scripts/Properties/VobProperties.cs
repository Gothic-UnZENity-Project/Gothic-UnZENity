using System.Linq;
using UnityEngine;
using ZenKit.Vobs;

namespace GUZ.Core.Properties
{
    public class VobProperties : AbstractProperties
    {
        /// <summary>
        /// It's some hidden magic. Created based on IVirtualObject.Visual by extracting the first part.
        /// Because within Daedalus there are functions requesting it. e.g. Wld_IsMobAvailable (self,"BED")
        /// </summary>
        [field: SerializeField]
        public string VisualScheme { get; private set; }

        public IVirtualObject Properties;


        public virtual void SetData(IVirtualObject data)
        {
            Properties = data;

            if (data?.Visual != null)
            {
                VisualScheme = data.Visual.Name.Split('_').First(); // e.g. BED_1_OC.ASC => BED
            }
        }

        public virtual string GetFocusName()
        {
            return Properties?.Name;
        }

    }
}
