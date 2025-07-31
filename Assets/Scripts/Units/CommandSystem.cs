using System.Collections.Generic;
using UnityEngine;

namespace RedAlert.Units
{
    /// <summary>
    /// Adds Attack-Move: hold A-key (latched) then RMB to issue attack-move destination. Simple mode flag on units.
    /// </summary>
    public class CommandSystem : MonoBehaviour
    {
        [SerializeField] private LayerMask groundMask = -1;
        [SerializeField] private float pickMaxDistance = 1000f;

        private Camera _cam;
        private readonly RaycastHit[] _rayHits = new RaycastHit[8];
        private static readonly List<ISelectable> Buffer = new List<ISelectable>(64);
        private bool _attackMoveLatched;

        private void Awake()
        {
            _cam = Camera.main;
            if (_cam == null) _cam = FindObjectOfType<Camera>();
        }

        private void Update()
        {
            // Latch A-key
            if (Input.GetKeyDown(KeyCode.A)) _attackMoveLatched = true;
            if (Input.GetKeyUp(KeyCode.A)) _attackMoveLatched = false;

            if (Input.GetMouseButtonDown(1))
            {
                Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
                int hitCount = Physics.RaycastNonAlloc(ray, _rayHits, pickMaxDistance, groundMask, QueryTriggerInteraction.Ignore);
                if (hitCount > 0)
                {
                    Vector3 dest = _rayHits[0].point;
                    if (_attackMoveLatched) IssueAttackMove(dest);
                    else IssueMove(dest);
                }
            }
        }

        public void IssueMove(Vector3 worldPos)
        {
            SelectionSystem.GetSelectedNonAlloc(Buffer);
            for (int i = 0; i < Buffer.Count; i++)
            {
                var sel = Buffer[i] as Component;
                if (sel == null) continue;
                if (sel.TryGetComponent<LocomotionAgent>(out var loco))
                {
                    loco.SetDestination(worldPos);
                    if (sel.TryGetComponent<ITacticalMode>(out var t)) t.SetAttackMove(false);
                }
            }
        }

        public void IssueAttackMove(Vector3 worldPos)
        {
            SelectionSystem.GetSelectedNonAlloc(Buffer);
            for (int i = 0; i < Buffer.Count; i++)
            {
                var sel = Buffer[i] as Component;
                if (sel == null) continue;
                if (sel.TryGetComponent<LocomotionAgent>(out var loco))
                {
                    loco.SetDestination(worldPos);
                }
                if (sel.TryGetComponent<ITacticalMode>(out var t))
                {
                    t.SetAttackMove(true);
                }
            }
        }
    }

    /// <summary>Optional interface for units that support attack-move behavior.</summary>
    public interface ITacticalMode
    {
        void SetAttackMove(bool enabled);
    }
}