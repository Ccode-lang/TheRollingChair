using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace TheRollingChair
{
    internal class Chair : EnemyAI
    {

        PlayerControllerB selectedPlayer = null;

        public GameObject RopePhysics;
        public GameObject Noose;

        public Transform ViewPoint;

        public AudioSource RollingAudio;

        public override void DoAIInterval()
        {
            base.DoAIInterval();
            if (StartOfRound.Instance.allPlayersDead)
            {
                return;
            }
            if (isEnemyDead)
            {
                return;
            }
            if (selectedPlayer == null)
            {
                return;
            }

            if (!selectedPlayer.isInsideFactory)
            {
                agent.speed = 0;
                agent.angularSpeed = 0;
            }

            if (!selectedPlayer.HasLineOfSightToPosition(ViewPoint.position))
            {
                // Do not touch the magic 1.8f
                agent.acceleration = Plugin.RollSpeed.Value * 1.8f;
                agent.speed = Plugin.RollSpeed.Value;
                agent.angularSpeed = 120;
                SetDestinationToPosition(RoundManager.Instance.GetNavMeshPosition(selectedPlayer.transform.position, RoundManager.Instance.navHit, 2.75f));

                if (!RollingAudio.isPlaying)
                {
                    PlayRollingAudioClientRpc();
                }
            }
            else
            {
                agent.acceleration = 0.2f;
                agent.speed = 0;
                agent.angularSpeed = 0;
                SetDestinationToPosition(transform.position);

                if (RollingAudio.isPlaying)
                {
                    StopPlayingRollingAudioClientRpc();
                }
            }
        }

        private PlayerControllerB nearestPlayer()
        {
            float distance = float.MaxValue;
            PlayerControllerB nearPlayer = null;
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                float distanceTemp = Vector3.Distance(player.transform.position, transform.position);
                if (distanceTemp < distance)
                {
                    distance = distanceTemp;
                    nearPlayer = player;
                }
            }

            return nearPlayer;
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);

            PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();

            if (!selectedPlayer.HasLineOfSightToPosition(ViewPoint.position) && GameNetworkManager.Instance.localPlayerController.playerClientId == player.playerClientId) {
                player.DamagePlayer(100, false, true, CauseOfDeath.Strangulation, 0, false, default);
                HangPlayerServerRpc(new NetworkObjectReference(other.gameObject.GetComponent<NetworkObject>()));
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void HangPlayerServerRpc(NetworkObjectReference player) {
            HangPlayerClientRpc(player);
        }

        [ClientRpc]
        public void PlayRollingAudioClientRpc()
        {
            RollingAudio.Play();
        }

        [ClientRpc]
        public void StopPlayingRollingAudioClientRpc()
        {
            RollingAudio.Stop();
        }

        [ClientRpc]
        public void HangPlayerClientRpc(NetworkObjectReference player)
        {
            // Most of this is pretty much just sand spider code since it works well
            NetworkObject playerClient;
            if (player.TryGet(out playerClient, null))
            {
                PlayerControllerB playerController = playerClient.GetComponent<PlayerControllerB>();
                DeadBodyInfo body = playerController.deadBody;

                if (body == null)
                {
                    return;
                }

                RaycastHit hit;

                Vector3 position = transform.position + Vector3.up * 6f;
                if (Physics.Raycast(transform.position, Vector3.up, out hit, 25f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                {
                    position = hit.point;
                }

                SetLineRendererPoints component = Instantiate<GameObject>(RopePhysics, position, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform).GetComponent<SetLineRendererPoints>();
                component.target.position = body.bodyParts[0].transform.position;

                Instantiate(Noose, body.bodyParts[0].transform, false);

                body.attachedLimb = body.bodyParts[0];
                body.attachedTo = component.target;
            }
        }

        public override void Update()
        {
            base.Update();
            selectedPlayer = nearestPlayer();
        }

    }
}
