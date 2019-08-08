// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SampleXamarin.AnchorSharing
{
    public class AnchorSharingServiceClient
    {
        private readonly string endpointUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnchorSharingServiceClient"/> class.
        /// </summary>
        /// <param name="endpointUrl">The anchor sharing service's URL endpoint.</param>
        /// <exception cref="System.ArgumentException"></exception>
        public AnchorSharingServiceClient(string endpointUrl)
        {
            if (string.IsNullOrWhiteSpace(endpointUrl))
            {
                throw new ArgumentException("The base address cannot be null, empty, or whitespace.", nameof(endpointUrl));
            }

            this.endpointUrl = endpointUrl;
        }

        /// <summary>
        /// Sends the Azure Spatial Anchors anchor identifier to the anchor sharing service be shared with others.
        /// </summary>
        /// <param name="anchorId">The Azure Spatial Anchors anchor identifier.</param>
        /// <returns><see cref="Task{SendAnchorResponse}"/>.</returns>
        public async Task<SendAnchorResponse> SendAnchorIdAsync(string anchorId)
        {
            using (HttpClient client = new HttpClient())
            {
                StringContent content = new StringContent(anchorId);
                HttpResponseMessage response = await client.PostAsync(this.endpointUrl, content);

                response.EnsureSuccessStatusCode();

                string anchorNumber = await response.Content.ReadAsStringAsync();

                return new SendAnchorResponse(anchorNumber);
            }
        }

        /// <summary>
        /// Retrieves an anchor identifier from the anchor sharing service using the specified anchor number.
        /// </summary>
        /// <param name="anchorNumber">The anchor number from the anchor sharing service.</param>
        /// <returns><see cref="Task{RetrieveAnchorResponse}"/>.</returns>
        public async Task<RetrieveAnchorResponse> RetrieveAnchorIdAsync(string anchorNumber)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage httpResponse = await client.GetAsync($"{this.endpointUrl}/{anchorNumber}");

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        string anchorId = await httpResponse.Content.ReadAsStringAsync();

                        return new RetrieveAnchorResponse(anchorId);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return RetrieveAnchorResponse.NotFound;
        }
    }
}