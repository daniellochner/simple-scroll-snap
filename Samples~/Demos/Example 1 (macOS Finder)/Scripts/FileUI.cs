// Simple Scroll-Snap - https://assetstore.unity.com/packages/tools/gui/simple-scroll-snap-140884
// Copyright (c) Daniel Lochner

using UnityEngine;
using UnityEngine.UI;

namespace DanielLochner.Assets.SimpleScrollSnap
{
    public class FileUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Toggle toggle;
        [Space]
        [SerializeField] private Text nameText;
        [SerializeField] private Text dateModifiedText;
        [SerializeField] private Text sizeText;
        [SerializeField] private Text kindText;
        #endregion

        #region Methods
        private void Awake()
        {
            toggle.onValueChanged.AddListener(delegate (bool isOn)
            {
                nameText.color = dateModifiedText.color = sizeText.color = kindText.color = (isOn ? Color.white : Color.black);
            });
        }
        #endregion
    }
}