#import <Foundation/Foundation.h>

extern NSString * const MMEAPIClientBaseURL;
extern NSString * const MMEAPIClientBaseAPIURL;
extern NSString * const MMEAPIClientBaseChinaEventsURL;
extern NSString * const MMEAPIClientBaseChinaAPIURL;
extern NSString * const MMEAPIClientEventsPath;
extern NSString * const MMEAPIClientEventsConfigPath;
extern NSString * const MMEAPIClientAttachmentsPath;
extern NSString * const MMEAPIClientHeaderFieldUserAgentKey;
extern NSString * const MMEAPIClientHeaderFieldContentTypeKey;
extern NSString * const MMEAPIClientHeaderFieldContentTypeValue;
extern NSString * const MMEAPIClientAttachmentsHeaderFieldContentTypeValue;
extern NSString * const MMEAPIClientHeaderFieldContentEncodingKey;
extern NSString * const MMEAPIClientHTTPMethodPost;
extern NSString * const MMEAPIClientHTTPMethodGet;
extern NSString * const MMEErrorDomain;

// Debug types
extern NSString * const MMEDebugEventType;
extern NSString * const MMEDebugEventTypeError;
extern NSString * const MMEDebugEventTypeFlush;
extern NSString * const MMEDebugEventTypePush;
extern NSString * const MMEDebugEventTypePost;
extern NSString * const MMEDebugEventTypePostFailed;
extern NSString * const MMEDebugEventTypeTurnstile;
extern NSString * const MMEDebugEventTypeTurnstileFailed;
extern NSString * const MMEDebugEventTypeBackgroundTask;
extern NSString * const MMEDebugEventTypeMetricCollection;
extern NSString * const MMEDebugEventTypeLocationManager;
extern NSString * const MMEDebugEventTypeTelemetryMetrics;

// Event types
extern NSString * const MMEEventTypeAppUserTurnstile;
extern NSString * const MMEEventTypeTelemetryMetrics;
extern NSString * const MMEEventTypeMapLoad;
extern NSString * const MMEEventTypeMapTap;
extern NSString * const MMEEventTypeMapDragEnd;
extern NSString * const MMEEventTypeLocation;
extern NSString * const MMEEventTypeVisit;
extern NSString * const MMEEventTypeLocalDebug;
extern NSString * const MMEventTypeOfflineDownloadStart;
extern NSString * const MMEventTypeOfflineDownloadEnd;

// Gestures
extern NSString * const MMEEventGestureSingleTap;
extern NSString * const MMEEventGestureDoubleTap;
extern NSString * const MMEEventGestureTwoFingerSingleTap;
extern NSString * const MMEEventGestureQuickZoom;
extern NSString * const MMEEventGesturePanStart;
extern NSString * const MMEEventGesturePinchStart;
extern NSString * const MMEEventGestureRotateStart;
extern NSString * const MMEEventGesturePitchStart;

// Event keys
extern NSString * const MMEEventKeyArrivalDate;
extern NSString * const MMEEventKeyDepartureDate;
extern NSString * const MMEEventKeyLatitude;
extern NSString * const MMEEventKeyLongitude;
extern NSString * const MMEEventKeyZoomLevel;
extern NSString * const MMEEventKeyMaxZoomLevel;
extern NSString * const MMEEventKeyMinZoomLevel;
extern NSString * const MMEEventKeyGestureID;
extern NSString * const MMEEventKeyEvent;
extern NSString * const MMEEventKeyCreated;
extern NSString * const MMEEventKeyStyleURL;
extern NSString * const MMEEventKeyVendorID;
extern NSString * const MMEEventKeyModel;
extern NSString * const MMEEventKeyDevice;
extern NSString * const MMEEventKeyEnabledTelemetry;
extern NSString * const MMEEventKeyOperatingSystem;
extern NSString * const MMEEventKeyResolution;
extern NSString * const MMEEventKeyAccessibilityFontScale;
extern NSString * const MMEEventKeyOrientation;
extern NSString * const MMEEventKeyPluggedIn;
extern NSString * const MMEEventKeyWifi;
extern NSString * const MMEEventKeyShapeForOfflineRegion;
extern NSString * const MMEEventKeySource;
extern NSString * const MMEEventKeySessionId;
extern NSString * const MMEEventKeyApplicationState;
extern NSString * const MMEEventKeyAltitude;
extern NSString * const MMEEventKeyLocationAuthorization;
extern NSString * const MMEEventKeyLocationEnabled;
extern NSString * const MMEEventHorizontalAccuracy;
extern NSString * const MMEEventSDKIdentifier;
extern NSString * const MMEEventSDKVersion;
extern NSString * const MMEEventKeyLocalDebugDescription;
extern NSString * const MMEEventKeyErrorCode;
extern NSString * const MMEEventKeyErrorDescription;
extern NSString * const MMEEventKeyErrorFailureReason;
extern NSString * const MMEEventKeyFailedRequests;
extern NSString * const MMEEventKeyHeader;
extern NSString * const MMEEventKeyPlatform;
extern NSString * const MMEEventKeyUserAgent;
extern NSString * const MMEEventKeyiOS;
extern NSString * const MMEEventKeyMac;
extern NSString * const MMENavigationEventPrefix;
extern NSString * const MMEVisionEventPrefix;
extern NSString * const MMEEventTypeNavigationDepart;
extern NSString * const MMEEventTypeNavigationArrive;
extern NSString * const MMEEventTypeNavigationCancel;
extern NSString * const MMEEventTypeNavigationFeedback;
extern NSString * const MMEEventTypeNavigationReroute;
extern NSString * const MMEventTypeNavigationCarplayConnect;
extern NSString * const MMEventTypeNavigationCarplayDisconnect;
extern NSString * const MMEEventTypeSearchSelected;
extern NSString * const MMEEventTypeSearchFeedback;
extern NSString * const MMESearchEventPrefix;
extern NSString * const MMEEventDateUTC;
extern NSString * const MMEEventRequests;
extern NSString * const MMEEventTotalDataSent;
extern NSString * const MMEEventCellDataSent;
extern NSString * const MMEEventWiFiDataSent;
extern NSString * const MMEEventTotalDataReceived;
extern NSString * const MMEEventCellDataReceived;
extern NSString * const MMEEventWiFiDataReceived;
extern NSString * const MMEEventAppWakeups;
extern NSString * const MMEEventEventCountPerType;
extern NSString * const MMEEventEventCountFailed;
extern NSString * const MMEEventEventCountTotal;
extern NSString * const MMEEventEventCountMax;
extern NSString * const MMEEventDeviceLat;
extern NSString * const MMEEventDeviceLon;
extern NSString * const MMEEventDeviceTimeDrift;
extern NSString * const MMEEventConfigResponse;
extern NSString * const MMEEventStatusDenied;
extern NSString * const MMEEventStatusRestricted;
extern NSString * const MMEEventStatusNotDetermined;
extern NSString * const MMEEventStatusAuthorizedAlways;
extern NSString * const MMEEventStatusAuthorizedWhenInUse;
extern NSString * const MMEEventUnknown;

extern NSString * const MMEResponseKey;

// SDK event source
extern NSString * const MMEEventSource;

// Log reporter HTML
extern NSString * const MMELoggerHTML;
extern NSString * const MMELoggerShareableHTML;

@interface MMEConstants: NSObject

@end
