using System;
using UnityEditor;
using UnityEngine;

namespace Flippit
{
    public class ApplicationData 
    {
        private const string SDKVersion = "0.0.1";
        private static readonly AppData Data;

        static ApplicationData()
        {
            Data.SDKVersion = SDKVersion;
            Data.UnityVersion = Application.unityVersion;
           
        }
        
    }
    /*public static AppData GetData()
    {
        return Data;
    }*/
}
