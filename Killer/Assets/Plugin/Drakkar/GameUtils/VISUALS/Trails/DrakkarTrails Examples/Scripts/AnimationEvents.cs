using UnityEngine;
using Drakkar.GameUtils;

namespace Drakkar.Examples
{
	public class AnimationEvents : MonoBehaviour
	{
		public DrakkarTrail Trail;

		[ContextMenu("Start Trail")]
		public void StartTrail()
		{
			Trail.Begin();
		}

		public void StopTrail()
		{
			Trail.End();
		}
	}
}