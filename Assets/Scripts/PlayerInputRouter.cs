using System;
using UnityEngine;

public static class PlayerInputRouter
{
    public static bool UsingController { get; private set; }

    static Vector3 lastMousePosition;
    static bool started;

    static float lastR2;
    static float lastL2;

    const float mouseMoveThreshold = 0.25f;
    const float stickThreshold = 0.25f;

    public static void UpdateInputType()
    {
        Vector3 mousePosition = Input.mousePosition;

        if (!started)
        {
            started = true;
            lastMousePosition = mousePosition;
        }

        if ((mousePosition - lastMousePosition).sqrMagnitude > mouseMoveThreshold)
        {
            UsingController = false;
            lastMousePosition = mousePosition;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            UsingController = false;

        if (Mathf.Abs(GetAxisRawSafe("Horizontal")) > stickThreshold ||
            Mathf.Abs(GetAxisRawSafe("Vertical")) > stickThreshold ||
            Mathf.Abs(GetAxisRawSafe("RightStickHorizontal")) > stickThreshold ||
            Mathf.Abs(GetAxisRawSafe("RightStickVertical")) > stickThreshold ||
            Mathf.Abs(GetAxisRawSafe("R2")) > stickThreshold ||
            Mathf.Abs(GetAxisRawSafe("L2")) > stickThreshold)
        {
            UsingController = true;
        }

        for (int i = 0; i <= 19; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + i)))
            {
                UsingController = true;
                break;
            }
        }
    }

    public static Vector2 GetMoveInput()
    {
        UpdateInputType();

        Vector2 keyboard = new Vector2(
            GetKeyboardAxis(KeyCode.A, KeyCode.D),
            GetKeyboardAxis(KeyCode.S, KeyCode.W)
        );

        Vector2 stick = new Vector2(
            GetAxisRawSafe("Horizontal"),
            GetAxisRawSafe("Vertical")
        );

        if (stick.magnitude > stickThreshold)
            return Vector2.ClampMagnitude(stick, 1f);

        return Vector2.ClampMagnitude(keyboard, 1f);
    }

    public static Vector2 GetAimStick()
    {
        UpdateInputType();

        Vector2 stick = new Vector2(
            GetAxisRawSafe("RightStickHorizontal"),
            GetAxisRawSafe("RightStickVertical")
        );

        if (stick.magnitude < stickThreshold)
            return Vector2.zero;

        return Vector2.ClampMagnitude(stick, 1f);
    }

    public static bool GetLightAttackDown()
    {
        UpdateInputType();
        return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.JoystickButton5);
    }

    public static bool GetHeavyAttackDown()
    {
        UpdateInputType();
        return Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.JoystickButton4);
    }

    public static bool GetDashDown()
    {
        UpdateInputType();
        return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.JoystickButton1);
    }

    public static bool GetTombInteractDown()
    {
        UpdateInputType();
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0);
    }

    public static bool GetActive1Down()
    {
        UpdateInputType();

        return Input.GetKeyDown(KeyCode.E) ||
               Input.GetKeyDown(KeyCode.JoystickButton7) ||
               GetAxisDownSafe("R2");
    }

    public static bool GetActive2Down()
    {
        UpdateInputType();

        return Input.GetKeyDown(KeyCode.Q) ||
               Input.GetKeyDown(KeyCode.JoystickButton6) ||
               GetAxisDownSafe("L2");
    }

    public static bool GetUpgradeLeftDown()
    {
        UpdateInputType();
        return Input.GetKeyDown(KeyCode.LeftArrow);
    }

    public static bool GetUpgradeTopDown()
    {
        UpdateInputType();
        return Input.GetKeyDown(KeyCode.UpArrow);
    }

    public static bool GetUpgradeRightDown()
    {
        UpdateInputType();
        return Input.GetKeyDown(KeyCode.RightArrow);
    }

    static float GetKeyboardAxis(KeyCode negative, KeyCode positive)
    {
        float value = 0f;

        if (Input.GetKey(negative))
            value -= 1f;

        if (Input.GetKey(positive))
            value += 1f;

        return value;
    }

    static bool GetAxisDownSafe(string axisName)
    {
        float current = GetAxisRawSafe(axisName);
        float previous = axisName == "R2" ? lastR2 : lastL2;

        bool down = previous < 0.5f && current >= 0.5f;

        if (axisName == "R2")
            lastR2 = current;
        else if (axisName == "L2")
            lastL2 = current;

        return down;
    }

    public static float GetAxisRawSafe(string axisName)
    {
        try
        {
            return Input.GetAxisRaw(axisName);
        }
        catch (ArgumentException)
        {
            return 0f;
        }
    }
}