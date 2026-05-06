using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Pithox.Player;
using Pithox.Combat;

namespace Pithox.Game
{
    public class UpgradePanel : MonoBehaviour
    {
        [Serializable]
        public class ActiveUpgradeOption
        {
            public string label;
            [TextArea(2, 4)] public string description;
            public GameObject abilityPrefab;
        }

        struct ChoiceOption
        {
            public string Label;
            public string Body;
            public bool CanChoose;
            public Action Apply;
            public PlayerPassiveController.PassiveUpgradeOption PassiveOption;
            public GameObject ActivePrefab;

            public ChoiceOption(
                string label,
                string body,
                bool canChoose,
                Action apply,
                PlayerPassiveController.PassiveUpgradeOption passiveOption,
                GameObject activePrefab
            )
            {
                Label = label;
                Body = body;
                CanChoose = canChoose;
                Apply = apply;
                PassiveOption = passiveOption;
                ActivePrefab = activePrefab;
            }
        }

        [SerializeField] PlayerPassiveController passiveController;
        [SerializeField] global::PlayerActiveAbilitySlots activeSlots;

        [Header("UI")]
        [SerializeField] GameObject panelRoot;
        [SerializeField] GameObject[] choiceCards = new GameObject[3];
        [SerializeField] TMP_Text[] choiceLabels = new TMP_Text[3];

        [Header("Active Upgrade Pool")]
        [SerializeField] ActiveUpgradeOption[] activeOptions;

        [Header("Controller D-Pad Optional")]
        [SerializeField] string dpadHorizontalAxis = "DPadHorizontal";
        [SerializeField] string dpadVerticalAxis = "DPadVertical";
        [SerializeField] int preferredStartSlot = 1;

        ChoiceOption[] currentChoices = new ChoiceOption[3];
        int focusedSlot;

        float lastDpadX;
        float lastDpadY;
        bool suppressConfirmThisFrame;

        void Awake()
        {
            if (passiveController == null)
                passiveController = FindAnyObjectByType<PlayerPassiveController>();

            if (activeSlots == null)
                activeSlots = FindAnyObjectByType<global::PlayerActiveAbilitySlots>();

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        void OnEnable()
        {
            LevelManager.OnLevelUp += HandleLevelUp;
        }

        void OnDisable()
        {
            LevelManager.OnLevelUp -= HandleLevelUp;
            global::PlayerInputRouter.SetGameplayInputBlocked(false);
        }

        void Update()
        {
            if (panelRoot == null || !panelRoot.activeSelf)
                return;

            float dpadX = GetAxisRawSafe(dpadHorizontalAxis);
            float dpadY = GetAxisRawSafe(dpadVerticalAxis);

            bool dpadLeft = lastDpadX > -0.5f && dpadX <= -0.5f;
            bool dpadRight = lastDpadX < 0.5f && dpadX >= 0.5f;
            bool dpadUp = lastDpadY < 0.5f && dpadY >= 0.5f;

            lastDpadX = dpadX;
            lastDpadY = dpadY;

            if (suppressConfirmThisFrame)
            {
                suppressConfirmThisFrame = false;
                return;
            }

            if (global::PlayerInputRouter.GetUpgradeLeftDown() || dpadLeft || Input.GetKeyDown(KeyCode.LeftArrow))
                ChooseSlot(0);

            if (global::PlayerInputRouter.GetUpgradeConfirmDown() || dpadUp || Input.GetKeyDown(KeyCode.UpArrow))
                ChooseSlot(1);

            if (global::PlayerInputRouter.GetUpgradeRightDown() || dpadRight || Input.GetKeyDown(KeyCode.RightArrow))
                ChooseSlot(2);
        }

        void HandleLevelUp(int newLevel)
        {
            ShowChoices();
        }

        public void ShowChoices()
        {
            HashSet<PlayerPassiveController.PassiveUpgradeOption> usedPassives = new HashSet<PlayerPassiveController.PassiveUpgradeOption>();
            HashSet<GameObject> usedActives = new HashSet<GameObject>();

            currentChoices[0] = CreatePassiveChoice(usedPassives);
            AddUsedChoice(currentChoices[0], usedPassives, usedActives);

            currentChoices[1] = CreateActiveChoice(usedActives);
            AddUsedChoice(currentChoices[1], usedPassives, usedActives);

            currentChoices[2] = CreateRandomChoice(usedPassives, usedActives);

            for (int i = 0; i < currentChoices.Length; i++)
            {
                if (i < choiceLabels.Length && choiceLabels[i] != null)
                    choiceLabels[i].text = currentChoices[i].Label;

                UpgradeChoiceSlotPresenter presenter = GetCardPresenter(i);
                if (presenter != null)
                    presenter.Bind(currentChoices[i].Label, currentChoices[i].Body, currentChoices[i].CanChoose);
                else
                    TryBindFallbackTexts(i, currentChoices[i].Label, currentChoices[i].Body);
            }

            if (panelRoot != null)
                panelRoot.SetActive(true);

            global::PlayerInputRouter.SetGameplayInputBlocked(true);

            lastDpadX = 0f;
            lastDpadY = 0f;
            suppressConfirmThisFrame = true;
            RefreshHighlights(false);

            Time.timeScale = 0f;
        }

        ChoiceOption CreatePassiveChoice(HashSet<PlayerPassiveController.PassiveUpgradeOption> usedPassives)
        {
            if (passiveController == null)
                return EmptyChoice("No Passive");

            List<PlayerPassiveController.PassiveUpgradeOption> available = passiveController.GetAvailablePassives(usedPassives);

            if (available.Count <= 0)
                return EmptyChoice("No Passive");

            PlayerPassiveController.PassiveUpgradeOption option = available[UnityEngine.Random.Range(0, available.Count)];

            return new ChoiceOption(
                option.label,
                BuildPassiveDescription(option),
                true,
                () => passiveController.ApplyPassive(option),
                option,
                null
            );
        }

        ChoiceOption CreateActiveChoice(HashSet<GameObject> usedActives)
        {
            ActiveUpgradeOption option = PickActiveOption(usedActives);

            if (option == null)
                return EmptyChoice("No Active Yet");

            string label = string.IsNullOrEmpty(option.label) ? "Active Ability" : option.label;

            return new ChoiceOption(
                label,
                BuildActiveDescription(option),
                true,
                () =>
                {
                    if (activeSlots != null)
                        activeSlots.TryAddActiveAbility(option.abilityPrefab);
                },
                null,
                option.abilityPrefab
            );
        }

        ChoiceOption CreateRandomChoice(
            HashSet<PlayerPassiveController.PassiveUpgradeOption> usedPassives,
            HashSet<GameObject> usedActives
        )
        {
            bool canPassive = passiveController != null && passiveController.GetAvailablePassives(usedPassives).Count > 0;
            bool canActive = HasActiveOptions(usedActives);

            if (canPassive && canActive)
            {
                if (UnityEngine.Random.value < 0.5f)
                    return CreatePassiveChoice(usedPassives);

                return CreateActiveChoice(usedActives);
            }

            if (canPassive)
                return CreatePassiveChoice(usedPassives);

            if (canActive)
                return CreateActiveChoice(usedActives);

            return EmptyChoice("No Upgrade");
        }

        ActiveUpgradeOption PickActiveOption(HashSet<GameObject> usedActives)
        {
            if (activeOptions == null || activeOptions.Length <= 0)
                return null;

            List<ActiveUpgradeOption> available = new List<ActiveUpgradeOption>();

            for (int i = 0; i < activeOptions.Length; i++)
            {
                ActiveUpgradeOption option = activeOptions[i];

                if (option == null)
                    continue;

                if (option.abilityPrefab == null)
                    continue;

                if (usedActives != null && usedActives.Contains(option.abilityPrefab))
                    continue;

                if (activeSlots != null && !activeSlots.CanAddActiveAbility(option.abilityPrefab))
                    continue;

                available.Add(option);
            }

            if (available.Count <= 0)
                return null;

            return available[UnityEngine.Random.Range(0, available.Count)];
        }

        bool HasActiveOptions(HashSet<GameObject> usedActives)
        {
            return PickActiveOption(usedActives) != null;
        }

        void AddUsedChoice(
            ChoiceOption choice,
            HashSet<PlayerPassiveController.PassiveUpgradeOption> usedPassives,
            HashSet<GameObject> usedActives
        )
        {
            if (choice.PassiveOption != null)
                usedPassives.Add(choice.PassiveOption);

            if (choice.ActivePrefab != null)
                usedActives.Add(choice.ActivePrefab);
        }

        ChoiceOption EmptyChoice(string label)
        {
            return new ChoiceOption(label, string.Empty, false, null, null, null);
        }

        void ChooseSlot(int slot)
        {
            if (slot < 0 || slot >= currentChoices.Length)
                return;

            if (!currentChoices[slot].CanChoose)
                return;

            currentChoices[slot].Apply?.Invoke();

            Time.timeScale = 1f;
            global::PlayerInputRouter.SetGameplayInputBlocked(false);

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        string BuildPassiveDescription(PlayerPassiveController.PassiveUpgradeOption option)
        {
            if (option == null)
                return string.Empty;

            switch (option.passiveType)
            {
                case PlayerPassiveController.PassiveType.MoveSpeedPercent:
                    return $"Increases movement speed by {FormatPercent(option.amount)}.";

                case PlayerPassiveController.PassiveType.AttackDamagePercent:
                    return $"Increases attack damage by {FormatPercent(option.amount)}.";

                case PlayerPassiveController.PassiveType.PickupRangeFlat:
                    return $"Increases tombstone pickup range by +{FormatNumber(option.amount)} units.";

                case PlayerPassiveController.PassiveType.StreakWindowFlat:
                    return $"Increases combo timer duration by +{FormatNumber(option.amount)} seconds.";

                case PlayerPassiveController.PassiveType.DashSpeedPercent:
                    return $"Increases dash speed by {FormatPercent(option.amount)}.";

                case PlayerPassiveController.PassiveType.DashCooldownReduction:
                    return $"Reduces dash cooldown by {FormatPercent(option.amount)}.";

                case PlayerPassiveController.PassiveType.HealPercent:
                    return $"Instantly heals {FormatPercent(option.amount)} of max health.";
            }

            return "Improves one of your passive stats.";
        }

        static string FormatPercent(float value)
        {
            return $"{value * 100f:0.#}%";
        }

        static string FormatNumber(float value)
        {
            return value.ToString("0.##");
        }

        string BuildActiveDescription(ActiveUpgradeOption option)
        {
            if (option == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(option.description))
                return option.description.Trim();

            string label = (option.label ?? string.Empty).Trim().ToLowerInvariant();
            string prefabName = option.abilityPrefab != null
                ? option.abilityPrefab.name.Trim().ToLowerInvariant()
                : string.Empty;
            string key = $"{label} {prefabName}";

            if (key.Contains("crystal"))
                return "Unleash 3 crystal waves in front of you. Hits close, mid, and far enemies in sequence.";

            if (key.Contains("shield"))
                return "Become invincible for a short time. The shield breaks after blocking a hit.";

            if (key.Contains("heal"))
                return "Heal over time for a short duration. Taking damage can break the effect early.";

            if (key.Contains("radial") || key.Contains("burst"))
                return "Charge then release a circular blast around you, damaging all nearby enemies.";

            return "Use a powerful active ability with its own cooldown.";
        }

        void FocusFirstAvailable()
        {
            // Always start from the preferred slot (middle by default) so border/hint are visible immediately.
            focusedSlot = Mathf.Clamp(preferredStartSlot, 0, currentChoices.Length - 1);
        }

        void SetFocusedSlot(int slot)
        {
            focusedSlot = Mathf.Clamp(slot, 0, currentChoices.Length - 1);
            RefreshHighlights();
        }

        void MoveFocusedSlot(int delta)
        {
            int count = currentChoices.Length;
            focusedSlot = (focusedSlot + delta + count) % count;

            RefreshHighlights();
        }

        void RefreshHighlights(bool highlighted = false)
        {
            int count = Mathf.Min(choiceCards.Length, currentChoices.Length);
            for (int i = 0; i < count; i++)
            {
                UpgradeChoiceSlotPresenter presenter = GetCardPresenter(i);
                if (presenter == null)
                    continue;

                presenter.SetHighlighted(highlighted);
            }
        }

        UpgradeChoiceSlotPresenter GetCardPresenter(int index)
        {
            if (index < 0 || index >= choiceCards.Length)
                return null;

            GameObject cardObject = choiceCards[index];
            if (cardObject == null)
                return null;

            return cardObject.GetComponent<UpgradeChoiceSlotPresenter>()
                ?? cardObject.GetComponentInChildren<UpgradeChoiceSlotPresenter>(true);
        }

        void TryBindFallbackTexts(int index, string title, string body)
        {
            if (index < 0 || index >= choiceCards.Length)
                return;

            GameObject cardObject = choiceCards[index];
            if (cardObject == null)
                return;

            TMP_Text[] texts = cardObject.GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null)
                    continue;

                string n = texts[i].name.Trim();

                if (string.Equals(n, "Title", StringComparison.OrdinalIgnoreCase))
                    texts[i].text = title ?? string.Empty;
                else if (string.Equals(n, "Body", StringComparison.OrdinalIgnoreCase))
                {
                    bool hasBody = !string.IsNullOrWhiteSpace(body);
                    texts[i].gameObject.SetActive(hasBody);
                    if (hasBody)
                        texts[i].text = body.Trim();
                }
            }
        }

        float GetAxisRawSafe(string axisName)
        {
            if (string.IsNullOrEmpty(axisName))
                return 0f;

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
}