// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.os.AsyncTask;

import java.io.DataInputStream;
import java.io.InputStream;
import java.net.HttpURLConnection;
import java.net.URL;

// Gets an anchor GUID from the service detailed in the
// Azure Spatial Anchors share anchors across devices tutorial
// Consumes the anchor number associated with the GUID for easier typing
class AnchorGetter extends AsyncTask<String, Void, String>
{
    private String baseAddress;
    private Shared sharedActivity;

    public AnchorGetter(String BaseAddress, Shared SharedActivity)
    {
        baseAddress = BaseAddress;
        sharedActivity= SharedActivity;
    }

    public String GetAnchor(String AnchorNumber)
    {
        String ret = "";
        try {
            String anchorAddress = baseAddress+"/"+AnchorNumber;
            URL url = new URL(anchorAddress);
            HttpURLConnection connection = (HttpURLConnection) url.openConnection();
            connection.setRequestMethod("GET");

            int responseCode = connection.getResponseCode();
            InputStream res = new DataInputStream(connection.getInputStream());

            String temp = "";
            int readValue = -1;
            do {
                readValue = res.read();
                if(readValue!=-1)
                {
                    temp += (char)readValue;
                }
            }while(readValue != -1);


            ret = temp;

            connection.disconnect();
        }
        catch(Exception e)
        {
            ret = e.getMessage();
        }

        return ret;
    }

    @Override
    protected String doInBackground(String... input)
    {
        return GetAnchor(input[0]);
    }

    @Override
    protected void onPostExecute(String result)
    {
        sharedActivity.AnchorLookedUp(result);
    }
}
