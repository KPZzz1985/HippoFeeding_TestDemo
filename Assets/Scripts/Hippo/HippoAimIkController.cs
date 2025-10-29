using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using RootMotion.FinalIK;

namespace HippoFeeding.Gameplay.Hippo
{
	/// <summary>
	/// Controls external AimIK-like solver via public fields: Weight and Axis.y.
	/// Lerp Weight based on distance to player; when isMouthOpen toggles, animate Axis.y 0->-1 or -1->0 over 1s.
	/// This script assumes you bind two delegates or references to the IK component.
	/// </summary>
	public sealed class HippoAimIkController : MonoBehaviour
	{
		[SerializeField] private Transform player;
		[SerializeField] private float radius = 6f;
		[SerializeField] private float weightLerpSpeed = 6f; // smooth

		[Header("IK Bindings")] 
		[SerializeField] private AimIK aimIK; // Final IK AimIK component
		[SerializeField] private float currentWeight;
		[SerializeField] private float currentAxisY;

		[Header("Axis Y Control")] 
		[SerializeField] private float axisYClosedValue = 0f;
		[SerializeField] private float axisYOpenValue = -5f; // required: -5 when mouth open
		[SerializeField] private float axisLerpDuration = 1f;

		[Header("Post-throw Mouth Close")] 
		[SerializeField] private float mouthCloseDelayAfterThrow = 1.5f;

		[Header("Return Smoothing")] 
		[SerializeField] private float returnFadeDuration = 1f; // weight fade when restoring target
		[SerializeField] private float returnProxyTravelDuration = 1f; // move a proxy from last food pos to original target
		private bool weightFading;

		[Header("Sleep Override")]
		[SerializeField] private bool sleeping; // when true, suppress distance logic
		[SerializeField] private float sleepFadeOut = 0.5f;
		[SerializeField] private float wakeFadeIn = 0.7f;

		[Header("Mouth Flag Source")] 
		[SerializeField] private Animator animator;
		[SerializeField] private string paramIsMouthOpen = "IsMouthOpen";

		private bool axisAnimInProgress;
		private bool lastMouth;

		private void Awake()
		{
			if (animator == null)
				animator = GetComponentInChildren<Animator>();
			if (aimIK == null)
				aimIK = GetComponentInChildren<AimIK>();
			lastMouth = GetIsMouthOpen();
		}

		public void SetTemporaryTarget(Transform tempTarget, float duration)
		{
			if (aimIK == null) return;
			if (tempTarget == null) return;
			AimTemporaryTarget(tempTarget, duration).Forget();
		}

		private async UniTaskVoid AimTemporaryTarget(Transform tempTarget, float duration)
		{
			Transform originalTarget = aimIK.solver.target;
			aimIK.solver.target = tempTarget;
			float startTime = Time.time;
			Vector3 lastPos = tempTarget != null ? tempTarget.position : originalTarget.position;
			while (Time.time - startTime < duration)
			{
				if (tempTarget == null) break;
				lastPos = tempTarget.position;
				await UniTask.Yield();
			}

			if (tempTarget == null && returnProxyTravelDuration > 0f)
			{
				// Create a transient proxy at last known food position and move it to original target
				GameObject proxy = new GameObject("AimReturnProxy");
				proxy.transform.position = lastPos;
				Transform proxyT = proxy.transform;
				if (aimIK != null) aimIK.solver.target = proxyT;
				float t = 0f;
				Vector3 from = proxyT.position;
				Vector3 to = originalTarget != null ? originalTarget.position : from;
				while (t < returnProxyTravelDuration)
				{
					t += Time.deltaTime;
					float k = Mathf.Clamp01(t / returnProxyTravelDuration);
					proxyT.position = Vector3.Lerp(from, to, k);
					await UniTask.Yield();
				}
				if (aimIK != null && aimIK.solver.target == proxyT)
					aimIK.solver.target = originalTarget;
				Destroy(proxy);
			}
			else
			{
				// Smooth by fading weight when duration ends but food still exists
				await FadeWeightAsync(0f, returnFadeDuration);
				if (aimIK != null && aimIK.solver.target == tempTarget)
					aimIK.solver.target = originalTarget;
				await FadeWeightAsync(1f, returnFadeDuration);
			}
		}

		private async UniTask FadeWeightAsync(float to, float duration)
		{
			weightFading = true;
			float from = currentWeight;
			float t = 0f;
			if (duration <= 0f)
			{
				currentWeight = to;
				ApplyWeight(currentWeight);
				weightFading = false;
				return;
			}
			while (t < duration)
			{
				t += Time.deltaTime;
				float k = Mathf.Clamp01(t / duration);
				currentWeight = Mathf.Lerp(from, to, k);
				ApplyWeight(currentWeight);
				await UniTask.Yield();
			}
			currentWeight = to;
			ApplyWeight(currentWeight);
			weightFading = false;
		}

		private void Update()
		{
			if (player == null) return;
			if (!sleeping)
			{
				float target = Vector3.Distance(transform.position, player.position) <= radius ? 1f : 0f;
				currentWeight = Mathf.MoveTowards(currentWeight, target, weightLerpSpeed * Time.deltaTime);
				ApplyWeight(currentWeight);
			}

			bool mouth = GetIsMouthOpen();
			if (mouth != lastMouth && !axisAnimInProgress)
			{
				lastMouth = mouth;
				float from = currentAxisY;
				float to = mouth ? axisYOpenValue : axisYClosedValue;
				AnimateAxis(from, to, axisLerpDuration).Forget();
			}
		}

		public void SetSleeping(bool value)
		{
			sleeping = value;
			if (value)
			{
				FadeWeightAsync(0f, sleepFadeOut).Forget();
			}
			else
			{
				FadeWeightAsync(1f, wakeFadeIn).Forget();
			}
		}

		private bool GetIsMouthOpen()
		{
			if (animator == null || string.IsNullOrEmpty(paramIsMouthOpen)) return false;
			return animator.GetBool(paramIsMouthOpen);
		}

		private async UniTaskVoid AnimateAxis(float from, float to, float duration)
		{
			axisAnimInProgress = true;
			float t = 0f;
			while (t < duration)
			{
				t += Time.deltaTime;
				float k = Mathf.Clamp01(t / duration);
				currentAxisY = Mathf.Lerp(from, to, k);
				ApplyAxis(currentAxisY);
				await UniTask.Yield();
			}
			currentAxisY = to;
			ApplyAxis(currentAxisY);
			axisAnimInProgress = false;
		}

		private void ApplyWeight(float w)
		{
			if (aimIK != null)
			{
				aimIK.solver.IKPositionWeight = w;
			}
		}

		private void ApplyAxis(float y)
		{
			if (aimIK != null)
			{
				Vector3 axis = aimIK.solver.axis;
				axis.y = y;
				aimIK.solver.axis = axis;
			}
		}
	}
}


