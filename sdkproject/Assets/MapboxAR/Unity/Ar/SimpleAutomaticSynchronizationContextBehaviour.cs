namespace Mapbox.Unity.Ar
{
    using Mapbox.Unity.Map;
    using Mapbox.Unity.Location;
    using Mapbox.Utils;
    using UnityARInterface;
    using UnityEngine;
    using Mapbox.Unity.Utilities;
    using System;
    using UnityEngine.UI;

    public class SimpleAutomaticSynchronizationContextBehaviour : MonoBehaviour, ISynchronizationContext
    {
        public Text compassvalue;
        public Button GPS_Button;
        float firstCompassValue;
        GameObject target;



        public InstructionObject _firstTarget; 
        public InstructionObject secondTarget;
        public InstructionObject takingGps;
        public InstructionObject endofCalibration;


        [SerializeField]
        Transform _arPositionReference;

        [SerializeField]
        AbstractMap _map;

        [SerializeField]
        bool _useAutomaticSynchronizationBias;

        [SerializeField]
        AbstractAlignmentStrategy _alignmentStrategy;

        [SerializeField]
        float _synchronizationBias = 1f;

        [SerializeField]
        float _arTrustRange = 10f;

        [SerializeField]
        float _minimumDeltaDistance = 2f;

        [SerializeField]
        float _minimumDesiredAccuracy = 5f;

        SimpleAutomaticSynchronizationContext _synchronizationContext;

        float _lastHeading;
        float _lastHeight;

        // TODO: move to "base" class SimpleAutomaticSynchronizationContext
        // keep it here for now as map position is also calculated here
        //private KalmanLatLong _kalman = new KalmanLatLong(3); // 3:very fast walking

        ILocationProvider _locationProvider;

        public event Action<Alignment> OnAlignmentAvailable = delegate { };

        public ILocationProvider LocationProvider
        {
            private get
            {
                if (_locationProvider == null)
                {
#if UNITY_EDITOR
                    Debug.LogWarningFormat("SimpleAutomaticSynchronizationContextBehaviour, isRemoteConnected:{0}", UnityEditor.EditorApplication.isRemoteConnected);
                    if (!UnityEditor.EditorApplication.isRemoteConnected)
                    {
                        _locationProvider = LocationProviderFactory.Instance.TransformLocationProvider;
                    }
                    else
                    {
                        _locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
                    }
#else
					_locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
#endif
                }

                return _locationProvider;
            }
            set
            {
                if (_locationProvider != null)
                {
                    _locationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;

                }
                _locationProvider = value;
                _locationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
            }
        }

        private void Start()
        {

            InstructionUXCanvas.Instance.SetInstruction(_firstTarget);



        }


        void Awake()
        {
            Input.compass.enabled = true;
            Input.location.Start(1f, 0.1f);//could be a problem? Since location parameters are not the same with Device Location Provider

            _alignmentStrategy.Register(this);
            _synchronizationContext = new SimpleAutomaticSynchronizationContext();
            _synchronizationContext.MinimumDeltaDistance = _minimumDeltaDistance;
            _synchronizationContext.ArTrustRange = _arTrustRange;
            _synchronizationContext.UseAutomaticSynchronizationBias = _useAutomaticSynchronizationBias;
            _synchronizationContext.SynchronizationBias = _synchronizationBias;
            _synchronizationContext.OnAlignmentAvailable += SynchronizationContext_OnAlignmentAvailable;
            _map.OnInitialized += Map_OnInitialized;


            // TODO: not available in ARInterface yet?!
            //UnityARSessionNativeInterface.ARSessionTrackingChangedEvent += UnityARSessionNativeInterface_ARSessionTrackingChanged;
            ARInterface.planeAdded += PlaneAddedHandler;

            //First Arrow
            target = Instantiate(Resources.Load("target")) as GameObject;
            target.transform.position = new Vector3(0, 0, 2);
            //InstructionUXCanvas.Instance.SetInstruction(_firstTarget);
        }


        void OnDestroy()
        {
            _alignmentStrategy.Unregister(this);
            LocationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;
            ARInterface.planeAdded -= PlaneAddedHandler;
        }


        void Map_OnInitialized()
        {
            _map.OnInitialized -= Map_OnInitialized;

            // We don't want location updates until we have a map, otherwise our conversion will fail.
            LocationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
        }


        void PlaneAddedHandler(BoundedPlane plane)
        {
            _lastHeight = plane.center.y;
            //Unity.Utilities.Console.Instance.Log(string.Format("AR Plane Height: {0}", _lastHeight), "yellow");
        }

        private void Update()
        {
            

        }
        private void LateUpdate()
        {
            if (firstCompassValue == 0)
            {
                firstCompassValue = Input.compass.trueHeading;
                return;
            }
            compassvalue.text = firstCompassValue.ToString();


        }


        //void UnityARSessionNativeInterface_ARSessionTrackingChanged(UnityEngine.XR.iOS.UnityARCamera camera)
        //{
        //	Unity.Utilities.Console.Instance.Log(string.Format("AR Tracking State Changed: {0}: {1}", camera.trackingState, camera.trackingReason), "silver");
        //}



        int step = 0;//initial step where user is instructed about calibration process

        public void GPS()
        {
            if (step == 0)
            {
                step = 1;//Second step where user is at the first target and pressed GPS button waiting for GPS update.
                InstructionUXCanvas.Instance.SetInstruction(takingGps);
            }
            else if (step == 2)//This means third step where user is instructed to walk to second target to complete the calibration.
            {
                step = 3;
                InstructionUXCanvas.Instance.SetInstruction(takingGps);
            }
        }

        void LocationProvider_OnLocationUpdated(Location location)
        {
            if (step == 1)
            {

                if (location.IsLocationUpdated)
                {
                    if (location.Accuracy > _minimumDesiredAccuracy) //With this line, we can control accuracy of Gps updates. 
                    {
                        Unity.Utilities.Console.Instance.Log(
                            string.Format(
                                "Gps update ignored due to bad accuracy: {0:0.0} > {1:0.0}"
                                , location.Accuracy
                                , _minimumDesiredAccuracy
                            )
                            , "red"
                        );
                    }
                    else
                    {
                        var latitudeLongitude = location.LatitudeLongitude;
                        Unity.Utilities.Console.Instance.Log(
                            string.Format(
                                "Location: {0},{1}\tAccuracy: {2}\tHeading: {3}"
                                , latitudeLongitude.x
                                , latitudeLongitude.y
                                , location.Accuracy, location.UserHeading
                            )
                            , "lightblue"
                        );

                        var position = Conversions.GeoToWorldPosition(latitudeLongitude, _map.CenterMercator, _map.WorldRelativeScale).ToVector3xz();
                        _synchronizationContext.AddSynchronizationNodes(location, position, _arPositionReference.localPosition, firstCompassValue);

                        step = 2;//third step where user is instructed to walk to second target to complete the calibration.

                        //put GPS target further north to make sure that user walks in straight line along its north.This will do the trick
                        target.transform.position = new Vector3(0, 0, 4);


                        //direct user to go to second GPS target.
                        InstructionUXCanvas.Instance.SetInstruction(secondTarget);



                    }


                }

            }
            else if (step == 3)
            {

                if (location.IsLocationUpdated)
                {
                    if (location.Accuracy > _minimumDesiredAccuracy) //With this line, we can control accuracy of Gps updates. 
                    {
                        Unity.Utilities.Console.Instance.Log(
                            string.Format(
                                "Gps update ignored due to bad accuracy: {0:0.0} > {1:0.0}"
                                , location.Accuracy
                                , _minimumDesiredAccuracy
                            )
                            , "red"
                        );
                    }
                    else
                    {
                        var latitudeLongitude = location.LatitudeLongitude;
                        Unity.Utilities.Console.Instance.Log(
                            string.Format(
                                "Location: {0},{1}\tAccuracy: {2}\tHeading: {3}"
                                , latitudeLongitude.x
                                , latitudeLongitude.y
                                , location.Accuracy, location.UserHeading
                            )
                            , "lightblue"
                        );

                        var position = Conversions.GeoToWorldPosition(latitudeLongitude, _map.CenterMercator, _map.WorldRelativeScale).ToVector3xz();
                        _synchronizationContext.AddSynchronizationNodes(location, position, _arPositionReference.localPosition, firstCompassValue);

                        //destroy gps target since we no longer need it.
                        Destroy(target);

                        //End of calibration
                        InstructionUXCanvas.Instance.SetInstruction(endofCalibration);
                        InstructionUXCanvas.Instance.SetButtonActive();

                        //change step to 4 so that we do not get any gps updates.
                        //Because calibration with the first two GPS updates are the best calibration we can get in terms of error.
                        //So first aligment is the last known "good" and "only" aligment. After that we rely on manual calibration with taking roads as references.
                        step = 4;

                    }
                }

            }
        }


        void SynchronizationContext_OnAlignmentAvailable(Ar.Alignment alignment)
        {
            var position = alignment.Position;
            position.y = _lastHeight;
            alignment.Position = position;
            OnAlignmentAvailable(alignment);
        }
    }
}
