using UnityEngine;

using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class PolicyUIManager : MonoBehaviour
{
    public FloodData scriptableObject;
    [SerializeField] private Slider homesFloodedSlider;
    [SerializeField] private Slider casualtiesSlider;
    [SerializeField] private Slider businessesAffectedSlider;
    [SerializeField] private Slider economicLossesSlider;
    [SerializeField] private Slider infrastructureDamageSlider;

    [System.Serializable]
    public struct Policy
    {
        public string name;
        public bool affectsDistricts; // If true, create 3 checkboxes
    }

    public Transform policyListContainer;
    public GameObject policyEntryPrefab;

    private List<Policy> policies = new List<Policy>()
    {
        new Policy { name = "Zoning & Land Use Regulations", affectsDistricts = true },
        new Policy { name = "Mandatory Flood Insurance", affectsDistricts = true },
        new Policy { name = "Flood Infrastructure", affectsDistricts = false },
        new Policy { name = "Flood Early Warning Systems", affectsDistricts = false },
        new Policy { name = "Natural Flood Management", affectsDistricts = false },
        new Policy { name = "Education & Community Preparedness", affectsDistricts = false },
        new Policy { name = "Climate Adaptation & Resilience", affectsDistricts = false },
        new Policy { name = "Retreat & Relocation Programs", affectsDistricts = true },
        new Policy { name = "Property-Level Flood Protection", affectsDistricts = true },
        new Policy { name = "Emergency Disaster Funds & Relief Programs", affectsDistricts = true }
    };

    void Start()
    {
        GeneratePolicyUI();
    }

    void GeneratePolicyUI()
    {
        foreach (Policy policy in policies)
        {
            GameObject entry = Instantiate(policyEntryPrefab, policyListContainer);
            entry.transform.Find("PolicyName").GetComponent<TMP_Text>().text = policy.name;

            if (policy.affectsDistricts)
            {
                entry.transform.Find("ResidentsCheckbox").gameObject.SetActive(true);
                entry.transform.Find("CorporateCheckbox").gameObject.SetActive(true);
                entry.transform.Find("PoliticalCheckbox").gameObject.SetActive(true);
            }
            else
            {
                entry.transform.Find("ResidentsCheckbox").gameObject.SetActive(false);
                entry.transform.Find("CorporateCheckbox").gameObject.SetActive(false);
                entry.transform.Find("PoliticalCheckbox").gameObject.SetActive(false);
            }
        }
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(policyListContainer.GetComponent<RectTransform>());
    }

    public void UpdateHomesFlooded(float newValue)
    {
        scriptableObject.homesFloodedPercent = newValue*100;
    }

    public void UpdateCasualties(float newValue)
    {
        scriptableObject.casualties = newValue*100;
    }

    public void UpdateBusinessesAffected(float newValue)
    {
        scriptableObject.businessesAffectedPercent = newValue*100;
    }

    public void UpdateEconomicLosses(float newValue)
    {
        scriptableObject.economicLosses = newValue*100;
    }

    public void UpdateInfrastructureDamage(float newValue)
    {
        scriptableObject.infrastructureDamagePercent = newValue*100;
    }

    public void UpdateUtilityDowntime(float newValue)
    {
        scriptableObject.utilityDowntimeHours = newValue;
    }
}

