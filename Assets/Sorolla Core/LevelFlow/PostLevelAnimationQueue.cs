using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Sorolla.LevelFlow
{
    /// <summary>
    /// Drop this component on a persistent GameObject in the bootstrap scene
    /// (alongside GameManager). It self-registers as
    /// <see cref="IPostLevelAnimationQueue"/> in <see cref="Awake"/>; no other
    /// wiring is required.
    /// </summary>
    public class PostLevelAnimationQueue : MonoBehaviour, IPostLevelAnimationQueue
    {
        private readonly List<IPostLevelAnimation> _registered = new();

        private void Awake()
        {
            ServiceLocator.Instance.Register<IPostLevelAnimationQueue>(this);
        }

        public void Register(IPostLevelAnimation animation)
        {
            if (animation == null) return;
            if (_registered.Contains(animation)) return;
            _registered.Add(animation);
        }

        public void Unregister(IPostLevelAnimation animation)
        {
            if (animation == null) return;
            _registered.Remove(animation);
        }

        public async Task PlayAllAsync(float interAnimationDelaySeconds = 0f)
        {
            // Snapshot + filter so providers can safely register/unregister
            // mid-iteration (e.g. a panel deactivates while another animates).
            var ordered = new List<IPostLevelAnimation>(_registered.Count);
            for (int i = 0; i < _registered.Count; i++)
            {
                var a = _registered[i];
                if (a != null && a.ShouldPlay) ordered.Add(a);
            }
            ordered.Sort((x, y) => x.Priority.CompareTo(y.Priority));

            for (int i = 0; i < ordered.Count; i++)
            {
                if (i > 0 && interAnimationDelaySeconds > 0f)
                    await Task.Delay(Mathf.RoundToInt(interAnimationDelaySeconds * 1000f));

                try
                {
                    await ordered[i].PlayAsync();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PostLevelAnimationQueue] {ordered[i].GetType().Name}.PlayAsync threw: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
    }
}
