using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Owns Time.timeScale. Nothing else in the project writes it, so pause, fast-forward and
// the "freeze while a report is open" locks can never fight each other over the value.
public class GameSpeedManager : MonoBehaviour
{
    public static GameSpeedManager Instance { get; private set; }

    public static event Action OnSpeedChanged;

    private static readonly float[] Speeds = { 0f, 1f, 2f, 3f };

    private readonly HashSet<object> pauseLocks = new HashSet<object>();
    private int speedIndex = 1;
    private int lastRunningSpeedIndex = 1;

    public int SpeedIndex => speedIndex;
    public bool IsLocked => pauseLocks.Count > 0;

    private void Awake()
    {
        Instance = this;
        Apply();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || IsLocked)
        {
            return;
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            TogglePause();
        }
        else if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
        {
            SetSpeed(1);
        }
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
        {
            SetSpeed(2);
        }
        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
        {
            SetSpeed(3);
        }
    }

    public void TogglePause()
    {
        SetSpeed(speedIndex == 0 ? lastRunningSpeedIndex : 0);
    }

    public void SetSpeed(int index)
    {
        index = Mathf.Clamp(index, 0, Speeds.Length - 1);
        if (index > 0)
        {
            lastRunningSpeedIndex = index;
        }

        if (index == speedIndex)
        {
            return;
        }

        speedIndex = index;
        Apply();
    }

    public void AddPauseLock(object owner)
    {
        if (owner != null && pauseLocks.Add(owner))
        {
            Apply();
        }
    }

    public void RemovePauseLock(object owner)
    {
        if (owner != null && pauseLocks.Remove(owner))
        {
            Apply();
        }
    }

    private void Apply()
    {
        Time.timeScale = IsLocked ? 0f : Speeds[speedIndex];
        OnSpeedChanged?.Invoke();
    }
}
