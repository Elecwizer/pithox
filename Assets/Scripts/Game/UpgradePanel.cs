using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Pithox.Player;

namespace Pithox.Game
{
    public class UpgradePanel : MonoBehaviour
    {
        [SerializeField] PlayerStats playerStats;
        [SerializeField] GameObject panelRoot;
        [SerializeField] Button[] choiceButtons = new Button[3];
        [SerializeField] TMP_Text[] choiceLabels = new TMP_Text[3];

        struct UpgradeOption
        {
            public string Label;
            public Action<PlayerStats> Apply;
            public Func<PlayerStats, bool> IsAvailable;

            public UpgradeOption(string label, Action<PlayerStats> apply, Func<PlayerStats, bool> isAvailable = null)
            {
                Label = label;
                Apply = apply;
                IsAvailable = isAvailable;
            }
        }

        UpgradeOption[] options;

        void Awake()
        {
            options = new[]
            {
                new UpgradeOption("+15% Move Speed", s => s.AddMoveSpeed(0.15f)),
                new UpgradeOption("+0.5 Pickup Range", s => s.AddPickupRange(0.5f)),
                new UpgradeOption("+1s Streak Timer", s => s.AddStreakWindow(1f)),
                new UpgradeOption("+20% Attack Damage", s => s.AddAttackDamage(0.20f)),
                new UpgradeOption(
                    "Damage Aura",
                    s => s.EnableDamageAura(),
                    s => !s.DamageAuraEnabled),
                new UpgradeOption(
                    "Orbit Balls",
                    s => { if (!s.OrbitUnlocked) s.UnlockOrbit(); else s.AddOrbitDamage(0.20f); }),
                new UpgradeOption(
                    "Beam",
                    s => { if (!s.BeamUnlocked) s.UnlockBeam(); else s.AddBeamDamage(0.20f); }),
            };

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

        void HandleLevelUp(int newLevel)
        {
            ShowChoices();
        }

        void ShowChoices()
        {
            List<int> available = new List<int>();
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i].IsAvailable == null || options[i].IsAvailable(playerStats))
                    available.Add(i);
            }

            int slots = Mathf.Min(choiceButtons.Length, available.Count);

            for (int slot = 0; slot < choiceButtons.Length; slot++)
            {
                if (slot >= slots)
                {
                    if (choiceButtons[slot] != null) choiceButtons[slot].gameObject.SetActive(false);
                    continue;
                }

                int pickIdx = UnityEngine.Random.Range(0, available.Count);
                int optionIdx = available[pickIdx];
                available.RemoveAt(pickIdx);

                UpgradeOption opt = options[optionIdx];
                if (choiceLabels[slot] != null) choiceLabels[slot].text = opt.Label;

                if (choiceButtons[slot] != null)
                {
                    choiceButtons[slot].gameObject.SetActive(true);
                    choiceButtons[slot].onClick.RemoveAllListeners();
                    choiceButtons[slot].onClick.AddListener(() => Choose(opt));
                }
            }

            if (panelRoot != null) panelRoot.SetActive(true);
            Time.timeScale = 0f;
        }

        void Choose(UpgradeOption opt)
        {
            opt.Apply?.Invoke(playerStats);
            Time.timeScale = 1f;
            if (panelRoot != null) panelRoot.SetActive(false);
        }
    }
}
