using TMPro;
using UnityEngine;

namespace Pithox.Game
{
    public class UpgradeChoiceSlotPresenter : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] GameObject selectionHighlight;
        [SerializeField] GameObject confirmHint;
        [SerializeField] CanvasGroup cardCanvasGroup;

        void Awake()
        {
            AutoWireIfNeeded();
            SetHighlighted(false);
        }

        void OnValidate()
        {
            AutoWireIfNeeded();
        }

        public void Bind(string title, string body, bool canChoose)
        {
            if (titleText != null)
                titleText.text = title ?? string.Empty;

            if (bodyText != null)
            {
                bool hasBody = !string.IsNullOrWhiteSpace(body);
                bodyText.gameObject.SetActive(hasBody);
                if (hasBody)
                    bodyText.text = body.Trim();
            }

            if (cardCanvasGroup != null)
                cardCanvasGroup.alpha = canChoose ? 1f : 0.45f;
        }

        public void SetHighlighted(bool highlighted)
        {
            if (selectionHighlight != null)
                selectionHighlight.SetActive(highlighted);

            if (confirmHint != null)
                confirmHint.SetActive(highlighted);
        }

        void AutoWireIfNeeded()
        {
            if (titleText == null)
                titleText = FindTextByName("Title");

            if (bodyText == null)
                bodyText = FindTextByName("Body");

            if (selectionHighlight == null)
                selectionHighlight = FindChildByName("SelectionBorder");

            if (confirmHint == null)
                confirmHint = FindChildByName("Hint");
        }

        TMP_Text FindTextByName(string objectName)
        {
            Transform t = FindTransformByName(objectName);
            return t != null ? t.GetComponent<TMP_Text>() : null;
        }

        GameObject FindChildByName(string objectName)
        {
            Transform t = FindTransformByName(objectName);
            return t != null ? t.gameObject : null;
        }

        Transform FindTransformByName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return null;

            Transform[] all = GetComponentsInChildren<Transform>(true);
            string wanted = objectName.Trim();

            // Pass 1: exact match (case-insensitive).
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && string.Equals(all[i].name.Trim(), wanted, System.StringComparison.OrdinalIgnoreCase))
                    return all[i];
            }

            // Pass 2: contains match for minor naming differences (e.g. "Body TMP").
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null)
                    continue;

                if (all[i].name.Trim().IndexOf(wanted, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return all[i];
            }

            return null;
        }
    }
}
