using UnityEngine;
using UnityEngine.UI;

namespace HippoFeeding.Gameplay.UI
{
	/// <summary>
	/// Lightweight fill bar controller. Assign an Image with Fill Method set to Horizontal/Vertical.
	/// Call SetTargetFill01 to animate, or SetImmediate to snap.
	/// </summary>
	public sealed class SatiationBarUI : MonoBehaviour
	{
		[SerializeField] private Image fillImage;
		[SerializeField] private float current01;
		private float target01;
		private float lerpSpeed;

		private void Awake()
		{
			if (fillImage == null) fillImage = GetComponentInChildren<Image>(true);
			Apply();
		}

		private void Update()
		{
			if (!Mathf.Approximately(current01, target01))
			{
				current01 = Mathf.MoveTowards(current01, target01, Mathf.Max(0f, lerpSpeed) * Time.deltaTime);
				Apply();
			}
		}

		public void SetTargetFill01(float value01, float speed)
		{
			target01 = Mathf.Clamp01(value01);
			lerpSpeed = Mathf.Max(0f, speed);
		}

		public void SetImmediate(float value01)
		{
			current01 = target01 = Mathf.Clamp01(value01);
			Apply();
		}

		private void Apply()
		{
			if (fillImage != null)
				fillImage.fillAmount = current01;
		}
	}
}


