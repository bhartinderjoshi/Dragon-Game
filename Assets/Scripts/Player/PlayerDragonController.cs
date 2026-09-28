using UnityEngine;
using DragonBattle.Core;

namespace DragonBattle.Player
{
    public class PlayerDragonController : DragonController
    {
        [Header("Player Input Configuration")]
        [SerializeField] private bool useMouseAim = false;
        [SerializeField] private KeyCode ability1Key = KeyCode.Alpha1;
        [SerializeField] private KeyCode ability2Key = KeyCode.Alpha2;
        [SerializeField] private KeyCode ability3Key = KeyCode.Alpha3;
        [SerializeField] private KeyCode altAbility1Key = KeyCode.Q;
        [SerializeField] private KeyCode altAbility2Key = KeyCode.E;
        [SerializeField] private KeyCode altAbility3Key = KeyCode.Space;

        private UnityEngine.Camera mainCam;

        protected override void Awake()
        {
            base.Awake();
            mainCam = UnityEngine.Camera.main;
        }

        private void Update()
        {
            if (!IsAlive) return;

            HandleLocomotionInput();
            HandleAbilityInput();
        }

        private void HandleLocomotionInput()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector3 inputDir = new Vector3(h, 0f, v);

            if (inputDir.sqrMagnitude > 0.001f)
            {
                // Align input with isometric / top-down camera perspective
                if (mainCam == null) mainCam = UnityEngine.Camera.main;
                if (mainCam != null)
                {
                    Vector3 camForward = mainCam.transform.forward;
                    Vector3 camRight = mainCam.transform.right;
                    camForward.y = 0;
                    camRight.y = 0;
                    camForward.Normalize();
                    camRight.Normalize();

                    Vector3 moveDir = camForward * v + camRight * h;
                    if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();
                    Move(moveDir);
                }
                else
                {
                    if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();
                    Move(inputDir);
                }
            }
            else
            {
                Move(Vector3.zero);
            }

            if (useMouseAim && !IsAttacking)
            {
                AimTowardsMouse();
            }
        }

        private void AimTowardsMouse()
        {
            if (mainCam == null) mainCam = UnityEngine.Camera.main;
            if (mainCam == null) return;

            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 lookDir = hitPoint - transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.1f)
                {
                    RotateTowards(lookDir);
                }
            }
        }

        private void HandleAbilityInput()
        {
            if (Input.GetKeyDown(ability1Key) || Input.GetKeyDown(altAbility1Key))
            {
                UseAbility(0);
            }
            else if (Input.GetKeyDown(ability2Key) || Input.GetKeyDown(altAbility2Key))
            {
                UseAbility(1);
            }
            else if (Input.GetKeyDown(ability3Key) || Input.GetKeyDown(altAbility3Key))
            {
                UseAbility(2);
            }
        }
    }
}
