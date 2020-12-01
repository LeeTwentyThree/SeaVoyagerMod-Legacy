using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ShipMod.Ship
{
    public class ShipMove : MonoBehaviour
    {
        public ShipBehaviour ship;

        void FixedUpdate()
        {
            if(ship.currentState == ShipState.Idle)
            {
                //ship.rb.angularVelocity = Vector3.zero;
                return;
            }
            if(GameModeUtils.IsInvisible() || ship.powerRelay.ConsumeEnergy(Time.fixedDeltaTime * 2f * QPatch.config.PowerDepletionRate, out float amountConsumed))
            {
                switch (ship.currentState)
                {
                    default:
                        break;
                    case ShipState.Manual:
                        ship.rb.AddForce(-ship.transform.forward * ship.moveAmount * ship.rb.mass, ForceMode.Force);
                        ship.rb.angularVelocity = Vector3.zero;
                        break;
                    case ShipState.Rotating:
                        ship.rb.AddTorque(Vector3.up * ship.rotationAmount * ship.rb.mass, ForceMode.Force);
                        break;
                }
            }
            else
            {
                ship.currentState = ShipState.Idle;
                ship.hud.DisableDirectionButtons();
                ship.voiceNotificationManager.PlayVoiceNotification(ship.noPowerNotification);
            }
            ship.rb.angularVelocity = new Vector3(ship.rb.angularVelocity.x, Mathf.Clamp(ship.rb.angularVelocity.y, -1f, 1f), ship.rb.angularVelocity.z);
        }
        void OnEnable()
        {
            Player.main.playerDeathEvent.AddHandler(this, OnDeath);
            Player.main.playerRespawnEvent.AddHandler(this, OnRespawn);
        }
        void OnDisable()
        {
            Player.main.playerDeathEvent.RemoveHandler(this, OnDeath);
            Player.main.playerRespawnEvent.RemoveHandler(this, OnRespawn);
        }
        void OnDeath(Player player)
        {
            ship.currentState = ShipState.Idle;
            ship.rb.velocity = Vector3.zero;
            ship.rb.angularVelocity = Vector3.zero;
            ship.rb.isKinematic = true;
            ship.hud.DisableDirectionButtons();
        }
        void OnRespawn(Player player)
        {
            if(Player.main.GetCurrentSub() == ship) ship.enginePowerDownNotification.Play();
            ship.rb.isKinematic = false;
        }
    }
}
