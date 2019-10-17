// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;

namespace SampleXamarin.AnchorSharing
{
    public class RetrieveAnchorResponse
    {
        /// <summary>
        /// Gets a response indicating that the anchor could not be found.
        /// </summary>
        public static RetrieveAnchorResponse NotFound => new RetrieveAnchorResponse();

        /// <summary>
        /// Gets a value indicating whether the was anchor found or not.
        /// </summary>
        public bool AnchorFound { get; }

        /// <summary>
        /// Gets the Azure Spatial Anchors anchor identifier.
        /// </summary>
        public string AnchorId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetrieveAnchorResponse"/> class.
        /// </summary>
        /// <param name="anchorId">The anchor identifier.</param>
        /// <exception cref="System.ArgumentException"></exception>
        public RetrieveAnchorResponse(string anchorId)
        {
            if (string.IsNullOrWhiteSpace(anchorId))
            {
                throw new ArgumentException("The anchor identifier cannot be null, empty, or whitespace.", nameof(anchorId));
            }

            this.AnchorId = anchorId;
            this.AnchorFound = true;
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="RetrieveAnchorResponse"/> class from being created.
        /// </summary>
        private RetrieveAnchorResponse()
        {
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="RetrieveAnchorResponse"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator string(RetrieveAnchorResponse value)
        {
            return value?.AnchorId;
        }
    }
}
