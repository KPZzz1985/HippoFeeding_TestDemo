using UnityEngine;
using HippoFeeding.Gameplay.Interaction;
using HippoFeeding.Gameplay.Items;

namespace HippoFeeding.Gameplay.Hippo
{
	/// <summary>
	/// Place this on a trigger collider inside the hippo mouth. Detects food items entering.
	/// When a CarryableItem with tag "Food" enters, invokes simple events and rigidbody control.
	/// </summary>
public sealed class MouthTrigger : MonoBehaviour
	{
	[SerializeField] private string requiredTag = "Food";
	[SerializeField] private Transform chewHoldPoint;
	[SerializeField] private float chewHoldLerp = 12f;
	[SerializeField] private float chewSeconds = 1.2f; // time before breaking/eating
	[SerializeField] private Animator hippoAnimator;
	[SerializeField] private string animParamMouthOpen = "isMouthOpen";
	[SerializeField] private string animTriggerFoodCatch = "isFoodCatch";
	[SerializeField] private HippoMouthOpener mouthOpener; // optional: to immediately cancel open-delay
	[SerializeField] private HippoSleepController sleepController; // optional: to accumulate satiation
	[SerializeField] private float mouthCloseAfterCatchDelay = 0.5f;
	[SerializeField] private float eatLockDelay = 0.2f; // delay before starting lock after contact
	[SerializeField] private float eatLockDuration = 2.5f + 2.667f; // total lock duration
	[SerializeField] private float convertToSlicesDelay = 1f; // delay before swapping to sliced prefab

	[Header("Audio")]
	[SerializeField] private AudioSource chewAudioSource; // assign clip in inspector
	[SerializeField] private float chewVolume = 1f;
	[SerializeField] private bool chewLoop = true; // if false we will re-trigger clip manually
	[SerializeField] private float chewRepeatGap = 0f; // extra gap between repeats when loop=false
	[SerializeField] private bool stopChewAudioOnFinish = true;
	[SerializeField] private AudioClip[] chewClips; // optional list of clips to randomize
	[SerializeField] private bool chewRandomPreventImmediateRepeat = true;

	private CarryableItem itemInMouth;
private ThrownWatermelon thrownInMouth;
private Transform slicedRoot;
private float convertTimer;
private bool isChewing;
	private Rigidbody caughtRb;
	private float chewTimer;

		private void Reset()
		{
			GetComponent<Collider>().isTrigger = true;
		}

private void OnTriggerEnter(Collider other)
		{
			if (!other.CompareTag(requiredTag))
				return;
			if (isChewing)
				return; // ignore new hits while current chew is active

    // Prefer thrown projectile, else generic carryable
    thrownInMouth = other.GetComponentInParent<ThrownWatermelon>();
    if (thrownInMouth != null)
    {
        // Hold the whole fruit for a short time before converting to pieces
        caughtRb = thrownInMouth.GetComponent<Rigidbody>();
        if (caughtRb != null)
        {
            caughtRb.isKinematic = true;
            caughtRb.linearVelocity = Vector3.zero;
            caughtRb.angularVelocity = Vector3.zero;
        }
        convertTimer = convertToSlicesDelay;
        isChewing = true;
        chewTimer = chewSeconds;
        if (hippoAnimator != null)
        {
            // keep mouth open briefly to satisfy animator left-branch conditions
            if (!string.IsNullOrEmpty(animParamMouthOpen)) hippoAnimator.SetBool(animParamMouthOpen, true);
            if (!string.IsNullOrEmpty(animTriggerFoodCatch))
            {
                hippoAnimator.SetTrigger(animTriggerFoodCatch);
                StartCoroutine(ResetFoodCatchTriggerNextFrame());
            }
            StartCoroutine(CloseMouthDelayed());
        }
        if (mouthOpener != null)
        {
            // Start chew lock after a short delay so left-branch can engage first
            StartCoroutine(BeginEatLockDelayed(eatLockDelay, eatLockDuration));
        }
        return;
    }

    CarryableItem item = other.GetComponentInParent<CarryableItem>();
	if (item != null)
	{
		itemInMouth = item;
		itemInMouth.OnDropped();
		itemInMouth.Rigidbody.isKinematic = true;
		caughtRb = itemInMouth.Rigidbody;
        chewTimer = chewSeconds;
        isChewing = true;
        if (hippoAnimator != null)
		{
			if (!string.IsNullOrEmpty(animParamMouthOpen)) hippoAnimator.SetBool(animParamMouthOpen, true);
			if (!string.IsNullOrEmpty(animTriggerFoodCatch))
			{
				hippoAnimator.SetTrigger(animTriggerFoodCatch);
				StartCoroutine(ResetFoodCatchTriggerNextFrame());
			}
			StartCoroutine(CloseMouthDelayed());
		}
        if (mouthOpener != null)
        {
            StartCoroutine(BeginEatLockDelayed(eatLockDelay, eatLockDuration));
        }
	}
		}

private void Update()
		{
	if (chewHoldPoint == null)
		return;

	// Follow to chew point if holding something
    if (slicedRoot != null)
	{
        Transform t = slicedRoot;
		t.position = Vector3.Lerp(t.position, chewHoldPoint.position, chewHoldLerp * Time.deltaTime);
		t.rotation = Quaternion.Slerp(t.rotation, chewHoldPoint.rotation, chewHoldLerp * Time.deltaTime);
	}
    else if (thrownInMouth != null)
    {
        Transform t = thrownInMouth.transform;
        t.position = Vector3.Lerp(t.position, chewHoldPoint.position, chewHoldLerp * Time.deltaTime);
        t.rotation = Quaternion.Slerp(t.rotation, chewHoldPoint.rotation, chewHoldLerp * Time.deltaTime);

        // schedule conversion to slices
        convertTimer -= Time.deltaTime;
        if (convertTimer <= 0f)
        {
            GameObject sliced = thrownInMouth.SpawnSlicedAndDestroySelf();
            thrownInMouth = null;
            if (sliced != null)
            {
                slicedRoot = sliced.transform;
                var releaser = sliced.GetComponent<SlicedProgressiveRelease>();
                if (releaser == null) releaser = sliced.AddComponent<SlicedProgressiveRelease>();
                releaser.Begin(chewSeconds, AnimationCurve.Linear(0f,0f,1f,1f), SlicedProgressiveRelease.ReleaseOrder.Random, 0.15f);
            }
            // Start chewing audio exactly when conversion delay ends
            StartChewAudio();
        }
    }
	else if (itemInMouth != null)
	{
		Transform t = itemInMouth.transform;
		t.position = Vector3.Lerp(t.position, chewHoldPoint.position, chewHoldLerp * Time.deltaTime);
		t.rotation = Quaternion.Slerp(t.rotation, chewHoldPoint.rotation, chewHoldLerp * Time.deltaTime);
	}

    if (thrownInMouth != null || itemInMouth != null || slicedRoot != null)
 	{
 		chewTimer -= Time.deltaTime;
 		if (chewTimer <= 0f)
 		{
            if (thrownInMouth != null)
 			{
 				thrownInMouth.Break();
 				thrownInMouth = null;
 			}
 			else if (itemInMouth != null)
 			{
 				Destroy(itemInMouth.gameObject);
 				itemInMouth = null;
 			}
			if (hippoAnimator != null && !string.IsNullOrEmpty(animParamMouthOpen))
 				hippoAnimator.SetBool(animParamMouthOpen, false);
			// notify satiation system
			if (sleepController != null)
				sleepController.NotifyMelonEaten();
            // cleanup state for next melons
            slicedRoot = null;
            convertTimer = 0f;
            isChewing = false;
            StopChewAudio();
 		}
    }

    }

    private Coroutine chewAudioRoutine;
    private void StartChewAudio()
    {
        if (chewAudioSource == null || chewAudioSource.clip == null)
            return;
        chewAudioSource.volume = chewVolume;
        // If список клипов задан — всегда управляем повтором сами, чтобы рандомизировать каждое воспроизведение
        bool useManualLoop = (chewClips != null && chewClips.Length > 0) || !chewLoop;
        if (useManualLoop)
        {
            if (chewAudioRoutine != null) StopCoroutine(chewAudioRoutine);
            chewAudioRoutine = StartCoroutine(ChewAudioRepeat());
        }
        else
        {
            chewAudioSource.loop = true;
            if (!chewAudioSource.isPlaying)
                chewAudioSource.Play();
        }
    }

    private int lastChewIndex = -1;

    private System.Collections.IEnumerator ChewAudioRepeat()
    {
        while (isChewing)
        {
            chewAudioSource.volume = chewVolume;
            // choose random clip if provided
            if (chewClips != null && chewClips.Length > 0)
            {
                int index = Random.Range(0, chewClips.Length);
                if (chewRandomPreventImmediateRepeat && chewClips.Length > 1)
                {
                    int guard = 0;
                    while (index == lastChewIndex && guard++ < 4)
                        index = Random.Range(0, chewClips.Length);
                }
                lastChewIndex = index;
                chewAudioSource.clip = chewClips[index];
            }
            if (chewAudioSource.clip == null)
                yield break;
            chewAudioSource.loop = false;
            chewAudioSource.Play();
            yield return new WaitForSeconds(chewAudioSource.clip.length + Mathf.Max(0f, chewRepeatGap));
        }
    }

    private void StopChewAudio()
    {
        if (!stopChewAudioOnFinish) return;
        if (chewAudioRoutine != null)
        {
            StopCoroutine(chewAudioRoutine);
            chewAudioRoutine = null;
        }
        if (chewAudioSource != null)
        {
            chewAudioSource.loop = false;
            if (chewAudioSource.isPlaying) chewAudioSource.Stop();
        }
    }
private System.Collections.IEnumerator CloseMouthDelayed()
{
	yield return new WaitForSeconds(mouthCloseAfterCatchDelay);
	if (hippoAnimator != null && !string.IsNullOrEmpty(animParamMouthOpen))
		hippoAnimator.SetBool(animParamMouthOpen, false);
	if (mouthOpener != null)
		mouthOpener.ForceCloseNow();
}

private System.Collections.IEnumerator ResetFoodCatchTriggerNextFrame()
{
	yield return null; // one frame
	// Animator triggers нельзя снять напрямую SetTrigger(false), поэтому используем ResetTrigger
	if (hippoAnimator != null && !string.IsNullOrEmpty(animTriggerFoodCatch))
		hippoAnimator.ResetTrigger(animTriggerFoodCatch);
}

private System.Collections.IEnumerator BeginEatLockDelayed(float delay, float duration)
{
	yield return new WaitForSeconds(delay);
	if (mouthOpener != null)
		mouthOpener.BeginEatLock(duration);
}

}
}


