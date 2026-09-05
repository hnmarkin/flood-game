using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class Dev_WaterLifecyclePlayModeTests
{
    private GameObject _root;
    private Dev_MapDef _map;
    private Dev_TerrainTypeDef _terrain;
    private Dev_ScenarioDef _scenario;
    private TestModifierProvider _provider;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (_root != null)
            UnityEngine.Object.Destroy(_root);
        if (_map != null)
            UnityEngine.Object.Destroy(_map);
        if (_terrain != null)
            UnityEngine.Object.Destroy(_terrain);
        if (_scenario != null)
            UnityEngine.Object.Destroy(_scenario);

        yield return null;
    }

    [UnityTest]
    public IEnumerator PreliminaryFlooding_CommitsOnceAtConfiguredThreshold()
    {
        Dev_WaterController controller = CreateRuntime(out Dev_WaterLifecycleCoordinator coordinator);

        Assert.That(coordinator.NotifyCompletedPreparationTurn(1), Is.False);
        Assert.That(coordinator.HasAppliedPreliminaryFlooding, Is.False);
        Assert.That(coordinator.NotifyCompletedPreparationTurn(2), Is.True);
        Assert.That(coordinator.HasAppliedPreliminaryFlooding, Is.True);
        Assert.That(coordinator.NotifyCompletedPreparationTurn(3), Is.False);
        Assert.That(controller.ActiveProfileStage, Is.EqualTo(Dev_WaterProfileStage.Preliminary));

        yield return null;
    }

    [UnityTest]
    public IEnumerator PreliminaryFlooding_FailureDoesNotCommitPartialStateAndCanRetry()
    {
        Dev_WaterController controller = CreateRuntime(out _);
        _provider.FailOnCall = 3;

        float before = controller.GetWaterDepth(Vector2Int.zero);
        Assert.That(controller.RunPreliminaryFlooding(0.5f), Is.False);
        Assert.That(controller.ActiveProfileStage, Is.EqualTo(Dev_WaterProfileStage.Baseline));
        Assert.That(controller.GetWaterDepth(Vector2Int.zero), Is.EqualTo(before).Within(0.0001f));

        _provider.FailOnCall = 0;
        Assert.That(controller.RunPreliminaryFlooding(0.5f), Is.True);
        Assert.That(controller.ActiveProfileStage, Is.EqualTo(Dev_WaterProfileStage.Preliminary));

        yield return null;
    }

    [UnityTest]
    public IEnumerator CrisisTransition_DoesNotCommitPhaseWhenProfileFails()
    {
        Dev_WaterController controller = CreateRuntime(out Dev_WaterLifecycleCoordinator coordinator);
        SetPrivateField(_scenario, "crisisProfile", null);
        SetPrivateField(controller, "configurationMode", Dev_WaterConfigurationMode.Production);

        Assert.That(coordinator.NotifyGamePhaseChanged(Dev_WaterGamePhase.Crisis), Is.False);
        Assert.That(controller.GamePhase, Is.EqualTo(Dev_WaterGamePhase.Preparation));

        yield return null;
    }

    private Dev_WaterController CreateRuntime(out Dev_WaterLifecycleCoordinator coordinator)
    {
        _root = new GameObject("Dev_WaterLifecyclePlayModeTests");
        _provider = _root.AddComponent<TestModifierProvider>();
        Dev_WaterController controller = _root.AddComponent<Dev_WaterController>();
        coordinator = _root.AddComponent<Dev_WaterLifecycleCoordinator>();

        CreateMapAndScenario();
        SetPrivateField(controller, "mapDef", _map);
        SetPrivateField(controller, "scenarioDef", _scenario);
        SetPrivateField(controller, "modifierProviderBehaviour", _provider);
        SetPrivateField(controller, "configurationMode", Dev_WaterConfigurationMode.DevDefaultsWithWarnings);
        SetPrivateField(controller, "initializeOnStart", false);
        SetPrivateField(controller, "startOnPlay", false);
        SetPrivateField(coordinator, "waterController", controller);

        Assert.That(controller.InitializeRuntimeState(), Is.True);
        return controller;
    }

    private void CreateMapAndScenario()
    {
        _terrain = ScriptableObject.CreateInstance<Dev_TerrainTypeDef>();
        _terrain.Configure("test", true, 1f, null);

        _map = ScriptableObject.CreateInstance<Dev_MapDef>();
        _map.Configure(Vector2Int.zero, 2, 1);
        _map.TryConfigureCell(Vector2Int.zero, 0, _terrain, 0f, false);
        _map.TryConfigureCell(Vector2Int.right, 0, _terrain, 0f, false);

        Dev_WaterSimulationSettings baselineSettings = CreateSettings();
        Dev_WaterSimulationSettings preliminarySettings = CreateSettings();
        Dev_WaterSimulationSettings crisisSettings = CreateSettings();
        Dev_WaterStormProfile baseline = CreateProfile(baselineSettings);
        Dev_WaterStormProfile preliminary = CreateProfile(preliminarySettings);
        Dev_WaterStormProfile crisis = CreateProfile(crisisSettings);

        _scenario = ScriptableObject.CreateInstance<Dev_ScenarioDef>();
        SetPrivateField(_scenario, "baselineProfile", baseline);
        SetPrivateField(_scenario, "preliminaryProfile", preliminary);
        SetPrivateField(_scenario, "crisisProfile", crisis);
        SetPrivateField(_scenario, "initialSources", Array.Empty<Dev_WaterSourceSpec>());
        SetPrivateField(_scenario, "hasPreliminaryFlooding", true);

        Dev_WaterPreliminaryFloodingConfig preliminaryFlooding = new Dev_WaterPreliminaryFloodingConfig();
        SetPrivateField(preliminaryFlooding, "completedPreparationTurnThreshold", 2);
        SetPrivateField(preliminaryFlooding, "simulatedDuration", 0.5f);
        SetPrivateField(_scenario, "preliminaryFlooding", preliminaryFlooding);
    }

    private static Dev_WaterSimulationSettings CreateSettings()
    {
        return new Dev_WaterSimulationSettings
        {
            dt = 0.25f,
            gravity = 0f,
            friction = 0f,
            maxWaterDepth = 100f,
            useSpreadGating = false,
            baseDrainageDepthPerSecond = 0f,
            windForceScale = 0f
        };
    }

    private static Dev_WaterStormProfile CreateProfile(Dev_WaterSimulationSettings settings)
    {
        Dev_WaterStormProfile profile = new Dev_WaterStormProfile();
        SetPrivateField(profile, "simulationSettings", settings);
        SetPrivateField(profile, "continuousSources", Array.Empty<Dev_WaterSourceSpec>());
        return profile;
    }

    private static void SetPrivateField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing private field {target.GetType().Name}.{name}");
        field.SetValue(target, value);
    }

    private sealed class TestModifierProvider : MonoBehaviour, IDev_WaterModifierProvider
    {
        public int FailOnCall;
        private int _callCount;

        public bool TryGetResolvedWaterModifiers(out Dev_WaterModifierSnapshot modifiers, out string error)
        {
            _callCount++;
            if (FailOnCall > 0 && _callCount == FailOnCall)
            {
                modifiers = default;
                error = "intentional test failure";
                return false;
            }

            modifiers = Dev_WaterModifierSnapshot.Defaults();
            error = null;
            return true;
        }
    }
}
