// Simple Scroll-Snap - https://assetstore.unity.com/packages/tools/gui/simple-scroll-snap-140884
// Copyright (c) Daniel Lochner

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