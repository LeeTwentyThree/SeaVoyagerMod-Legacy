using SMLHelper.V2.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ShipMod.Ship
{
    public class ShipExitDoor : HandTarget, IHandTarget
    {
        ShipBehaviour sub;
        Transform entrancePosition;
        
        void Start()
        {
            sub = GetComponentInParent<ShipBehaviour>();
            entrancePosition = transform.GetChild(0);
        }
        public void OnHandClick(GUIHand hand)
        {
            Player.main.SetCurrentSub(null);
            Player.main.SetPosition(entrancePosition.position);
            GetComponent<AudioSource>().Play();
        }

        public void OnHandHover(GUIHand hand)
        {
            HandReticle.main.SetIcon(HandReticle.IconType.Interact);
            HandReticle.main.SetInteractText("Exit");
        }
    }
}
