// Simple Scroll-Snap
// Version: 1.0.0
// Author: Daniel Lochner

using UnityEngine;

namespace DanielLochner.Assets.SimpleScrollSnap
{
    public class SlotMachine : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        protected SimpleScrollSnap[] slots;
        #endregion

        #region Methods
        public void OnSpin()
        {
            foreach (SimpleScrollSnap slot in slots)
            {
                slot.AddVelocity(Random.Range(2500, 10000) * Vector2.up);
            }
        }
        #endregion
    }
}