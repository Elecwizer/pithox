using Pithox.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pithox.Game
{
    public class GameUiFlowController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] PlayerHealth playerHealth;

        [Header("Pause UI")]
        [SerializeField] GameObject pauseOverlay;
        [SerializeField] bool pauseEnabled = true;
        [SerializeField] KeyCode pauseKeyboardKey = KeyCode.Escape;
        [SerializeField] KeyCode pauseControllerKey = KeyCode.JoystickButton9;

        [Header("Game Over UI")]
        [SerializeField] GameObject gameOverOverlay;
        [SerializeField] KeyCode restartKeyboardKey = KeyCode.Return;
        [SerializeField] KeyCode restartKeyboardAltKey = KeyCode.R;
        [SerializeField] KeyCode restartControllerKey = KeyCode.JoystickButton1; // Cross / A

        bool paused;
        bool gameOverShown;

        void Awake()
        {
            if (playerHealth == null)
                playerHealth = FindAnyObjectByType<PlayerHealth>();

            SetOverlayVisible(pauseOverlay, false);
            SetOverlayVisible(gameOverOverlay, false);
            ApplyGameplayBlock(false);
            Time.timeScale = 1f;
        }

        void OnEnable()
        {
            if (playerHealth != null)
                playerHealth.OnDied += HandlePlayerDied;
        }

        void OnDisable()
        {
            if (playerHealth != null)
                playerHealth.OnDied -= HandlePlayerDied;
        }

        void Update()
        {
            if (!gameOverShown && pauseEnabled && IsPausePressed())
            {
                TogglePause();
                return;
            }

            if (gameOverShown && IsRestartPressed())
                RestartCurrentScene();
        }

        void HandlePlayerDied()
        {
            gameOverShown = true;
            paused = false;

            SetOverlayVisible(pauseOverlay, false);
            SetOverlayVisible(gameOverOverlay, true);

            Time.timeScale = 0f;
            ApplyGameplayBlock(true);
        }

        void TogglePause()
        {
            paused = !paused;

            SetOverlayVisible(pauseOverlay, paused);

            Time.timeScale = paused ? 0f : 1f;
            ApplyGameplayBlock(paused);
        }

        void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            ApplyGameplayBlock(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        bool IsPausePressed()
        {
            return Input.GetKeyDown(pauseKeyboardKey) || Input.GetKeyDown(pauseControllerKey);
        }

        bool IsRestartPressed()
        {
            return Input.GetKeyDown(restartKeyboardKey)
                || Input.GetKeyDown(restartKeyboardAltKey)
                || Input.GetKeyDown(restartControllerKey);
        }

        static void SetOverlayVisible(GameObject overlay, bool visible)
        {
            if (overlay != null)
                overlay.SetActive(visible);
        }

        static void ApplyGameplayBlock(bool blocked)
        {
            global::PlayerInputRouter.SetGameplayInputBlocked(blocked);
        }
    }
}
