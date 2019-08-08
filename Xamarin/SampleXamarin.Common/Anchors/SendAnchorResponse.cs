// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;

namespace SampleXamarin.AnchorSharing
{
    public class SendAnchorResponse
    {
        /// <summary>
        /// Gets the anchor sharing service anchor number.
        /// </summary>
        public string AnchorNumber { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SendAnchorResponse"/> class.
        /// </summary>
        /// <param name="anchorNumber">The anchor number from the anchor sharing service.</param>
        /// <exception cref="System.ArgumentException"></exception>
        public SendAnchorResponse(string anchorNumber)
        {
            if (string.IsNullOrWhiteSpace(anchorNumber))
            {
                throw new ArgumentException("The anchor number cannot be null, empty, or whitespace.", nameof(anchorNumber));
            }

            this.AnchorNumber = anchorNumber;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="SendAnchorResponse"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator string(SendAnchorResponse value)
        {
            return value?.AnchorNumber;
        }
    }
}
