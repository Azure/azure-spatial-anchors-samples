using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.Azure.SpatialAnchors.Unity.Samples
{
    public class XRUXPickerForLauncher : XRUXPicker
    {
        private static XRUXPickerForLauncher _Instance;
        public new static XRUXPickerForLauncher Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = FindObjectOfType<XRUXPickerForLauncher>();
                }

                return _Instance;
            }
        }
        
    }
}
