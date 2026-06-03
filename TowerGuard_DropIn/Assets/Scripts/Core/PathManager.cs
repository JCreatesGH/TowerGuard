using UnityEngine;

namespace TowerGuard.Core
{
    /// <summary>
    /// Holds the ordered list of waypoints that define the enemy path.
    /// Draws yellow editor gizmos so the path is visible in the Scene view.
    /// </summary>
    public class PathManager : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;

        /// <summary>Returns the waypoints in order (may be null / empty if unassigned).</summary>
        public Transform[] GetWaypoints() => waypoints;

        public int WaypointCount => waypoints != null ? waypoints.Length : 0;

        public Transform GetWaypoint(int index)
        {
            if (waypoints == null || index < 0 || index >= waypoints.Length) return null;
            return waypoints[index];
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Length < 2) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                var a = waypoints[i];
                var b = waypoints[i + 1];
                if (a == null || b == null) continue;
                Gizmos.DrawLine(a.position, b.position);
                Gizmos.DrawSphere(a.position, 0.1f);
            }

            var last = waypoints[waypoints.Length - 1];
            if (last != null)
            {
                Gizmos.DrawSphere(last.position, 0.1f);
            }
        }
    }
}
