// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.os.AsyncTask;

import java.io.BufferedInputStream;
import java.io.DataOutputStream;
import java.io.InputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.util.function.Consumer;

// Posts an anchor GUID to the service detailed in the
// Azure Spatial Anchors share anchors across devices tutorial
// Provides the anchor number associated with the GUID for easier typing
class AnchorPoster extends AsyncTask<String, Void, String>
{
    private final String baseAddress;
    private final Consumer<String> anchorPostedCallback;

    public AnchorPoster(String baseAddress, Consumer<String> anchorPostedCallback) {
        this.baseAddress = baseAddress;
        this.anchorPostedCallback = anchorPostedCallback;
    }

    @Override
    protected String doInBackground(String... input) {
        return postAnchor(input[0]);
    }

    @Override
    protected void onPostExecute(String result) {
        if (anchorPostedCallback != null) {
            anchorPostedCallback.accept(result);
        }
    }

    private String postAnchor(String anchor) {
        String ret;
        try {
            URL url = new URL(baseAddress);
            HttpURLConnection connection = (HttpURLConnection) url.openConnection();
            connection.setRequestMethod("POST");
            connection.setDoOutput(true);
            DataOutputStream output = new DataOutputStream(connection.getOutputStream());
            output.writeBytes(anchor);

            int responseCode = connection.getResponseCode();
            InputStream res = new BufferedInputStream(connection.getInputStream());

            byte[] resBytes = new byte[16];

            res.read(resBytes, 0, resBytes.length);
            ret = new String(resBytes);
            connection.disconnect();
        }
        catch(Exception e)
        {
            ret = e.getMessage();
        }

        return ret;
    }
}
