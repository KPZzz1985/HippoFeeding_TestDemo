using UnityEngine;

namespace HippoFeeding.Gameplay.Hippo
{
	/// <summary>
	/// Plays ambient hippo loop depending on IsMouthOpen.
	/// When mouth closed: idle loop; when open: special loop.
	/// Smoothly crossfades between clips.
	/// </summary>
	public sealed class HippoAmbientAudio : MonoBehaviour
	{
		[SerializeField] private Animator animator;
		[SerializeField] private string paramIsMouthOpen = "IsMouthOpen";

		[Header("Audio")]
		[SerializeField] private AudioSource audioSource;
		[SerializeField] private AudioClip idleLoop;
		[SerializeField] private float idleVolume = 0.6f;
		[SerializeField] private AudioClip mouthOpenLoop;
		[SerializeField] private float mouthOpenVolume = 0.8f;
		[SerializeField] private float crossfadeDuration = 0.35f;
		[SerializeField] private bool randomStartTime = true;

		private bool lastOpen;
		private Coroutine fadeRoutine;

		private void Awake()
		{
			if (animator == null) animator = GetComponentInChildren<Animator>();
			if (audioSource == null) audioSource = GetComponent<AudioSource>();
		}

		private void OnEnable()
		{
			ApplyState(GetIsMouthOpen(), instant: true);
		}

		private void Update()
		{
			bool open = GetIsMouthOpen();
			if (open != lastOpen)
			{
				ApplyState(open, instant: false);
			}
		}

		private bool GetIsMouthOpen()
		{
			if (animator == null || string.IsNullOrEmpty(paramIsMouthOpen)) return false;
			return animator.GetBool(paramIsMouthOpen);
		}

		private void ApplyState(bool open, bool instant)
		{
			lastOpen = open;
			AudioClip nextClip = open ? mouthOpenLoop : idleLoop;
			float nextVol = open ? mouthOpenVolume : idleVolume;
			if (audioSource == null || nextClip == null)
				return;

			if (fadeRoutine != null) StopCoroutine(fadeRoutine);
			if (instant)
			{
				SwitchClipImmediate(nextClip, nextVol);
			}
			else
			{
				fadeRoutine = StartCoroutine(CrossfadeTo(nextClip, nextVol));
			}
		}

		private void SwitchClipImmediate(AudioClip clip, float vol)
		{
			audioSource.Stop();
			audioSource.clip = clip;
			audioSource.loop = true;
			audioSource.volume = vol;
			if (randomStartTime && clip.length > 0.01f)
			{
				audioSource.time = Random.Range(0f, Mathf.Max(0f, clip.length - 0.05f));
			}
			audioSource.Play();
		}

		private System.Collections.IEnumerator CrossfadeTo(AudioClip nextClip, float nextVol)
		{
			float startVol = audioSource.isPlaying ? audioSource.volume : 0f;
			float t = 0f;
			float half = Mathf.Max(0.01f, crossfadeDuration * 0.5f);
			// fade out
			while (t < half)
			{
				t += Time.deltaTime;
				float k = Mathf.Clamp01(t / half);
				audioSource.volume = Mathf.Lerp(startVol, 0f, k);
				yield return null;
			}
			SwitchClipImmediate(nextClip, 0f);
			// fade in
			t = 0f;
			while (t < half)
			{
				t += Time.deltaTime;
				float k = Mathf.Clamp01(t / half);
				audioSource.volume = Mathf.Lerp(0f, nextVol, k);
				yield return null;
			}
			audioSource.volume = nextVol;
			fadeRoutine = null;
		}
	}
}


