using UnityEngine;

namespace RedAlert.Units
{
    /// <summary>
    /// Assigns an integer team identifier to objects for FF/FO checks and ownership.
    /// </summary>
    public class Team : MonoBehaviour
    {
        [SerializeField] private int _teamId;

        public int TeamId => _teamId;

        public void DebugSetTeam(int id) { _teamId = id; }
    }
}