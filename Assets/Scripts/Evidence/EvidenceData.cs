using UnityEngine;

public enum EvidenceCategory
{
    Document,
    Testimony,
    Observation,
    Financial,
    Political,
    Historical,
    Physical,
    Contraband
}

[CreateAssetMenu(
    fileName = "Evidence_",
    menuName = "Vajra Astra/Evidence/Evidence Data"
)]
public class EvidenceData : ScriptableObject
{
    [Header("Identity")]
    public string evidenceID;

    public string displayName;

    [Header("Description")]
    [TextArea(3, 8)]
    public string description;

    [Header("Classification")]
    public EvidenceCategory category;

    [Header("Source")]
    public string sourceName;

    [Header("Visual")]
    public Sprite icon;

    [Header("Importance")]
    public bool importantEvidence;

    private void OnValidate()
    {
        if (evidenceID != null)
        {
            evidenceID = evidenceID.Trim();
        }
    }
}