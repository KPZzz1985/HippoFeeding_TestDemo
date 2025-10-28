using UnityEngine;
using HippoFeeding.Gameplay.Interaction;

namespace HippoFeeding.Gameplay.Player
{
	/// <summary>
	/// Bridges player Animator and gameplay: handles IsFoodHandled, throw trigger, and spawning a projectile watermelon.
	/// Use animation events to call AE_HideInHandMelon and AE_SpawnThrownMelon at the exact frame.
	/// </summary>
	public sealed class PlayerHandsController : MonoBehaviour
	{
		[Header("Animator")]
		[SerializeField] private Animator animator;
		[SerializeField] private string paramIsFoodHandled = "IsFoodHandled";
		[SerializeField] private string paramIsThrow = "isThrow"; // trigger or bool; we call SetTrigger

		[Header("In-hand Visual")] 
		[SerializeField] private GameObject inHandMelon; // visual only under hands

		[Header("Throw Spawn")] 
		[SerializeField] private Transform throwSpawn;
		[SerializeField] private GameObject thrownWatermelonPrefab; // prefab with Rigidbody + collision/break logic
		[SerializeField] private float throwSpeed = 10f;
		[SerializeField] private float throwUpwardBias = 0.05f;
		[SerializeField] private Vector3 initialTorque = new Vector3(0f, 2.5f, 0f); // spin on start

		[Header("Throw Timing")] 
		[SerializeField] private bool useAnimationEvents = true; // if false, use the delays below
		[SerializeField] private float hideInHandDelay = 0.0f;   // seconds after trigger
		[SerializeField] private float spawnProjectileDelay = 0.05f; // seconds after trigger

		[Header("References")]
		[SerializeField] private Camera playerCamera;

		[Header("Audio")]
		[SerializeField] private AudioSource audioSource;
		[SerializeField] private AudioClip sfxPickup;
		[SerializeField] private float sfxPickupVolume = 1f;
		[SerializeField] private AudioClip sfxHideInHand;
		[SerializeField] private float sfxHideVolume = 1f;
		[SerializeField] private AudioClip sfxThrow;
		[SerializeField] private float sfxThrowVolume = 1f;

		public bool HasFood { get; private set; }
		public System.Action<bool> OnFoodHandledChanged;
		public System.Action<GameObject> OnFoodThrown;
		private bool throwInProgress;

		private void Awake()
		{
			if (animator == null)
				animator = GetComponentInChildren<Animator>();
			if (playerCamera == null)
				playerCamera = GetComponentInChildren<Camera>();
			if (audioSource == null)
				audioSource = GetComponent<AudioSource>();
		}

		private void Update()
		{
			if (Input.GetMouseButtonDown(0))
			{
				StartThrowIfPossible();
			}
		}

		public void SetHasFood(bool hasFood)
		{
			HasFood = hasFood;
			if (animator != null && !string.IsNullOrEmpty(paramIsFoodHandled))
				animator.SetBool(paramIsFoodHandled, HasFood);
			SetInHandActive(HasFood);
			OnFoodHandledChanged?.Invoke(HasFood);
			if (HasFood)
				PlayOneShot(sfxPickup, sfxPickupVolume);
		}

		public void StartThrowIfPossible()
		{
			if (!HasFood || animator == null || throwInProgress)
				return;
			if (!string.IsNullOrEmpty(paramIsThrow))
				animator.SetTrigger(paramIsThrow);
			throwInProgress = true;

			if (!useAnimationEvents)
			{
				// Use timed delays instead of animation events
				StartCoroutine(ThrowSequenceByTime());
			}
		}

		private System.Collections.IEnumerator ThrowSequenceByTime()
		{
			if (hideInHandDelay > 0f)
				yield return new WaitForSeconds(hideInHandDelay);
			AE_HideInHandMelon();
			if (spawnProjectileDelay > 0f)
				yield return new WaitForSeconds(spawnProjectileDelay);
			AE_SpawnThrownMelon();
		}

		private void SetInHandActive(bool active)
		{
			if (inHandMelon != null)
				inHandMelon.SetActive(active);
		}

		// Animation Event: hide melon at the frame hands release it
		public void AE_HideInHandMelon()
		{
			SetInHandActive(false);
			PlayOneShot(sfxHideInHand, sfxHideVolume);
		}

		// Animation Event: spawn projectile and give it velocity
		public void AE_SpawnThrownMelon()
		{
			if (thrownWatermelonPrefab == null)
				return;

			Transform spawnFrom = throwSpawn != null ? throwSpawn : (playerCamera != null ? playerCamera.transform : transform);
			Vector3 dir = playerCamera != null ? playerCamera.transform.forward : transform.forward;
			dir = (dir + Vector3.up * throwUpwardBias).normalized;

			GameObject go = Instantiate(thrownWatermelonPrefab, spawnFrom.position, Quaternion.LookRotation(dir));
			if (go.TryGetComponent<Rigidbody>(out var rb))
			{
				rb.linearVelocity = dir * throwSpeed;
				rb.AddTorque(initialTorque, ForceMode.VelocityChange);
			}
			OnFoodThrown?.Invoke(go);
			PlayOneShot(sfxThrow, sfxThrowVolume);

			// After throw, the player no longer holds food until they pick up again
			SetHasFood(false);
			throwInProgress = false;
		}

		private void PlayOneShot(AudioClip clip, float volume)
		{
			if (clip == null || audioSource == null) return;
			audioSource.PlayOneShot(clip, volume);
		}
	}
}


