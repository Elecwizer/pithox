using System;
using UnityEngine;

public static class PlayerInputRouter
{
    public static bool UsingController { get; private set; }
    public static bool GameplayInputBlocked { get; private set; }

    static Vector3 lastMousePosition;
    static bool started;

    const float mouseMoveThreshold = 0.25f;
    const float stickThreshold = 0.25f;

    const KeyCode PS5_Square = KeyCode.JoystickButton0;
    const KeyCode PS5_Cross = KeyCode.JoystickButton1;
    const KeyCode PS5_Circle = KeyCode.JoystickButton2;
    const KeyCode PS5_Triangle = KeyCode.JoystickButton3;
    const KeyCode PS5_L1 = KeyCode.JoystickButton4;
    const KeyCode PS5_R1 = KeyCode.JoystickButton5;
    const KeyCode PS5_L2 = KeyCode.JoystickButton6;
    const KeyCode PS5_R2 = KeyCode.JoystickButton7;

    public static void SetGameplayInputBlocked(bool blocked)
    {
        GameplayInputBlocked = blocked;
    }

    public static void UpdateInputType()
    {
        Vector3 mousePosition = Input.mousePosition;

        if (!started)
        {
            started = true;
            lastMousePosition = mousePosition;
        }

        if (!UsingController && (mousePosition - lastMousePosition).sqrMagnitude > mouseMoveThreshold)
        {
            UsingController = false;
            lastMousePosition = mousePosition;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            UsingController = false;
            lastMousePosition = mousePosition;
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
            UsingController = false;

        if (Mathf.Abs(GetAxisRawSafe("Horizontal")) > stickThreshold ||
            Mathf.Abs(GetAxisRawSafe("Vertical")) > stickThreshold ||
            Mathf.Abs(GetAxisRawSafe("RightStickHorizontal")) > stickThreshold ||
            Mathf.Abs(GetAxisRawSafe("RightStickVertical")) > stickThreshold)
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

        if (GameplayInputBlocked)
            return Vector2.zero;

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

        if (GameplayInputBlocked)
            return false;

        return Input.GetMouseButtonDown(0) || Input.GetKeyDown(PS5_R1);
    }

    public static bool GetHeavyAttackDown()
    {
        UpdateInputType();

        if (GameplayInputBlocked)
            return false;

        return Input.GetMouseButtonDown(1) || Input.GetKeyDown(PS5_L1);
    }

    public static bool GetDashDown()
    {
        UpdateInputType();

        if (GameplayInputBlocked)
            return false;

        return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(PS5_Circle);
    }

    public static bool GetTombInteractDown()
    {
        UpdateInputType();

        if (GameplayInputBlocked)
            return false;

        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(PS5_Cross);
    }

    public static bool GetActive1Down()
    {
        UpdateInputType();

        if (GameplayInputBlocked)
            return false;

        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(PS5_R2);
    }

    public static bool GetActive2Down()
    {
        UpdateInputType();

        if (GameplayInputBlocked)
            return false;

        return Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(PS5_L2);
    }

    public static bool GetUpgradeLeftDown()
    {
        UpdateInputType();
        return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(PS5_Square);
    }

    public static bool GetUpgradeTopDown()
    {
        UpdateInputType();
        return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(PS5_Triangle);
    }

    public static bool GetUpgradeRightDown()
    {
        UpdateInputType();
        return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(PS5_Circle);
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