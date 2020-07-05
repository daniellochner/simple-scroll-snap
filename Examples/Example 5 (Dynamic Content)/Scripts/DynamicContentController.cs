// Simple Scroll-Snap
// Version: 1.2.0
// Author: Daniel Lochner

using System;
using UnityEngine;
using UnityEngine.UI;

namespace DanielLochner.Assets.SimpleScrollSnap
{
    public class DynamicContentController : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        protected GameObject panel, toggle, addInput, removeInput;

        private float toggleWidth;
        private SimpleScrollSnap sss;
        #endregion

        #region Methods
        private void Awake()
        {
            sss = GetComponent<SimpleScrollSnap>();
            toggleWidth = toggle.GetComponent<RectTransform>().sizeDelta.x * (Screen.width / 2048f); ;
        }

        public void AddToFront()
        {
            Add(0);
        }
        public void AddToBack()
        {
            Add(sss.NumberOfPanels);
        }
        public void AddAtIndex()
        {
            int index = Convert.ToInt32(addInput.GetComponent<InputField>().text);
            Add(index);
        }
        private void Add(int index)
        {
            //Pagination
            Instantiate(toggle, sss.pagination.transform.position + new Vector3(toggleWidth * (sss.NumberOfPanels + 1), 0, 0), Quaternion.identity, sss.pagination.transform);
            sss.pagination.transform.position -= new Vector3(toggleWidth / 2f, 0, 0);

            //Panel
            panel.GetComponent<Image>().color = new Color(UnityEngine.Random.Range(0, 255) / 255f, UnityEngine.Random.Range(0, 255) / 255f, UnityEngine.Random.Range(0, 255) / 255f);
            sss.Add(panel, index);
        }

        public void RemoveFromFront()
        {
            Remove(0);
        }
        public void RemoveFromBack()
        {
            if (sss.NumberOfPanels > 0)
            {
                Remove(sss.NumberOfPanels - 1);
            }
            else
            {
                Remove(0);
            }
        }
        public void RemoveAtIndex()
        {
            int index = Convert.ToInt32(removeInput.GetComponent<InputField>().text);
            Remove(index);
        }
        private void Remove(int index)
        {
            if (sss.NumberOfPanels > 0)
            {
                //Pagination
                DestroyImmediate(sss.pagination.transform.GetChild(sss.NumberOfPanels - 1).gameObject);
                sss.pagination.transform.position += new Vector3(toggleWidth / 2f, 0, 0);

                //Panel
                sss.Remove(index);
            }
        }
        #endregion
    }
}