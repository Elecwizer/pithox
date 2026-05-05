using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pithox.Player
{
    public enum PlayerInputDevice
    {
        KeyboardMouse,
        Controller
    }

    public static class PlayerInputRouter
    {
        static readonly HashSet<string> missingAxes = new HashSet<string>();
        static Vector3 lastMousePosition;
        static bool mouseStarted;
        static int lastTickFrame = -1;

        public static PlayerInputDevice LastDevice { get; private set; } = PlayerInputDevice.KeyboardMouse;
        public static bool UsingController => LastDevice == PlayerInputDevice.Controller;

        public static void Tick()
        {
            if (lastTickFrame == Time.frameCount)
                return;

            lastTickFrame = Time.frameCount;

            Vector3 mousePosition = Input.mousePosition;

            if (!mouseStarted)
            {
                lastMousePosition = mousePosition;
                mouseStarted = true;
            }
            else
            {
                if ((mousePosition - lastMousePosition).sqrMagnitude > 0.25f)
                    MarkKeyboardMouse();

                lastMousePosition = mousePosition;
            }

            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                MarkKeyboardMouse();

            for (int i = 0; i <= 19; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + i)))
                {
                    MarkController();
                    break;
                }
            }
        }

        public static void MarkKeyboardMouse()
        {
            LastDevice = PlayerInputDevice.KeyboardMouse;
        }

        public static void MarkController()
        {
            LastDevice = PlayerInputDevice.Controller;
        }

        public static float GetAxisRaw(string axisName)
        {
            if (string.IsNullOrEmpty(axisName) || missingAxes.Contains(axisName))
                return 0f;

            try
            {
                return Input.GetAxisRaw(axisName);
            }
            catch (ArgumentException)
            {
                missingAxes.Add(axisName);
                return 0f;
            }
        }
    }
}
