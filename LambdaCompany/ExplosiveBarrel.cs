using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace LambdaCompany
{
	public class ExplosiveBarrel : GrabbableObject, IHittable
	{
		public bool exploded;

		public AudioSource barrelAudio;
		public AudioSource barrelFarAudio;

		public AudioClip barrelBlast;
		public AudioClip dropSFX;
		public AudioClip intenseSFX;

		[HideInInspector]
		public float distanceFallen { get 
			{
				if (startFallingPosition == Vector3.zero || targetFloorPosition == Vector3.zero) return 0f;
				else return startFallingPosition.y - targetFloorPosition.y; 
			}
		}

		[HideInInspector]
		internal static float explodeChance = 1f / 3;

		public override void Start()
		{
			base.Start();
			explodeChance = P.explodeChance.Value;
		}

		bool IHittable.Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX) // playerWhoHit = null, playHitSFX = false
		{
			if (playHitSFX)
			{
				barrelAudio.PlayOneShot(intenseSFX, 1f);
				WalkieTalkie.TransmitOneShotAudio(barrelAudio, intenseSFX);
			}
			P.Log("Hit barrel with shovel, setting off.");
			SetOffServerRpc(); //doing it this way has it called only once
			return true;
		}
		
		public override void OnHitGround()
		{
			base.OnHitGround();
			if (exploded) return;
			P.Log($"Fall distance = {distanceFallen}", BepInEx.Logging.LogLevel.Warning);
			if (!base.IsOwner) return;	// owner = client that was holding the object, apparently
			
			bool willExplode = false;
			bool safeDrop = false;
			switch (distanceFallen)
			{
				case > 2f:
					willExplode = true;
					P.Log("Setting off guarunteed barrel explosion!");
					break;
				case > 1f:
					willExplode = UnityEngine.Random.Range(0f, 1f) <= explodeChance; //does doing a random check on the server rpc work better
					P.Log((willExplode ? "Setting off explosion due to failed roll!" : "Barrel explosion avoided!"));
					break;
				default:
					safeDrop = true;
					P.Log("Barrel not exploding, drop too short.");
					break;
			}
			if (StartOfRound.Instance.inShipPhase) willExplode = false;
			if (willExplode) {
				if (!exploded) SetOffServerRpc();
			}
			else {
				DropSoundServerRPC(safeDrop); }
			}

		[ServerRpc(RequireOwnership = false)]
		public void DropSoundServerRPC(bool safeDrop) { DropSoundClientRPC(safeDrop); }

		[ClientRpc]
		public void DropSoundClientRPC(bool safeDrop)
		{
			barrelAudio.PlayOneShot((safeDrop ? dropSFX : intenseSFX));
			WalkieTalkie.TransmitOneShotAudio(barrelAudio, (safeDrop ? dropSFX : intenseSFX));
			if (base.IsOwner) RoundManager.Instance.PlayAudibleNoise(base.transform.position, 8f + distanceFallen, 0.5f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed, 941);
		}

		[ServerRpc(RequireOwnership = false)]
		public void SetOffServerRpc() { SetOffClientRpc(); }

		[ClientRpc]
		public void SetOffClientRpc() { Detonate(); }

		public void Detonate()
		{
			exploded = true;
			barrelAudio.PlayOneShot(barrelBlast, 1f);
			WalkieTalkie.TransmitOneShotAudio(barrelAudio, barrelBlast);
			Landmine.SpawnExplosion(base.transform.position + Vector3.up, spawnExplosionEffect: true, 5f, 8f);
			StunGrenadeItem.StunExplosion(base.transform.position + Vector3.up, true, 0.1f, 3.0f, 0.5f);
			if (base.IsOwner) {
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 11f, 1f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed, 941);
				RemoveScrapServerRpc();
			}
		}

		[ServerRpc(RequireOwnership = false)]
		private void RemoveScrapServerRpc()
		{
			grabbable = false;
			grabbableToEnemies = false;
			deactivated = true;
			//GameObject.Destroy(this.gameObject);
			P.Log("Despawning barrel object.");
			NetworkObject.Despawn(true); //apparently this is better?
		}
	}
}
