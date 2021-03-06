﻿﻿using System;
 using System.Collections.Generic;
 using System.ComponentModel;
using Xamarin.Forms.Platform.iOS;
using Google.Maps;
using System.Drawing;
using RedCorners.Forms.GoogleMaps.Logics.iOS;
using RedCorners.Forms.GoogleMaps.Logics;
using RedCorners.Forms.GoogleMaps.iOS.Extensions;
using UIKit;
using RedCorners.Forms.GoogleMaps.Internals;
using GCameraUpdate = Google.Maps.CameraUpdate;
using GCameraPosition = Google.Maps.CameraPosition;
using System.Threading.Tasks;
using Foundation;
using Xamarin.Forms;
using Xamarin;

[assembly: ExportRenderer(typeof(RedCorners.Forms.GoogleMaps.MapBase), typeof(RedCorners.Forms.GoogleMaps.iOS.MapRenderer))]
[assembly: Preserve]
namespace RedCorners.Forms.GoogleMaps.iOS
{
    public class MapRenderer : ViewRenderer
    {
        public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, IntPtr context)
        {
            var mkMapView = ofObject as MapView;
            if (mkMapView?.MyLocation != null)
                MapLocationSystem.Instance.InjectMapModel(
                    mkMapView.MyLocation.Coordinate.Latitude,
                    mkMapView.MyLocation.Coordinate.Longitude);
        }

        bool _shouldUpdateRegion = true;

        // ReSharper disable once MemberCanBePrivate.Global
        protected MapView NativeMap => (MapView)Control;
        // ReSharper disable once MemberCanBePrivate.Global
        protected MapBase Map => (MapBase)Element;

        protected internal static PlatformConfig Config { protected get; set; }

        readonly UiSettingsLogic _uiSettingsLogic = new UiSettingsLogic();
        readonly CameraLogic _cameraLogic;

        private bool _ready;

        internal readonly IList<BaseLogic<MapView>> Logics;
        
        public MapRenderer()
        {
            Logics = new List<BaseLogic<MapView>>
            {
                new PolylineLogic(),
                new PolygonLogic(),
                new CircleLogic(),
                new PinLogic(Config.ImageFactory, OnMarkerCreating, OnMarkerCreated, OnMarkerDeleting, OnMarkerDeleted),
                new TileLayerLogic(),
                new GroundOverlayLogic(Config.ImageFactory)
            };

            _cameraLogic = new CameraLogic(() =>
            {
                OnCameraPositionChanged(NativeMap.Camera);
            });
        }

        public override SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            return Control.GetSizeRequest(widthConstraint, heightConstraint);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if(Map!=null)
                {
                    Map.OnSnapshot -= OnSnapshot;
                    foreach (var logic in Logics)
                    {
                        logic.Unregister(NativeMap, Map);
                    }
                }               
                _cameraLogic.Unregister();
                _uiSettingsLogic.Unregister();

                var mkMapView = (MapView)Control;
                if(mkMapView!=null)
                {
                    mkMapView.CoordinateLongPressed -= CoordinateLongPressed;
                    mkMapView.CoordinateTapped -= CoordinateTapped;
                    mkMapView.CameraPositionChanged -= CameraPositionChanged;
                    mkMapView.DidTapMyLocationButton = null;
                }
            }

            base.Dispose(disposing);
        }

        protected override void OnElementChanged(ElementChangedEventArgs<View> e)
        {
            base.OnElementChanged(e);

            // For XAML Previewer or GoogleMapsSystem.Init not called.
            if (!GoogleMapsSystem.IsInitialized)
            {
                var label = new UILabel()
                {
                    Text = "RedCorners.Forms.GoogleMaps",
                    BackgroundColor = Xamarin.Forms.Color.Teal.ToUIColor(),
                    TextColor = Xamarin.Forms.Color.Black.ToUIColor(),
                    TextAlignment = UITextAlignment.Center
                };
                SetNativeControl(label);
                return;
            }

            var oldMapView = (MapView)Control;
            if (e.OldElement != null)
            {
                var oldMapModel = (MapBase)e.OldElement;
                oldMapModel.OnSnapshot -= OnSnapshot;
                _cameraLogic.Unregister();

                if (oldMapView != null)
                {
                    oldMapView.CoordinateLongPressed -= CoordinateLongPressed;
                    oldMapView.CoordinateTapped -= CoordinateTapped;
                    oldMapView.CameraPositionChanged -= CameraPositionChanged;
                    oldMapView.DidTapMyLocationButton = null;
                }
            }

            if (e.NewElement != null)
            {
                var mapModel = (MapBase)e.NewElement;

                if (Control == null)
                {
                    SetNativeControl(new MapView(CoreGraphics.CGRect.Empty));
                    var mkMapView = (MapView)Control;
                    mkMapView.CameraPositionChanged += CameraPositionChanged;
                    mkMapView.CoordinateTapped += CoordinateTapped;
                    mkMapView.CoordinateLongPressed += CoordinateLongPressed;
                    mkMapView.DidTapMyLocationButton = DidTapMyLocation;
                    InvokeOnMainThread(() => mkMapView.AddObserver(this, new NSString("myLocation"), NSKeyValueObservingOptions.New, IntPtr.Zero));
                }

                _cameraLogic.Register(Map, NativeMap);
                Map.OnSnapshot += OnSnapshot;

                //_cameraLogic.MoveCamera(mapModel.InitialCameraUpdate);
                //_ready = true;

                _uiSettingsLogic.Register(Map, NativeMap);
                UpdateMapType();
                UpdateHasScrollEnabled(_uiSettingsLogic.ScrollGesturesEnabled);
                UpdateHasZoomEnabled(_uiSettingsLogic.ZoomGesturesEnabled);
                UpdateHasRotationEnabled(_uiSettingsLogic.RotateGesturesEnabled);
                UpdateIsTrafficEnabled();
                UpdatePadding();
                UpdateMapStyle();
                UpdateMyLocationEnabled();
                _uiSettingsLogic.Initialize();

                foreach (var logic in Logics)
                {
                    logic.Register(oldMapView, (MapBase)e.OldElement, NativeMap, Map);
                    logic.RestoreItems();
                    logic.OnMapPropertyChanged(new PropertyChangedEventArgs(MapBase.SelectedPinProperty.PropertyName));
                }

            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            // For XAML Previewer or GoogleMapsSystem.Init not called.
            if (!GoogleMapsSystem.IsInitialized)
            {
                return;
            }

            if (e.PropertyName == MapBase.MapTypeProperty.PropertyName)
            {
                UpdateMapType();
            }
            else if (e.PropertyName == MapBase.MyLocationEnabledProperty.PropertyName ||
                e.PropertyName == MapBase.IsMyLocationButtonVisibleProperty.PropertyName)
            {
                UpdateMyLocationEnabled();
            }
            else if (e.PropertyName == MapBase.HasScrollEnabledProperty.PropertyName)
            {
                UpdateHasScrollEnabled();
            }
            else if (e.PropertyName == MapBase.HasRotationEnabledProperty.PropertyName)
            {
                UpdateHasRotationEnabled();
            }
            else if (e.PropertyName == MapBase.HasZoomEnabledProperty.PropertyName)
            {
                UpdateHasZoomEnabled();
            }
            else if (e.PropertyName == MapBase.IsTrafficEnabledProperty.PropertyName)
            {
                UpdateIsTrafficEnabled();
            }
            else if (e.PropertyName == VisualElement.HeightProperty.PropertyName &&
                     ((MapBase) Element).InitialCameraUpdate != null)
            {
                _shouldUpdateRegion = true;
            }
            else if (e.PropertyName == MapBase.IndoorEnabledProperty.PropertyName)
            {
                UpdateHasIndoorEnabled();
            }
            else if (e.PropertyName == MapBase.PaddingProperty.PropertyName)
            {
                UpdatePadding();
            }
            else if (e.PropertyName == MapBase.MapStyleProperty.PropertyName)
            {
                UpdateMapStyle();
            }

            foreach (var logic in Logics)
            {
                logic.OnMapPropertyChanged(e);
            }
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            // For XAML Previewer or GoogleMapsSystem.Init not called.
            if (!GoogleMapsSystem.IsInitialized)
            {
                return;
            }

            if (_shouldUpdateRegion && !_ready)
            {
                _cameraLogic.MoveCamera(((MapBase)Element).InitialCameraUpdate);
                _ready = true;
                _shouldUpdateRegion = false;
                UpdateMyLocationEnabled();
            }

        }

        void OnSnapshot(TakeSnapshotMessage snapshotMessage)
        {
            UIGraphics.BeginImageContextWithOptions(NativeMap.Frame.Size, false, 0f);
            NativeMap.Layer.RenderInContext(UIGraphics.GetCurrentContext());
            var snapshot = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();

            // Why using task? Because Android side is asynchronous. 
            Task.Run(() => 
            {
                snapshotMessage.OnSnapshot.Invoke(snapshot.AsPNG().AsStream());
            });
        }

        void CameraPositionChanged(object sender, GMSCameraEventArgs args)
        {
            OnCameraPositionChanged(args.Position);
        }

        void OnCameraPositionChanged(GCameraPosition pos)
        {
            if (Element == null)
                return;

            var mkMapView = (MapView)Control;

            Map.Region = mkMapView.Projection.VisibleRegion.ToRegion();

            var camera = pos.ToXamarinForms();
            Map.CameraPosition = camera;
            Map.SendCameraChanged(camera);
        }

        void CoordinateTapped(object sender, GMSCoordEventArgs e)
        {
            Map.SendMapClicked(e.Coordinate.ToPosition());
        }

        void CoordinateLongPressed(object sender, GMSCoordEventArgs e)
        {
            Map.SendMapLongClicked(e.Coordinate.ToPosition());
        }

        bool DidTapMyLocation(MapView mapView)
        {
            return Map.SendMyLocationClicked();
        }

        private void UpdateHasScrollEnabled(bool? initialScrollGesturesEnabled = null)
        {
#pragma warning disable 618
            NativeMap.Settings.ScrollGestures = initialScrollGesturesEnabled ?? ((MapBase)Element).HasScrollEnabled;
#pragma warning restore 618
        }

        private void UpdateHasZoomEnabled(bool? initialZoomGesturesEnabled = null)
        {
#pragma warning disable 618
            NativeMap.Settings.ZoomGestures = initialZoomGesturesEnabled ?? ((MapBase)Element).HasZoomEnabled;
#pragma warning restore 618
        }

        private void UpdateHasRotationEnabled(bool? initialRotateGesturesEnabled = null)
        {
#pragma warning disable 618
            NativeMap.Settings.RotateGestures = initialRotateGesturesEnabled ?? ((MapBase)Element).HasRotationEnabled;
#pragma warning restore 618
        }

        void UpdateMyLocationEnabled()
        {
            ((MapView)Control).MyLocationEnabled = ((MapBase)Element).MyLocationEnabled;
            ((MapView)Control).Settings.MyLocationButton = ((MapBase)Element).IsMyLocationButtonVisible;
        }

        void UpdateIsTrafficEnabled()
        {
            ((MapView)Control).TrafficEnabled = ((MapBase)Element).IsTrafficEnabled;
        }

        void UpdateHasIndoorEnabled()
        {
            ((MapView) Control).IndoorEnabled = ((MapBase)Element).IsIndoorEnabled;
        }

        void UpdateMapType()
        {
            switch (((MapBase)Element).MapType)
            {
                case MapType.Street:
                    ((MapView)Control).MapType = MapViewType.Normal;
                    break;
                case MapType.Satellite:
                    ((MapView)Control).MapType = MapViewType.Satellite;
                    break;
                case MapType.Hybrid:
                    ((MapView)Control).MapType = MapViewType.Hybrid;
                    break;
                case MapType.Terrain:
                    ((MapView)Control).MapType = MapViewType.Terrain;
                    break;
                case MapType.None:
                    ((MapView)Control).MapType = MapViewType.None;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void UpdatePadding()
        {
            ((MapView)Control).Padding = ((MapBase)Element).Padding.ToUIEdgeInsets();
        }

        void UpdateMapStyle()
        {
            var styleJson = ((MapBase)Element).MapStyle;
            if (!string.IsNullOrWhiteSpace(styleJson))
                ((MapView)Control).MapStyle = MapStyle.FromJson(styleJson, null);
            else
                ((MapView)Control).MapStyle = null;
        }

        #region Overridable Members

        /// <summary>
        /// Call when before marker create.
        /// You can override your custom renderer for customize marker.
        /// </summary>
        /// <param name="outerItem">the pin.</param>
        /// <param name="innerItem">the marker options.</param>
        protected virtual void OnMarkerCreating(Pin outerItem, Marker innerItem)
        {
        }

        /// <summary>
        /// Call when after marker create.
        /// You can override your custom renderer for customize marker.
        /// </summary>
        /// <param name="outerItem">the pin.</param>
        /// <param name="innerItem">thr marker.</param>
        protected virtual void OnMarkerCreated(Pin outerItem, Marker innerItem)
        {
        }

        /// <summary>
        /// Call when before marker delete.
        /// You can override your custom renderer for customize marker.
        /// </summary>
        /// <param name="outerItem">the pin.</param>
        /// <param name="innerItem">thr marker.</param>
        protected virtual void OnMarkerDeleting(Pin outerItem, Marker innerItem)
        {
        }

        /// <summary>
        /// Call when after marker delete.
        /// You can override your custom renderer for customize marker.
        /// </summary>
        /// <param name="outerItem">the pin.</param>
        /// <param name="innerItem">thr marker.</param>
        protected virtual void OnMarkerDeleted(Pin outerItem, Marker innerItem)
        {
        }

        #endregion    
    }
}
