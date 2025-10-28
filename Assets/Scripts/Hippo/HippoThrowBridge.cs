using UnityEngine;
using HippoFeeding.Gameplay.Player;

namespace HippoFeeding.Gameplay.Hippo
{
	/// <summary>
	/// Bridges player throw events to hippo logic:
	/// - Keeps mouth open for a delay after throw
	/// - Temporarily aims IK at the thrown food object
	/// Attach to hippo and assign references.
	/// </summary>
	public sealed class HippoThrowBridge : MonoBehaviour
	{
		[SerializeField] private PlayerHandsController playerHands;
		[SerializeField] private HippoMouthOpener mouthOpener;
		[SerializeField] private HippoAimIkController aimController;
		[SerializeField] private float aimAtThrownDuration = 1.5f;

		private void Reset()
		{
			mouthOpener = GetComponent<HippoMouthOpener>();
			aimController = GetComponent<HippoAimIkController>();
		}

		private void OnEnable()
		{
			if (playerHands == null)
				playerHands = FindObjectOfType<PlayerHandsController>();
			if (playerHands != null)
				playerHands.OnFoodThrown += HandleFoodThrown;
		}

		private void OnDisable()
		{
			if (playerHands != null)
				playerHands.OnFoodThrown -= HandleFoodThrown;
		}

		private void HandleFoodThrown(GameObject thrown)
		{
			if (mouthOpener != null)
				mouthOpener.DelayCloseMouth();
			if (aimController != null && thrown != null)
				aimController.SetTemporaryTarget(thrown.transform, aimAtThrownDuration);
		}
	}
}


