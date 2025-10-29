using UnityEngine;
using Cysharp.Threading.Tasks;
using HippoFeeding.Gameplay.UI;

namespace HippoFeeding.Gameplay.Hippo
{
	/// <summary>
	/// Tracks hippo satiation. Each eaten melon adds progress; when full, hippo sleeps.
	/// While sleeping: blocks mouth open logic and disables mouth trigger, sets animator IsSleeping,
	/// fades AimIK weight to 0, and drains the satiation bar back to 0 over sleep duration.
	/// After drain reaches 0, waits wakeUpDelay, clears IsSleeping, re-enables systems, and restores AimIK.
	/// </summary>
	public sealed class HippoSleepController : MonoBehaviour
	{
		[Header("Config")]
		[SerializeField] private int melonsToSleep = 4;
		[SerializeField] private float sleepDuration = 15f;
		[SerializeField] private float wakeUpDelay = 2.5f; // time to stand up before re-enabling mouth
		[SerializeField] private bool resetProgressOnSleep = false; // if true: bar drains only during sleep, else persists

		[Header("Refs")]
		[SerializeField] private Animator animator;
		[SerializeField] private string animParamIsSleeping = "IsSleeping";
		[SerializeField] private string animParamIsMouthOpen = "IsMouthOpen";
		[SerializeField] private HippoMouthOpener mouthOpener;
		[SerializeField] private MouthTrigger mouthTrigger;
		[SerializeField] private HippoAimIkController aimIkController;
		[SerializeField] private Collider mouthCollider; // if null, will try get from MouthTrigger

		[Header("UI")]
		[SerializeField] private SatiationBarUI satiationBar;
		[SerializeField] private float barFillLerp = 6f;

		private int eatenCount;
		private bool isSleeping;
		private float progress01; // 0..1
		private CancellationTokenSourceProxy sleepCts;

		private void Awake()
		{
			if (animator == null) animator = GetComponentInChildren<Animator>();
			if (mouthTrigger == null) mouthTrigger = GetComponentInChildren<MouthTrigger>();
			if (mouthOpener == null) mouthOpener = GetComponentInChildren<HippoMouthOpener>();
			if (aimIkController == null) aimIkController = GetComponentInChildren<HippoAimIkController>();
			if (mouthCollider == null && mouthTrigger != null) mouthCollider = mouthTrigger.GetComponent<Collider>();
			melonsToSleep = Mathf.Max(1, melonsToSleep);
			ApplyBarInstant();
		}

		private void OnDisable()
		{
			// cancel any ongoing sleep coroutine
			sleepCts.Cancel();
		}

		/// <summary>Call this when a melon is fully eaten.</summary>
		public void NotifyMelonEaten()
		{
			if (isSleeping) return; // ignore while sleeping
			eatenCount = Mathf.Clamp(eatenCount + 1, 0, melonsToSleep);
			progress01 = Mathf.Clamp01((float)eatenCount / melonsToSleep);
			UpdateBarSmooth();
			if (Mathf.Approximately(progress01, 1f))
			{
				BeginSleep().Forget();
			}
		}

		private async UniTaskVoid BeginSleep()
		{
			if (isSleeping) return;
			isSleeping = true;
			// animator layer/flag
			if (animator != null && !string.IsNullOrEmpty(animParamIsSleeping))
				animator.SetBool(animParamIsSleeping, true);
			// shut mouth systems
			if (mouthOpener != null) mouthOpener.enabled = false;
			if (mouthCollider != null) mouthCollider.enabled = false;
			// ensure mouth closed
			if (animator != null && !string.IsNullOrEmpty(animParamIsMouthOpen)) animator.SetBool(animParamIsMouthOpen, false);
			// IK fade out
			if (aimIkController != null) aimIkController.SetSleeping(true);

			// Keep UI full and then drain to zero during sleepDuration
			float t = 0f;
			float duration = Mathf.Max(0.01f, sleepDuration);
			float start = resetProgressOnSleep ? 1f : progress01;
			float end = 0f;
			while (t < duration)
			{
				t += Time.deltaTime;
				float k = Mathf.Clamp01(t / duration);
				progress01 = Mathf.Lerp(start, end, k);
				UpdateBarImmediate();
				await UniTask.Yield();
			}
			progress01 = 0f;
			UpdateBarImmediate();
			eatenCount = 0;

			// wake window (still sleeping flag up to let animation stand up)
			if (wakeUpDelay > 0f)
				await UniTask.Delay(System.TimeSpan.FromSeconds(wakeUpDelay));

			// clear sleeping
			if (animator != null && !string.IsNullOrEmpty(animParamIsSleeping))
				animator.SetBool(animParamIsSleeping, false);
			isSleeping = false;

			// re-enable systems
			if (mouthCollider != null) mouthCollider.enabled = true;
			if (mouthOpener != null) mouthOpener.enabled = true;
			// IK restore
			if (aimIkController != null) aimIkController.SetSleeping(false);
		}

		private void UpdateBarSmooth()
		{
			if (satiationBar == null) return;
			satiationBar.SetTargetFill01(progress01, barFillLerp);
		}

		private void UpdateBarImmediate()
		{
			if (satiationBar == null) return;
			satiationBar.SetImmediate(progress01);
		}

		private void ApplyBarInstant()
		{
			if (satiationBar != null) satiationBar.SetImmediate(progress01);
		}
	}

	/// <summary>
	/// Minimal helper that allows simple cancellation without bringing full CTS dependency.
	/// </summary>
	internal struct CancellationTokenSourceProxy
	{
		private bool cancelled;
		public void Cancel() { cancelled = true; }
		public bool IsCancelled => cancelled;
	}
}


