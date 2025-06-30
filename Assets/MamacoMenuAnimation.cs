using UnityEngine;

public class IdleMonkeyTimer : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] string triggerName;
    [SerializeField, Range(0f,1f)] private float playChance = 0.2f;
    [SerializeField] float minDelay = 5f;
    [SerializeField] float maxDelay = 20f;

    private float nextTime;

    private void Start()
    {
        ScheduleNext();
    }

    private void Update()
    {
        nextTime -= Time.deltaTime;
        if (nextTime <= 0f)
        {
            if (Random.value < playChance)
                animator.SetTrigger(triggerName);

            ScheduleNext();
        }
    }

    private void ScheduleNext()
    {
        nextTime = Random.Range(minDelay, maxDelay);
    }
}