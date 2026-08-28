using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EvidenceEntryUI : MonoBehaviour
{
    [Header("References")]
    public TMP_Text evidenceNameText;
    public TMP_Text evidenceMetaText;

    public Image evidenceIcon;

    public GameObject importantIcon;

    [Header("Optional")]
    public Button button;

    private EvidenceData currentEvidence;

    public EvidenceData CurrentEvidence =>
        currentEvidence;

    public void Setup(EvidenceData evidence)
    {
        currentEvidence = evidence;

        if (evidence == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (evidenceNameText != null)
        {
            evidenceNameText.text =
                evidence.displayName;
        }

        if (evidenceMetaText != null)
        {
            string category =
                evidence.category
                    .ToString()
                    .ToUpper();

            string source =
                string.IsNullOrWhiteSpace(
                    evidence.sourceName)
                    ? ""
                    : evidence.sourceName
                        .Trim()
                        .ToUpper();

            if (string.IsNullOrWhiteSpace(source))
            {
                evidenceMetaText.text =
                    category;
            }
            else
            {
                evidenceMetaText.text =
                    category +
                    " • " +
                    source;
            }
        }

        if (evidenceIcon != null)
        {
            evidenceIcon.sprite =
                evidence.icon;

            evidenceIcon.enabled =
                evidence.icon != null;
        }

        if (importantIcon != null)
        {
            importantIcon.SetActive(
                evidence.importantEvidence
            );
        }
    }
}