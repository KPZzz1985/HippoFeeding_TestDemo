using UnityEngine;

namespace HippoFeeding.Gameplay.Hippo
{
	/// <summary>
	/// Very small state machine for hippo behavior: idle/walk, follow player within radius,
	/// open mouth if player holds food and is near. Animator parameters are left as strings
	/// so you can hook your clips easily.
	/// </summary>
	[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
	public sealed class HippoAI : MonoBehaviour
	{
		[SerializeField] private Transform player;
		[SerializeField] private float followRadius = 5f;
		[SerializeField] private float stopDistance = 2.2f;
		[SerializeField] private string animParamIsWalking = "IsWalking";
		[SerializeField] private string animParamMouthOpen = "MouthOpen";
		[SerializeField] private Animator animator;

		private UnityEngine.AI.NavMeshAgent agent;
		private bool mouthShouldBeOpen;

		private void Awake()
		{
			agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
			if (animator == null)
				animator = GetComponentInChildren<Animator>();
		}

		public void SetMouthOpenDemand(bool open)
		{
			mouthShouldBeOpen = open;
			if (animator != null && !string.IsNullOrEmpty(animParamMouthOpen))
				animator.SetBool(animParamMouthOpen, mouthShouldBeOpen);
		}

		private void Update()
		{
			if (player == null)
				return;

			float dist = Vector3.Distance(transform.position, player.position);
			if (dist <= followRadius)
			{
				Vector3 target = player.position;
				if (dist > stopDistance)
				{
					agent.isStopped = false;
					agent.SetDestination(target);
				}
				else
				{
					agent.isStopped = true;
				}
			}
			else
			{
				agent.isStopped = true;
			}

			if (animator != null && !string.IsNullOrEmpty(animParamIsWalking))
				animator.SetBool(animParamIsWalking, agent.velocity.sqrMagnitude > 0.01f);
		}
	}
}


