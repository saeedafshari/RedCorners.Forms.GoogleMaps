using System;

namespace RedCorners.Forms.GoogleMaps
{
    public sealed class MapClickedEventArgs : EventArgs
    {
        public Position Point { get; }

        internal MapClickedEventArgs(Position point)
        {
            this.Point = point;
        }
    }
}
