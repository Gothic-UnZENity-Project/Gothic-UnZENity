using System.Collections.Generic;

namespace GUZ.Core.Model.Marvin
{
    public interface IMarvinPropertyCollector
    {
        IEnumerable<object> CollectMarvinInspectorProperties();
    }
}
