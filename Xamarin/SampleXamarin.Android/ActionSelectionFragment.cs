// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Android.OS;
using Android.Support.V4.App;
using Android.Views;

namespace SampleXamarin
{
    internal class ActionSelectionFragment : Fragment
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.coarse_reloc_action_selection, container, false);
        }
    }
}
