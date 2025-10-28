using UnityEngine;
using HippoFeeding.Gameplay.Player;
using Cysharp.Threading.Tasks;
using System;

namespace HippoFeeding.Gameplay.Hippo
{
	/// <summary>
	/// Simple bridge that tells HippoAI to open mouth if the player is close and holding food.
	/// Attach to the hippo and assign player + interactor.
	/// </summary>
	public sealed class HippoMouthOpener : MonoBehaviour
	{
		[SerializeField] private Transform player;
		[SerializeField] private float openRadius = 3f;
		[SerializeField] private Animator hippoAnimator;
		[SerializeField] private string animParamMouthOpen = "IsMouthOpen";
		[SerializeField] private float mouthCloseDelayAfterThrow = 1.5f;

		private void Awake()
		{
			if (hippoAnimator == null)
				hippoAnimator = GetComponentInChildren<Animator>();
		}

		private PlayerHandsController hands;

		private void OnEnable()
		{
			hands = player != null ? player.GetComponentInChildren<PlayerHandsController>() : null;
			if (hands != null)
			{
				hands.OnFoodHandledChanged += HandleFoodChanged;
				playerHasFood = hands.HasFood;
			}
		}

		private void OnDisable()
		{
			if (hands != null)
				hands.OnFoodHandledChanged -= HandleFoodChanged;
		}

		private bool playerHasFood;
		private float keepOpenUntil; // time until which mouth is forced open after throw
		private float overrideUntil; // temporary animator override window
		private bool overrideValue;
		private float eatLockUntil; // during this window mouth cannot be opened

		private void HandleFoodChanged(bool hasFood)
		{
			playerHasFood = hasFood;
			if (hasFood)
				keepOpenUntil = 0f; // reset forced-open window when food picked again
		}

		private void Update()
		{
			if (player == null)
				return;

			float distance = Vector3.Distance(transform.position, player.position);
			bool forcedOpen = Time.time < keepOpenUntil;
			bool hasOverride = Time.time < overrideUntil;
			bool lockedEating = Time.time < eatLockUntil;
			bool computed = hasOverride
				? overrideValue
				: (distance <= openRadius && (playerHasFood || forcedOpen));
			bool shouldOpen = lockedEating ? false : computed;
			if (hippoAnimator != null && !string.IsNullOrEmpty(animParamMouthOpen))
				hippoAnimator.SetBool(animParamMouthOpen, shouldOpen);
		}

		public void DelayCloseMouth()
		{
			// keep mouth open for the configured delay window after throw
			keepOpenUntil = Time.time + mouthCloseDelayAfterThrow;
		}

		public void ForceCloseNow()
		{
			keepOpenUntil = 0f;
			if (hippoAnimator != null && !string.IsNullOrEmpty(animParamMouthOpen))
				hippoAnimator.SetBool(animParamMouthOpen, false);
		}

		public void OverrideMouthOpenFor(float seconds, bool open)
		{
			overrideValue = open;
			overrideUntil = Time.time + Mathf.Max(0f, seconds);
		}

		public void BeginEatLock(float durationSeconds)
		{
			eatLockUntil = Time.time + Mathf.Max(0f, durationSeconds);
			// close immediately for safety
			if (hippoAnimator != null && !string.IsNullOrEmpty(animParamMouthOpen))
				hippoAnimator.SetBool(animParamMouthOpen, false);
		}
	}
}


