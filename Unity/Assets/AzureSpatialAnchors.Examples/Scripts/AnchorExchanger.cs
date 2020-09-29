// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    public class AnchorExchanger
    {
#if !UNITY_EDITOR
        private string baseAddress = "";

        private List<string> anchorkeys = new List<string>();

        public List<string> AnchorKeys
        {
            get
            {
                lock (anchorkeys)
                {
                    return new List<string>(anchorkeys);
                }
            }
        }

        public void WatchKeys(string exchangerUrl)
        {
            baseAddress = exchangerUrl;
            Task.Factory.StartNew(async () =>
                {
                    string previousKey = string.Empty;
                    while (true)
                    {
                        string currentKey = await RetrieveLastAnchorKey();
                        if (!string.IsNullOrWhiteSpace(currentKey) && currentKey != previousKey)
                        {
                            Debug.Log("Found key " + currentKey);
                            lock (anchorkeys)
                            {
                                anchorkeys.Add(currentKey);
                            }
                            previousKey = currentKey;
                        }
                        await Task.Delay(500);
                    }
                }, TaskCreationOptions.LongRunning);
        }

        public async Task<string> RetrieveAnchorKey(long anchorNumber)
        {
            try
            {
                HttpClient client = new HttpClient();
                return await client.GetStringAsync(baseAddress + "/" + anchorNumber.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to retrieve anchor key for anchor number: {anchorNumber}.");
                return null;
            }
        }

        public async Task<string> RetrieveLastAnchorKey()
        {
            try
            {
                HttpClient client = new HttpClient();
                return await client.GetStringAsync(baseAddress + "/last");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError("Failed to retrieve last anchor key.");
                return null;
            }
        }

        internal async Task<long> StoreAnchorKey(string anchorKey)
        {
            if (string.IsNullOrWhiteSpace(anchorKey))
            {
                return -1;
            }

            try
            {
                HttpClient client = new HttpClient();
                var response = await client.PostAsync($"{baseAddress}/key?anchorKey={anchorKey}", new StringContent(string.Empty));
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    long ret;
                    if (long.TryParse(responseBody, out ret))
                    {
                        Debug.Log("Key " + ret.ToString());
                        return ret;
                    }
                    else
                    {
                        Debug.LogError($"Failed to store the anchor key. Failed to parse the response body to a long: {responseBody}.");
                    }
                }
                else
                {
                    Debug.LogError($"Failed to store the anchor key: {response.StatusCode} {response.ReasonPhrase}.");
                }

                Debug.LogError($"Failed to store the anchor key: {anchorKey}.");
                return -1;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to store the anchor key: {anchorKey}.");
                return -1;
            }
        }
#endif
    }
}
