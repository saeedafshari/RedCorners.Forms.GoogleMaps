using System;
namespace RedCorners.Forms.GoogleMaps
{
    public sealed class MyLocationButtonClickedEventArgs : EventArgs
    {
        public bool Handled { get; set; } = false;

        internal MyLocationButtonClickedEventArgs()
        {
        }
    }
}
