﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace TextControlBox_DemoApp
{
    public class AppSettings
    {
        public static void SaveSettings(string Value, object data)
        {
            if (data == null)
                return;

            //cancel if data is a type
            if (data.ToString() == data.GetType().Name)
                return;

            ApplicationData.Current.LocalSettings.Values[Value] = data.ToString();
        }
        public static string GetSettings(string Value)
        {
            return ApplicationData.Current.LocalSettings.Values[Value] as string;
        }
        public static int GetSettingsAsInt(string Value, int defaultvalue = 0)
        {
            int toInt(string val, int defaultValue = 0)
            {
                if(int.TryParse(val, out int result))
                    return result;
                return defaultValue;
            }

            return ApplicationData.Current.LocalSettings.Values[Value] is string value
                ? toInt(value, defaultvalue) : defaultvalue;
        }
    }
}
