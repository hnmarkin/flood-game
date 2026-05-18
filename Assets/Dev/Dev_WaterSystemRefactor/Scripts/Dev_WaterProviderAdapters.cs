using System;
using System.Reflection;
using UnityEngine;

public sealed class Dev_WaterBarrierProviderAdapter : Dev_IWaterBarrierProvider
{
    private readonly Dev_IWaterBarrierProvider _direct;
    private readonly object _target;
    private readonly MethodInfo _getBarrierHeightX;
    private readonly MethodInfo _getSeepageX;
    private readonly MethodInfo _getBarrierHeightY;
    private readonly MethodInfo _getSeepageY;
    private readonly MethodInfo _isBlockedX;
    private readonly MethodInfo _isBlockedY;

    public Dev_WaterBarrierProviderAdapter(MonoBehaviour behaviour)
    {
        _direct = behaviour as Dev_IWaterBarrierProvider;
        _target = behaviour;

        if (behaviour == null || _direct != null)
            return;

        Type type = behaviour.GetType();
        _getBarrierHeightX = FindMethod(type, "GetBarrierHeightX");
        _getSeepageX = FindMethod(type, "GetSeepageX");
        _getBarrierHeightY = FindMethod(type, "GetBarrierHeightY");
        _getSeepageY = FindMethod(type, "GetSeepageY");
        _isBlockedX = FindMethod(type, "IsBlockedX");
        _isBlockedY = FindMethod(type, "IsBlockedY");
    }

    public bool HasProvider => _direct != null || _target != null;

    public float GetBarrierHeightX(int x, int y)
    {
        if (_direct != null) return _direct.GetBarrierHeightX(x, y);
        return InvokeFloat(_getBarrierHeightX, x, y);
    }

    public float GetSeepageX(int x, int y)
    {
        if (_direct != null) return _direct.GetSeepageX(x, y);
        return InvokeFloat(_getSeepageX, x, y);
    }

    public float GetBarrierHeightY(int x, int y)
    {
        if (_direct != null) return _direct.GetBarrierHeightY(x, y);
        return InvokeFloat(_getBarrierHeightY, x, y);
    }

    public float GetSeepageY(int x, int y)
    {
        if (_direct != null) return _direct.GetSeepageY(x, y);
        return InvokeFloat(_getSeepageY, x, y);
    }

    public bool IsBlockedX(int x, int y)
    {
        if (_direct != null) return _direct.IsBlockedX(x, y);
        return InvokeBool(_isBlockedX, x, y);
    }

    public bool IsBlockedY(int x, int y)
    {
        if (_direct != null) return _direct.IsBlockedY(x, y);
        return InvokeBool(_isBlockedY, x, y);
    }

    private static MethodInfo FindMethod(Type type, string name)
    {
        return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }

    private float InvokeFloat(MethodInfo method, int x, int y)
    {
        if (method == null || _target == null)
            return 0f;

        object result = method.Invoke(_target, new object[] { x, y });
        return result is float value ? value : 0f;
    }

    private bool InvokeBool(MethodInfo method, int x, int y)
    {
        if (method == null || _target == null)
            return false;

        object result = method.Invoke(_target, new object[] { x, y });
        return result is bool value && value;
    }
}

public sealed class Dev_WaterModifierProviderAdapter : Dev_IWaterModifierProvider
{
    private readonly Dev_IWaterModifierProvider _direct;
    private readonly object _target;
    private readonly MethodInfo _getWaterModifierSnapshot;
    private readonly MethodInfo _getModifierValue;

    public Dev_WaterModifierProviderAdapter(MonoBehaviour behaviour)
    {
        _direct = behaviour as Dev_IWaterModifierProvider;
        _target = behaviour;

        if (behaviour == null || _direct != null)
            return;

        Type type = behaviour.GetType();
        _getWaterModifierSnapshot = type.GetMethod(
            "GetWaterModifierSnapshot",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _getModifierValue = type.GetMethod(
            "GetModifierValue",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(string) },
            null);
    }

    public Dev_WaterModifierSnapshot GetWaterModifierSnapshot()
    {
        if (_direct != null)
            return Sanitize(_direct.GetWaterModifierSnapshot());

        if (_target != null && _getWaterModifierSnapshot != null)
        {
            object result = _getWaterModifierSnapshot.Invoke(_target, Array.Empty<object>());
            if (result is Dev_WaterModifierSnapshot snapshot)
                return Sanitize(snapshot);
        }

        Dev_WaterModifierSnapshot fallback = Dev_WaterModifierSnapshot.Defaults();
        if (_target == null || _getModifierValue == null)
            return fallback;

        fallback.DrainageEfficiency = GetModifierValue("Drainage Efficiency", fallback.DrainageEfficiency);
        fallback.RainfallRate = GetModifierValue("Rainfall Rate", fallback.RainfallRate);
        fallback.AntecedentWetness = GetModifierValue("Antecedent Wetness", fallback.AntecedentWetness);
        fallback.ExternalWaterLoad = GetModifierValue("External Water Load", fallback.ExternalWaterLoad);
        fallback.WindStress = GetModifierValue("Wind Stress", fallback.WindStress);
        fallback.EventPacing = GetModifierValue("Event Pacing", fallback.EventPacing);

        return Sanitize(fallback);
    }

    private float GetModifierValue(string key, float defaultValue)
    {
        object result = _getModifierValue.Invoke(_target, new object[] { key });
        return result is float value ? value : defaultValue;
    }

    private static Dev_WaterModifierSnapshot Sanitize(Dev_WaterModifierSnapshot snapshot)
    {
        snapshot.Sanitize();
        return snapshot;
    }
}
