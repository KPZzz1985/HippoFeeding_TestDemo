using UnityEngine;

namespace HippoFeeding.Gameplay.Items
{
	/// <summary>
	/// Plays impact sounds for a sliced piece when it collides.
	/// Components are attached procedurally to each piece when the melon breaks.
	/// </summary>
	public sealed class SlicedPieceImpactAudio : MonoBehaviour
	{
		[SerializeField] private AudioClip[] impactClips;
		[SerializeField] private float minSpeed = 0.8f;
		[SerializeField] private float volume = 0.6f;
		[SerializeField] private float pitchJitter = 0.06f;
		[SerializeField] private float cooldown = 0.06f;

		private float lastPlayTime;

		public void Configure(AudioClip[] clips, float minVel, float vol, float jitter, float cd)
		{
			impactClips = clips;
			minSpeed = minVel;
			volume = vol;
			pitchJitter = jitter;
			cooldown = cd;
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (impactClips == null || impactClips.Length == 0)
				return;
			if (Time.time - lastPlayTime < cooldown)
				return;
			if (collision.relativeVelocity.magnitude < minSpeed)
				return;

			lastPlayTime = Time.time;
			var clip = impactClips[Random.Range(0, impactClips.Length)];
			Vector3 pos = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
			float vol = Random.Range(0f, volume);
			PlayClip3D(clip, pos, vol, 1f + Random.Range(-pitchJitter, pitchJitter));
		}

		private static void PlayClip3D(AudioClip clip, Vector3 position, float vol, float pitch)
		{
			if (clip == null) return;
			GameObject go = new GameObject("PieceImpactAudio");
			go.transform.position = position;
			var src = go.AddComponent<AudioSource>();
			src.clip = clip;
			src.spatialBlend = 1f;
			src.rolloffMode = AudioRolloffMode.Logarithmic;
			src.minDistance = 0.8f;
			src.maxDistance = 12f;
			src.volume = vol;
			src.pitch = Mathf.Max(0.1f, pitch);
			src.Play();
			Object.Destroy(go, clip.length / Mathf.Max(0.01f, src.pitch));
		}
	}
}


