using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Generic;

namespace HippoFeeding.Gameplay.Items
{
	public sealed class SlicedProgressiveRelease : MonoBehaviour
	{
		public enum ReleaseOrder
		{
			Random,
			BigFirst,
			SmallFirst
		}

		[SerializeField] private float totalDuration = 1.5f;
		[SerializeField] private AnimationCurve releaseCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
		[SerializeField] private ReleaseOrder order = ReleaseOrder.Random;
		[SerializeField] private float timeJitter = 0.15f;
		[SerializeField] private Transform worldParent; // optional: reparent released pieces to this
		[SerializeField] private float ejectionImpulse = 0.4f;
		[SerializeField] private float ejectionUpBias = 0.2f;

		[Header("Despawn (optional)")]
		[SerializeField] private bool enableDespawn = true;
		[SerializeField] private float pieceLifetimeMin = 2f;
		[SerializeField] private float pieceLifetimeMax = 4f;
		[SerializeField] private float shrinkDuration = 1f;

		private List<Rigidbody> pieceBodies;
		private HashSet<Transform> despawnScheduled;

		public void Begin(float duration, AnimationCurve curve, ReleaseOrder releaseOrder, float jitter)
		{
			totalDuration = duration;
			releaseCurve = curve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);
			order = releaseOrder;
			timeJitter = Mathf.Max(0f, jitter);
			SetupAndSchedule();
		}

		private void SetupAndSchedule()
		{
			pieceBodies = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>());
			if (pieceBodies.Count == 0) return;
			despawnScheduled = new HashSet<Transform>();

			// Set all to kinematic initially
			foreach (var rb in pieceBodies)
			{
				rb.isKinematic = true;
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
				// schedule despawn immediately, regardless of how this prefab appeared in scene
				ScheduleDespawn(rb.transform);
			}

			// Compute size metric for ordering
			var sizes = new Dictionary<Rigidbody, float>(pieceBodies.Count);
			foreach (var rb in pieceBodies)
			{
				float size = 1f;
				var rend = rb.GetComponentInChildren<Renderer>();
				if (rend != null)
				{
					var b = rend.bounds;
					size = Mathf.Max(0.0001f, b.size.x * b.size.y * b.size.z);
				}
				sizes[rb] = size;
			}

			pieceBodies.Sort((a, b) => sizes[a].CompareTo(sizes[b]));
			if (order == ReleaseOrder.BigFirst)
			{
				pieceBodies.Reverse();
			}
			else if (order == ReleaseOrder.Random)
			{
				for (int i = pieceBodies.Count - 1; i > 0; i--)
				{
					int j = Random.Range(0, i + 1);
					(var a, var b) = (pieceBodies[i], pieceBodies[j]);
					pieceBodies[i] = b;
					pieceBodies[j] = a;
				}
			}

			for (int i = 0; i < pieceBodies.Count; i++)
			{
				float rank = pieceBodies.Count == 1 ? 0f : (float)i / (pieceBodies.Count - 1);
				float t = Mathf.Clamp(releaseCurve.Evaluate(rank), 0f, 1f) * totalDuration;
				t += Random.Range(-timeJitter, timeJitter);
				t = Mathf.Clamp(t, 0f, totalDuration);
				StartCoroutine(ReleaseAfter(pieceBodies[i], t));
			}
		}

		private IEnumerator ReleaseAfter(Rigidbody rb, float delay)
		{
			yield return new WaitForSeconds(delay);
			if (rb != null)
			{
				// detach from sliced root so piece is no longer moved by parent
				rb.transform.SetParent(worldParent, true);
				rb.isKinematic = false;
				if (ejectionImpulse > 0f)
				{
					Vector3 dir = Random.onUnitSphere;
					dir.y = Mathf.Abs(dir.y) + ejectionUpBias;
					dir.Normalize();
					rb.AddForce(dir * ejectionImpulse, ForceMode.VelocityChange);
				}
			}
		}

		private void ScheduleDespawn(Transform piece)
		{
			if (!enableDespawn || piece == null) return;
			if (despawnScheduled != null && !despawnScheduled.Add(piece)) return;
			StartCoroutine(DespawnAfter(piece));
		}

		private IEnumerator DespawnAfter(Transform piece)
		{
			float life = Random.Range(pieceLifetimeMin, pieceLifetimeMax);
			if (life > 0f) yield return new WaitForSeconds(life);
			if (piece == null) yield break;
			Vector3 start = piece.localScale;
			float t = 0f;
			float dur = Mathf.Max(0.01f, shrinkDuration);
			while (t < dur && piece != null)
			{
				t += Time.deltaTime;
				float k = Mathf.Clamp01(t / dur);
				piece.localScale = Vector3.Lerp(start, Vector3.zero, k);
				yield return null;
			}
			if (piece != null) piece.localScale = Vector3.zero;
			if (piece != null) Destroy(piece.gameObject);
		}
	}
}


