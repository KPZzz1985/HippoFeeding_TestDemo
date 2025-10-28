using UnityEngine;
using System.Collections.Generic;

namespace HippoFeeding.Gameplay.Items
{
	/// <summary>
	/// Projectile watermelon: breaks on collision unless colliding with a special hippo mouth catcher.
	/// Expects: whole visual on this object; sliced prefab assigned; optional tag for safe mouth collider.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	public sealed class ThrownWatermelon : MonoBehaviour
	{
		[SerializeField] private GameObject slicedPrefab; // prefab containing multiple rigidbodies/colliders pieces
		[SerializeField] private string safeMouthTag = "HippoMouth"; // collision with this tag will NOT break
		[SerializeField] private float minBreakSpeed = 3f;
		[SerializeField] private float explosionImpulse = 2.2f; // velocity-change per piece
		[SerializeField] private float explosionUpBias = 0.3f;  // upward bias for nicer spread
		[SerializeField] private float explosionRandomness = 0.6f; // random variation 0..1

		[Header("Audio")]
		[SerializeField] private AudioClip sfxBreak;
		[SerializeField] private float sfxBreakVolume = 1f;
		[SerializeField] private AudioClip[] sfxPieceImpact;
		[SerializeField] private float sfxPieceImpactVolume = 0.6f;
		[SerializeField] private float sfxPieceImpactMinSpeed = 0.7f;
		[SerializeField] private float sfxPieceImpactPitchJitter = 0.05f;
		[SerializeField] private float sfxPieceImpactCooldown = 0.06f;

		private bool broken;

		private void OnCollisionEnter(Collision collision)
		{
			if (broken) return;
			if (collision.collider.CompareTag(safeMouthTag))
				return;
			if (collision.relativeVelocity.magnitude < minBreakSpeed)
				return;

			Break();
		}

		public void Break()
		{
			if (broken) return;
			broken = true;
			if (slicedPrefab != null)
			{
				var piecesRoot = Instantiate(slicedPrefab, transform.position, transform.rotation);
				var bodies = piecesRoot.GetComponentsInChildren<Rigidbody>();
				for (int i = 0; i < bodies.Length; i++)
				{
					Rigidbody rb = bodies[i];
					Vector3 dir = UnityEngine.Random.onUnitSphere;
					dir.y = Mathf.Abs(dir.y) + explosionUpBias;
					dir.Normalize();
					float scale = explosionImpulse * (1f + UnityEngine.Random.Range(-explosionRandomness, explosionRandomness));
					rb.AddForce(dir * scale, ForceMode.VelocityChange);
					Vector3 randomTorque = UnityEngine.Random.insideUnitSphere * scale;
					rb.AddTorque(randomTorque, ForceMode.VelocityChange);
				}
				SetupPieceImpactAudio(piecesRoot);
				PlayClip3D(sfxBreak, transform.position, sfxBreakVolume, 1f);
			}
			Destroy(gameObject);
		}

		public GameObject SpawnSlicedAndDestroySelf()
		{
			if (broken) return null;
			broken = true;
			GameObject piecesRoot = null;
			if (slicedPrefab != null)
			{
				piecesRoot = Instantiate(slicedPrefab, transform.position, transform.rotation);
				SetupPieceImpactAudio(piecesRoot);
				PlayClip3D(sfxBreak, transform.position, sfxBreakVolume, 1f);
			}
			Destroy(gameObject);
			return piecesRoot;
		}

		private void SetupPieceImpactAudio(GameObject piecesRoot)
		{
			if (piecesRoot == null || sfxPieceImpact == null || sfxPieceImpact.Length == 0) return;
			var bodies = piecesRoot.GetComponentsInChildren<Rigidbody>(true);
			for (int i = 0; i < bodies.Length; i++)
			{
				var rb = bodies[i];
				var audio = rb.gameObject.GetComponent<SlicedPieceImpactAudio>();
				if (audio == null) audio = rb.gameObject.AddComponent<SlicedPieceImpactAudio>();
				audio.Configure(sfxPieceImpact, sfxPieceImpactMinSpeed, sfxPieceImpactVolume, sfxPieceImpactPitchJitter, sfxPieceImpactCooldown);
			}

			var releaser = piecesRoot.GetComponent<SlicedProgressiveRelease>();
			if (releaser != null && releaser.transform != null)
			{
				if (releaser.transform.parent != null)
				{
					// set world parent to scene root by default if not assigned
				}
			}
		}

		private static void PlayClip3D(AudioClip clip, Vector3 position, float vol, float pitch)
		{
			if (clip == null) return;
			GameObject go = new GameObject("WatermelonBreakAudio");
			go.transform.position = position;
			var src = go.AddComponent<AudioSource>();
			src.clip = clip;
			src.spatialBlend = 1f;
			src.rolloffMode = AudioRolloffMode.Logarithmic;
			src.minDistance = 1f;
			src.maxDistance = 20f;
			src.volume = vol;
			src.pitch = Mathf.Max(0.1f, pitch);
			src.Play();
			Destroy(go, clip.length / Mathf.Max(0.01f, src.pitch));
		}
	}
}


