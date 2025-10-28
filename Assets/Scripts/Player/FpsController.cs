using UnityEngine;

namespace HippoFeeding.Gameplay.Player
{
	/// <summary>
	/// Simple first-person controller based on CharacterController.
	/// Uses legacy Input axes by default (Horizontal, Vertical, Mouse X, Mouse Y, Jump).
	/// Keeps code explicit and easy to tweak for a prototype.
	/// </summary>
	[RequireComponent(typeof(CharacterController))]
	public sealed class FpsController : MonoBehaviour
	{
		[Header("Movement")]
		[SerializeField] private float walkSpeed = 3.5f;
		[SerializeField] private float runSpeed = 5.5f;
		[SerializeField] private float gravity = -24f;
		[SerializeField] private float jumpHeight = 1.1f;

		[Header("Look")]
		[SerializeField] private Camera playerCamera;
		[SerializeField] private float mouseSensitivity = 120f;
		[SerializeField] private float maxVerticalAngle = 85f;
		[SerializeField] private bool lockCursor = true;
		[SerializeField] private bool resetInputOnLock = true; // prevent initial mouse jump on startup

		[Header("Animator (optional)")]
		[SerializeField] private Animator animator;
		[SerializeField] private string animParamIsWalk = "isWalk";
		[SerializeField] private string animParamIsRun = "isRun";

		[Header("Audio")]
		[SerializeField] private AudioSource audioSource;
		[SerializeField] private AudioClip[] footstepClips;
		[SerializeField] private float footstepWalkVolume = 0.6f;
		[SerializeField] private float footstepRunVolume = 0.65f;
		[SerializeField] private float footstepIntervalWalk = 0.55f;
		[SerializeField] private float footstepIntervalRun = 0.38f;

		private CharacterController characterController;
		private Vector3 velocityWorld;
		private float yaw;
		private float pitch;
		private float footstepTimer;

		private void Awake()
		{
			characterController = GetComponent<CharacterController>();
			if (playerCamera == null)
			{
				playerCamera = GetComponentInChildren<Camera>();
			}
			if (animator == null)
			{
				animator = GetComponentInChildren<Animator>();
			}
			if (audioSource == null)
			{
				audioSource = GetComponent<AudioSource>();
			}
		}

		private void OnEnable()
		{
			if (lockCursor)
			{
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
				if (resetInputOnLock)
					Input.ResetInputAxes();
				skipFirstMouseFrame = true;
			}
		}

		private void OnDisable()
		{
			if (lockCursor)
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
		}

		private void Start()
		{
			// Initialize yaw/pitch from current transforms so we start by looking where the prefab faces
			// This prevents the first Update from snapping to (0,0,0) orientation
			Vector3 yawSource = transform.localEulerAngles;
			yaw = yawSource.y;
			if (playerCamera != null)
			{
				float x = playerCamera.transform.localEulerAngles.x;
				if (x > 180f) x -= 360f; // convert to signed range
				pitch = Mathf.Clamp(x, -maxVerticalAngle, maxVerticalAngle);
				playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
			}
			transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
		}

		private bool skipFirstMouseFrame;

		private void Update()
		{
			UpdateLook();
			UpdateMovement();
		}

		private void UpdateLook()
		{
			if (skipFirstMouseFrame)
			{
				// Consume first frame after cursor lock to avoid jump from residual delta
				skipFirstMouseFrame = false;
				return;
			}
			float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
			float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

			yaw += mouseX;
			pitch -= mouseY;
			pitch = Mathf.Clamp(pitch, -maxVerticalAngle, maxVerticalAngle);

			transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
			if (playerCamera != null)
			{
				playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
			}
		}

		private void UpdateMovement()
		{
			bool isGrounded = characterController.isGrounded;
			if (isGrounded && velocityWorld.y < 0f)
			{
				velocityWorld.y = -2f; // small downward force to keep grounded
			}

			float inputX = Input.GetAxis("Horizontal");
			float inputZ = Input.GetAxis("Vertical");
			bool wantsRunInput = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
			bool forwardPressed = inputZ > 0f; // W only
			bool wantsRun = wantsRunInput && forwardPressed;

			Vector3 moveLocal = new Vector3(inputX, 0f, inputZ);
			moveLocal = Vector3.ClampMagnitude(moveLocal, 1f);
			float speed = wantsRun ? runSpeed : walkSpeed;
			Vector3 moveWorld = transform.TransformDirection(moveLocal) * speed;

			characterController.Move(moveWorld * Time.deltaTime);

			UpdateAnimator(moveLocal, wantsRun);
			HandleFootsteps(isGrounded, moveLocal, wantsRun);

			if (isGrounded && Input.GetButtonDown("Jump"))
			{
				velocityWorld.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
			}

			velocityWorld.y += gravity * Time.deltaTime;
			characterController.Move(velocityWorld * Time.deltaTime);
		}

		private void UpdateAnimator(Vector3 moveLocal, bool isRunning)
		{
			if (animator == null)
				return;
			bool isMoving = moveLocal.sqrMagnitude > 0.0001f;
			bool walk = isMoving && !isRunning;
			bool run = isMoving && isRunning;
			if (!string.IsNullOrEmpty(animParamIsWalk)) animator.SetBool(animParamIsWalk, walk);
			if (!string.IsNullOrEmpty(animParamIsRun)) animator.SetBool(animParamIsRun, run);
		}

		private void HandleFootsteps(bool isGrounded, Vector3 moveLocal, bool isRunning)
		{
			if (audioSource == null || footstepClips == null || footstepClips.Length == 0)
				return;
			bool isMoving = moveLocal.sqrMagnitude > 0.0001f;
			if (!isGrounded || !isMoving)
			{
				footstepTimer = 0f;
				return;
			}
			float interval = isRunning ? footstepIntervalRun : footstepIntervalWalk;
			footstepTimer -= Time.deltaTime;
			if (footstepTimer <= 0f)
			{
				var clip = footstepClips[Random.Range(0, footstepClips.Length)];
				float originalPitch = audioSource.pitch;
				audioSource.pitch = Random.Range(0.95f, 1.05f);
				audioSource.PlayOneShot(clip, isRunning ? footstepRunVolume : footstepWalkVolume);
				audioSource.pitch = originalPitch;
				footstepTimer = interval;
			}
		}
	}
}


