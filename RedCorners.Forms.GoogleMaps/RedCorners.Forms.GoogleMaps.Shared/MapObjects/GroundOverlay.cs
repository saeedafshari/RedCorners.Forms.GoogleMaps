﻿
using Neat.Map.Models;

using System;
using System.Linq;

using Xamarin.Forms;

namespace RedCorners.Forms.GoogleMaps
{
    public sealed class GroundOverlay : MapObject
    {
        public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(BitmapDescriptor), typeof(GroundOverlay), default(BitmapDescriptor));
        public static readonly BindableProperty TransparencyProperty = BindableProperty.Create(nameof(Transparency), typeof(float), typeof(GroundOverlay), 0f);
        public static readonly BindableProperty BoundsProperty = BindableProperty.Create(nameof(Bounds), typeof(Bounds), typeof(GroundOverlay), default(Bounds));
        public static readonly BindableProperty BearingProperty = BindableProperty.Create(nameof(Bearing), typeof(float), typeof(GroundOverlay), 0f);
        public static readonly BindableProperty IsClickableProperty = BindableProperty.Create(nameof(IsClickable), typeof(bool), typeof(GroundOverlay), false);
        public static readonly BindableProperty ZIndexProperty = BindableProperty.Create(nameof(ZIndex), typeof(int), typeof(GroundOverlay), 0);
        public static readonly BindableProperty IsVisibleProperty = BindableProperty.Create(nameof(IsVisible), typeof(bool), typeof(Pin), true);

        public BitmapDescriptor Icon
        {
            get { return (BitmapDescriptor)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public float Transparency
        {
            get { return (float)GetValue(TransparencyProperty); }
            set { SetValue(TransparencyProperty, value); }
        }

        public Bounds Bounds
        {
            get { return (Bounds)GetValue(BoundsProperty); }
            set { SetValue(BoundsProperty, value); }
        }

        public float Bearing
        {
            get { return (float)GetValue(BearingProperty); }
            set { SetValue(BearingProperty, value); }
        }

        public bool IsClickable
        {
            get { return (bool)GetValue(IsClickableProperty); }
            set { SetValue(IsClickableProperty, value); }
        }

        public int ZIndex
        {
            get { return (int)GetValue(ZIndexProperty); }
            set { SetValue(ZIndexProperty, value); }
        }

        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        public object NativeObject { get; internal set; }

        public event EventHandler Clicked;

        internal bool SendTap()
        {
            EventHandler handler = Clicked;
            if (handler == null)
                return false;

            handler(this, EventArgs.Empty);
            return true;
        }

        public override bool ShouldCull(MapRegion region)
        {
            return !region.Contains(Bounds);
        }

        public override bool ShouldCull(Position position, Distance distance)
        {
            if (Bounds == null) return true;
            return MapLocationSystem.CalculateDistance(position, Bounds.Center) <= distance;
        }

        internal override Position? GetRelativePosition(Position reference)
        {
            return new Position[]
            {
                Bounds.NorthEast,
                Bounds.NorthWest,
                Bounds.SouthEast,
                Bounds.SouthWest,
                Bounds.Center
            }
            .OrderBy(x => MapLocationSystem.CalculateDistance(reference, x))
            .First();
        }
    }
}