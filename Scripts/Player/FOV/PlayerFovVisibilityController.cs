using System.Collections.Generic;
using UnityEngine;
using VInspector;

public class PlayerFovVisibilityController : MonoBehaviour
{
    [Header("Config")]
    [Min(0f), SerializeField] private float forgetDelay = 0.2f;

    private readonly Dictionary<FovVisibilityTarget, float> lastSeenTimes = new Dictionary<FovVisibilityTarget, float>(64);
    private readonly List<FovVisibilityTarget> removeBuffer = new List<FovVisibilityTarget>(64);

    public void ReportSeen(FovVisibilityTarget target, float now)
    {
        if (target == null)
        {
            return;
        }

        lastSeenTimes[target] = now;
        target.SetVisible(true);
    }

    public void Tick(float now)
    {
        if (lastSeenTimes.Count == 0)
        {
            return;
        }

        removeBuffer.Clear();

        foreach (KeyValuePair<FovVisibilityTarget, float> pair in lastSeenTimes)
        {
            FovVisibilityTarget target = pair.Key;
            if (target == null)
            {
                removeBuffer.Add(target);
                continue;
            }

            bool shouldRemainVisible = now - pair.Value <= forgetDelay;
            if (shouldRemainVisible)
            {
                continue;
            }

            target.SetVisible(false);
            removeBuffer.Add(target);
        }

        for (int i = 0; i < removeBuffer.Count; i++)
        {
            lastSeenTimes.Remove(removeBuffer[i]);
        }
    }

    public void ResetMemory()
    {
        if (lastSeenTimes.Count > 0)
        {
            foreach (KeyValuePair<FovVisibilityTarget, float> pair in lastSeenTimes)
            {
                FovVisibilityTarget target = pair.Key;
                if (target == null)
                {
                    continue;
                }

                target.SetVisible(false);
            }

            lastSeenTimes.Clear();
        }

        removeBuffer.Clear();
    }
}