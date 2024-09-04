using UnityEngine;

namespace GUZ.Core.Properties
{
    public abstract class AbstractProperties : MonoBehaviour
    {
        public GameObject Go => gameObject;


        public abstract string GetFocusName();
    }
}
