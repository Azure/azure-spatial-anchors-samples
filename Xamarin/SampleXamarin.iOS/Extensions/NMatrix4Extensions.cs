// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace SampleXamarin.iOS
{
    using OpenTK;
    using SceneKit;

    public static class NMatrix4Extensions
    {
        /// <summary>
        /// Converts a transform to a position.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <returns>A <see cref="SCNVector3"/> position.</returns>
        public static SCNVector3 ToPosition(this NMatrix4 transform)
        {
            return new SCNVector3(transform.M14, transform.M24, transform.M34);
        }
    }
}
