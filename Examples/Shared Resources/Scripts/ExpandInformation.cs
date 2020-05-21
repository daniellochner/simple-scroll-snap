// Simple Scroll-Snap
// Version: 1.0.0
// Author: Daniel Lochner

using UnityEngine;

namespace DanielLochner.Assets.SimpleScrollSnap
{
    public class ExpandInformation : MonoBehaviour
    {
        #region Fields
        private bool expanded = false;
        #endregion

        #region Methods
        public void Expand()
        {
            expanded = !expanded;
            GetComponent<Animator>().SetBool("expanded", expanded);
        }
        #endregion
    }
}