using UnityEngine;

namespace GUZ.VR.Components.Marvin
{
    public class MarvinRootHandler : MonoBehaviour
    {
        [SerializeField] private GameObject _tabLogs;
        [SerializeField] private GameObject _tabMarvin;
        [SerializeField] private GameObject _tabMarvinSelected;


        private void Start()
        {
            ResetTabs();
        }

        public void OnLogsTabClicked()
        {
            ResetTabs();
            _tabLogs.SetActive(true);
        }

        public void OnMarvinTabClicked()
        {
            ResetTabs();
            _tabMarvin.SetActive(true);
        }

        public void OnTabMarvinSelectedClicked()
        {
            ResetTabs();
            _tabMarvinSelected.SetActive(true);
        }

        private void ResetTabs()
        {
            _tabLogs.SetActive(false);
            _tabMarvin.SetActive(false);
            // _tabMarvinSelected.SetActive(false);
        }
    }
}
