using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace LambdaCompany
{
	public class ExplosiveBarrel : GrabbableObject, IHittable
	{
		public bool exploded;
		public bool sendingExplodeRPC;

		public AudioSource barrelAudio;
		public AudioSource barrelFarAudio;

		public AudioClip barrelBlast;
		public AudioClip barrelHit;

		bool IHittable.Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX) // playerWhoHit = null, playHitSFX = false
		{
			if (playHitSFX)
			{
				barrelAudio.PlayOneShot(barrelHit, 1f);
				WalkieTalkie.TransmitOneShotAudio(barrelAudio, barrelHit);
			}
			//check chance
			SetOffLocally();

			return true;
		}

		public override void OnHitGround()
		{
			var dista = startFallingPosition.y - targetFloorPosition.y;
			Plugin.Log($"Fall time = {fallTime}", BepInEx.Logging.LogLevel.Warning);
			Plugin.Log($"Fall distance = {dista}", BepInEx.Logging.LogLevel.Warning);
			base.OnHitGround();
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
			Landmine.SpawnExplosion(base.transform.position + Vector3.up, spawnExplosionEffect: true, 5f, 8f);
			// TODO: give ear ringing effect
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
