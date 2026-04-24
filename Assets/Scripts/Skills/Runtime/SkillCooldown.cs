namespace Pithox.Skills
{
    public class SkillCooldown
    {
        public float Duration { get; private set; }
        public float Remaining { get; private set; }

        public bool IsReady => Remaining <= 0f;

        public SkillCooldown(float duration)
        {
            Duration = duration;
            Remaining = 0f;
        }

        public void StartCooldown()
        {
            Remaining = Duration;
        }

        public void Tick(float deltaTime)
        {
            if (Remaining > 0f)
            {
                Remaining -= deltaTime;
                if (Remaining < 0f)
                {
                    Remaining = 0f;
                }
            }
        }
    }
}