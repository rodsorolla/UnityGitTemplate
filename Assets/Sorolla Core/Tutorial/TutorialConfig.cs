using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sorolla.Tutorial
{
    [Serializable]
    public class LevelTutorialGroup
    {
        public int LevelIndex;
        public List<TutorialStepBase> Steps = new();
    }

    [CreateAssetMenu(fileName = "TutorialConfig", menuName = "Sorolla/Tutorial/Tutorial Config")]
    public class TutorialConfig : ScriptableObject
    {
        public List<LevelTutorialGroup> LevelGroups = new();

        /// <summary>
        /// Converts the level groups to a dictionary for runtime use.
        /// </summary>
        public Dictionary<int, List<TutorialStepBase>> ToDictionary()
        {
            var dict = new Dictionary<int, List<TutorialStepBase>>();
            foreach (var group in LevelGroups)
            {
                if (group.Steps != null && group.Steps.Count > 0)
                    dict[group.LevelIndex] = group.Steps;
            }
            return dict;
        }
    }
}
