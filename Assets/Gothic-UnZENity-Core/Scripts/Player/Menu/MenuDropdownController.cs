using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Globals;
using GUZ.Core.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace GUZ.Core.Player.Menu
{
    public class WorldSelectorDropdownController : SingletonBehaviour<WorldSelectorDropdownController>
    {
        private Dictionary<string, string> _waypoints = new();

        [FormerlySerializedAs("waypointDropdown")] [SerializeField]
        private TMP_Dropdown _waypointDropdown;

        private void Start()
        {
            SetWaypointDropdown();
        }

        private void SetWaypointDropdown()
        {
            _waypoints = new Dictionary<string, string>
            {
                { "START", "Start" },
                { "ENTRANCE_SURFACE_OLDMINE", "Entrance Old Mine" },
                { "ENTRANCE_FREEMINECAMP_FREEMINE", "Entrance Free Mine" },
                { "ENTRANCE_SURFACE_ORCGRAVEYARD", "Entrance Orc Graveyard" },
                { "ENTRANCE_SURFACE_ORCTEMPLE", "Entrance Orc Temple" },
                { "OCC_CHAPEL_UPSTAIRS", "Old Camp" },
                { "NC_KDW_CAVE_CENTER", "New Camp" },
                { "PSI_TEMPLE_COURT_GURU", "Sect Camp" },
                { "DT_E2_06", "Xardas' Tower" }
            };

            WaypointSetDropdownValues();
            _waypointDropdown.onValueChanged.AddListener(WaypointDropdownItemSelected);
            _waypointDropdown.value = _waypoints.Keys.ToList().IndexOf(Constants.SelectedWaypoint);
        }

        public void WaypointSetDropdownValues()
        {
            _waypointDropdown.options.Clear();

            foreach (var item in _waypoints)
            {
                _waypointDropdown.options.Add(new TMP_Dropdown.OptionData { text = item.Value });
            }
        }

        private void WaypointDropdownItemSelected(int value)
        {
            Constants.SelectedWaypoint = _waypoints.Keys.ElementAt(value);
        }
    }
}
