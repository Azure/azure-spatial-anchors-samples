// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.os.AsyncTask;

import java.io.DataInputStream;
import java.io.InputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.util.function.Consumer;

// Gets an anchor GUID from the service detailed in the
// Azure Spatial Anchors share anchors across devices tutorial
// Consumes the anchor number associated with the GUID for easier typing
class AnchorGetter extends AsyncTask<String, Void, String>
{
    private final String baseAddress;
    private final Consumer<String> anchorLocatedCallback;

    public AnchorGetter(String baseAddress, Consumer<String> anchorLocatedCallback) {
        this.baseAddress = baseAddress;
        this.anchorLocatedCallback = anchorLocatedCallback;
    }

    @Override
    protected String doInBackground(String... input) {
        return getAnchor(input[0]);
    }

    @Override
    protected void onPostExecute(String result) {
        if (this.anchorLocatedCallback != null) {
            this.anchorLocatedCallback.accept(result);
        }
    }

    private String getAnchor(String AnchorNumber) {
        String ret;
        try {
            String anchorAddress = baseAddress+"/"+AnchorNumber;
            URL url = new URL(anchorAddress);
            HttpURLConnection connection = (HttpURLConnection) url.openConnection();
            connection.setRequestMethod("GET");

            int responseCode = connection.getResponseCode();
            InputStream res = new DataInputStream(connection.getInputStream());

            StringBuilder temp = new StringBuilder();
            int readValue = -1;
            do {
                readValue = res.read();
                if(readValue != -1)
                {
                    temp.append((char)readValue);
                }
            } while(readValue != -1);


            ret = temp.toString();

            connection.disconnect();
        }
        catch(Exception e)
        {
            ret = e.getMessage();
        }

        return ret;
    }
}
