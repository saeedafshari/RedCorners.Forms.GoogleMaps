﻿using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Xamarin.Forms;
using Android.Content.PM;

namespace RedCorners.Forms.GoogleMaps.Demo.Droid
{
    [Activity(Label = "RedCorners.Forms.GoogleMaps.Demo", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]

    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);
            Xamarin.Forms.Forms.Init(this, savedInstanceState);
            RedCorners.Forms.GoogleMapsSystem.Init(this, savedInstanceState);

            LoadApplication(new App());
        }
    }
}