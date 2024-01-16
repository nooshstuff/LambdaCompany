using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace LambdaCompany
{
	public class ExplosiveBarrel : GrabbableObject, IHittable
	{
		public bool willExplode;
		public bool exploded;
		public bool sendingExplodeRPC;

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

		//[HideInInspector]
		//private static float explodeChance = 1f / 3;

		/*
		public override void Start()
		{
			base.Start();
			explodeChance = P.explodeChance.Value;
		}
		*/

		bool IHittable.Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX) // playerWhoHit = null, playHitSFX = false
		{
			if (playHitSFX)
			{
				barrelAudio.PlayOneShot(intenseSFX, 1f);
				WalkieTalkie.TransmitOneShotAudio(barrelAudio, intenseSFX);
			}
			SetOffLocally();
			return true;
		}
		
		public override void OnHitGround()
		{
			base.OnHitGround();
			if (exploded || sendingExplodeRPC) return;
			P.Log($"Fall distance = {distanceFallen}", BepInEx.Logging.LogLevel.Warning);
			
			AudioClip soundToPlay;
			switch (distanceFallen)
			{
				case > 2f:
					willExplode = true;
					soundToPlay = intenseSFX;
					P.Log("Setting off guarunteed barrel explosion!");
					break;
				case > 1f:
					//willExplode = rng.NextDouble() <= explodeChance;
					willExplode = true;
					soundToPlay = intenseSFX;
					P.Log((willExplode ? "Setting off explosion due to failed roll!" : "Barrel explosion avoided!"));
					break;
				default:
					willExplode = false;
					soundToPlay = dropSFX;
					P.Log("Barrel not exploding, drop too short.");
					break;
			}
			if (soundToPlay != null)
			{
				base.gameObject.GetComponent<AudioSource>().PlayOneShot(soundToPlay);
				if (base.IsOwner)
				{
					RoundManager.Instance.PlayAudibleNoise(base.transform.position, 8f + distanceFallen, 0.5f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed, 941);
				}
			}
			if (willExplode) SetOffLocally();
		}

		public void SetOffLocally()
		{
			if (!exploded)
			{
				Detonate();
				sendingExplodeRPC = true;
				SetOffServerRpc();
			}
		}

		[ServerRpc(RequireOwnership = false)]
		public void SetOffServerRpc()
		{
			SetOffClientRpc();
		}

		[ClientRpc]
		public void SetOffClientRpc()
		{
			if (sendingExplodeRPC) { sendingExplodeRPC = false; }
			else { Detonate(); }
		}

		public void Detonate()
		{
			exploded = true;
			barrelAudio.PlayOneShot(barrelBlast, 1f);
			WalkieTalkie.TransmitOneShotAudio(barrelAudio, barrelBlast);
			try
			{
				Landmine.SpawnExplosion(base.transform.position + Vector3.up, spawnExplosionEffect: true, 5f, 8f);
				StunGrenadeItem.StunExplosion(base.transform.position + Vector3.up, true, 0.1f, 3.0f, 0.5f);
			}
			catch (Exception e) { P.Log($"EXCEPTION!! {e}"); }
			RemoveScrapServerRpc();
		}

		[ServerRpc(RequireOwnership = false)]
		private void RemoveScrapServerRpc()
		{
			grabbable = false;
			grabbableToEnemies = false;
			deactivated = true;
			GameObject.Destroy(this.gameObject);
		}
	}
}
