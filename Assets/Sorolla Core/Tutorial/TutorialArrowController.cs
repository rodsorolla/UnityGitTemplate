using UnityEngine;

namespace Sorolla.Tutorial
{
    public class TutorialArrowController : MonoBehaviour
    {
        public void Init(TutorialStepBase step)
        {
            transform.position = step.ArrowWorldPos;
            GetComponent<PointAtTarget>().target = step.ArrowPointTo;
            GetComponent<FollowPlayerWithOffset>().enabled = step.ArrowFollowTarget;
        }
    }
}
