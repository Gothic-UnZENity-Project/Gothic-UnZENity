using HurricaneVR.Framework.Core.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GUZ
{
    public class MoveTransform : MonoBehaviour
    {
        // Update is called once per frame
        public Transform rootObject;  // The parent object holding everything
        public Transform cameraTransform;  // The camera object (child of rootObject)

        // Start is called before the first frame update
        void Start()
        {
        
        }

        void Update()
        {
            if (rootObject != null && cameraTransform != null)
            {
                // Step 1: Store the original children of rootObject
                Transform[] children = new Transform[rootObject.childCount];
                for (int i = 0; i < rootObject.childCount; i++)
                {
                    children[i] = rootObject.GetChild(i);
                }

                // Step 2: Detach the children from rootObject
                foreach (Transform child in children)
                {
                    child.SetParent(null);
                }

                // Step 3: Move the rootObject to the camera's world position
                rootObject.position = cameraTransform.position;

                // Step 4: Reattach the children back to rootObject
                foreach (Transform child in children)
                {
                    child.SetParent(rootObject);
                }
            }
            rootObject = this.transform;
            cameraTransform = rootObject.FindChildRecursive("Camera");
        }
    }
}
