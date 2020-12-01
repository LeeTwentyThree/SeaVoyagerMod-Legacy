using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ShipMod.Ship
{
    public class HeldByCable : MonoBehaviour
    {
        public ExosuitDock dock;
        public bool Docked
        {
            get
            {
                return dock != null;
            }
        }
    }
}
