using UnityEngine;

namespace Pithox.Player
{
    public interface IPlayerActiveAbility
    {
        string AbilityName { get; }
        bool CanUse { get; }
        void Use(GameObject owner);
    }

    public class PlayerActiveAbilitySlots : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] KeyCode active1Key = KeyCode.E;
        [SerializeField] KeyCode active2Key = KeyCode.Q;

        [Header("Controller")]
        [SerializeField] bool controllerSupport = true;
        [SerializeField] KeyCode active1Button = KeyCode.JoystickButton7;
        [SerializeField] KeyCode active2Button = KeyCode.JoystickButton6;
        [SerializeField] string active1Axis = "R2";
        [SerializeField] string active2Axis = "L2";
        [SerializeField] float triggerThreshold = 0.5f;

        MonoBehaviour slot1;
        MonoBehaviour slot2;
        bool active1AxisHeld;
        bool active2AxisHeld;

        public IPlayerActiveAbility Active1 => slot1 as IPlayerActiveAbility;
        public IPlayerActiveAbility Active2 => slot2 as IPlayerActiveAbility;
        public bool HasActive1 => Active1 != null;
        public bool HasActive2 => Active2 != null;

        void Update()
        {
            PlayerInputRouter.Tick();

            bool key1 = Input.GetKeyDown(active1Key);
            bool key2 = Input.GetKeyDown(active2Key);
            bool pad1 = controllerSupport && (Input.GetKeyDown(active1Button) || GetTriggerDown(active1Axis, ref active1AxisHeld));
            bool pad2 = controllerSupport && (Input.GetKeyDown(active2Button) || GetTriggerDown(active2Axis, ref active2AxisHeld));

            if (key1 || key2)
                PlayerInputRouter.MarkKeyboardMouse();

            if (pad1 || pad2)
                PlayerInputRouter.MarkController();

            if (key1 || pad1)
                UseSlot(1);

            if (key2 || pad2)
                UseSlot(2);
        }

        bool GetTriggerDown(string axisName, ref bool held)
        {
            float value = Mathf.Abs(PlayerInputRouter.GetAxisRaw(axisName));
            bool pressed = value >= triggerThreshold;
            bool down = pressed && !held;
            held = pressed;
            return down;
        }

        public bool TryAddAbility(MonoBehaviour ability)
        {
            if (ability == null || !(ability is IPlayerActiveAbility))
                return false;

            if (slot1 == null)
            {
                slot1 = ability;
                return true;
            }

            if (slot2 == null)
            {
                slot2 = ability;
                return true;
            }

            return false;
        }

        public void SetSlot1(MonoBehaviour ability)
        {
            slot1 = ability is IPlayerActiveAbility ? ability : null;
        }

        public void SetSlot2(MonoBehaviour ability)
        {
            slot2 = ability is IPlayerActiveAbility ? ability : null;
        }

        public void ClearSlot1()
        {
            slot1 = null;
        }

        public void ClearSlot2()
        {
            slot2 = null;
        }

        public void UseSlot(int slotNumber)
        {
            IPlayerActiveAbility ability = slotNumber == 1 ? Active1 : Active2;

            if (ability == null || !ability.CanUse)
                return;

            ability.Use(gameObject);
        }
    }
}
