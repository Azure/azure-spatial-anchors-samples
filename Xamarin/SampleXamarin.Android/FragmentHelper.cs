// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Android.Support.V4.App;

namespace SampleXamarin
{
    public class FragmentHelper
    {
        public static bool BackToPreviousFragment(FragmentActivity activity)
        {
            if (activity == null)
            {
                return false;
            }

            FragmentManager fragmentManager = activity.SupportFragmentManager;
            if (fragmentManager.BackStackEntryCount == 0)
            {
                return false;
            }

            fragmentManager.PopBackStack();
            return true;
        }

        public static void ReplaceFragment(FragmentActivity activity, Fragment fragment)
        {
            SwitchToFragment(activity, fragment, false);
        }

        public static void PushFragment(FragmentActivity activity, Fragment fragment)
        {
            SwitchToFragment(activity, fragment, true);
        }

        private static void SwitchToFragment(FragmentActivity activity, Fragment fragment, bool preserveCurrent)
        {
            FragmentManager fragmentManager = activity.SupportFragmentManager;
            FragmentTransaction transaction = fragmentManager.BeginTransaction();
            transaction.Replace(Resource.Id.ux_frame, fragment);
            if (preserveCurrent)
            {
                transaction.AddToBackStack(null);
            }
            transaction.Commit();
        }
    }
}
