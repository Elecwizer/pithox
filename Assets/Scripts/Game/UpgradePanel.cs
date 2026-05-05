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
            public GameObject abilityPrefab;
        }

        struct ChoiceOption
        {
            public string Label;
            public bool CanChoose;
            public Action Apply;
            public PlayerPassiveController.PassiveUpgradeOption PassiveOption;
            public GameObject ActivePrefab;

            public ChoiceOption(
                string label,
                bool canChoose,
                Action apply,
                PlayerPassiveController.PassiveUpgradeOption passiveOption,
                GameObject activePrefab
            )
            {
                Label = label;
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
        [SerializeField] Button[] choiceButtons = new Button[3];
        [SerializeField] TMP_Text[] choiceLabels = new TMP_Text[3];

        [Header("Active Upgrade Pool")]
        [SerializeField] ActiveUpgradeOption[] activeOptions;

        [Header("Controller Pick")]
        [SerializeField] string dpadHorizontalAxis = "DPadHorizontal";
        [SerializeField] string dpadVerticalAxis = "DPadVertical";

        ChoiceOption[] currentChoices = new ChoiceOption[3];

        float lastDpadX;
        float lastDpadY;

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

            if (global::PlayerInputRouter.GetUpgradeLeftDown() || dpadLeft || Input.GetKeyDown(KeyCode.JoystickButton13))
                ChooseSlot(0);

            if (global::PlayerInputRouter.GetUpgradeTopDown() || dpadUp || Input.GetKeyDown(KeyCode.JoystickButton15))
                ChooseSlot(1);

            if (global::PlayerInputRouter.GetUpgradeRightDown() || dpadRight || Input.GetKeyDown(KeyCode.JoystickButton14))
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

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] == null)
                    continue;

                int slot = i;

                choiceButtons[i].gameObject.SetActive(true);
                choiceButtons[i].interactable = currentChoices[i].CanChoose;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => ChooseSlot(slot));

                if (i < choiceLabels.Length && choiceLabels[i] != null)
                    choiceLabels[i].text = currentChoices[i].Label;
            }

            if (panelRoot != null)
                panelRoot.SetActive(true);

            lastDpadX = 0f;
            lastDpadY = 0f;

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
            return new ChoiceOption(label, false, null, null, null);
        }

        void ChooseSlot(int slot)
        {
            if (slot < 0 || slot >= currentChoices.Length)
                return;

            if (!currentChoices[slot].CanChoose)
                return;

            currentChoices[slot].Apply?.Invoke();

            Time.timeScale = 1f;

            if (panelRoot != null)
                panelRoot.SetActive(false);
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