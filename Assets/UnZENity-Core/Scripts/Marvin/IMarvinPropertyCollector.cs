using System.Collections.Generic;

namespace GUZ.Core.Marvin
{
    public interface IMarvinPropertyCollector
    {
        IEnumerable<object> CollectMarvinInspectorProperties();
    }
}
