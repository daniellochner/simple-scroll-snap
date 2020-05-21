// Simple Scroll-Snap
// Version: 1.0.0
// Author: Daniel Lochner

using UnityEngine;
using UnityEngine.UI;

namespace DanielLochner.Assets.SimpleScrollSnap
{
    public class FileSelected : MonoBehaviour
    {
        #region Methods
        private void Update()
        {
            if (transform.parent.GetComponent<Toggle>().isOn)
            {
                GetComponent<Text>().color = Color.white;
            }
            else
            {
                GetComponent<Text>().color = Color.black;
            }
        }
        #endregion
    }
}