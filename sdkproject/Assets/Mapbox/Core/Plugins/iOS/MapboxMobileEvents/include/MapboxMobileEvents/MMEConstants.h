#import <Foundation/Foundation.h>
#import "MMETypes.h"

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
extern NSString * const MMEDebugEventTypeCertPinning;

// Event types
extern NSString * const MMEEventTypeAppUserTurnstile;
extern NSString * const MMEEventTypeTelemetryMetrics;
extern NSString * const MMEEventTypeMapLoad;
extern NSString * const MMEEventTypeLocation;
extern NSString * const MMEEventTypeVisit;
extern NSString * const MMEEventTypeLocalDebug;
extern NSString * const MMEventTypeOfflineDownloadStart;
extern NSString * const MMEventTypeOfflineDownloadEnd;

// Event keys
extern NSString * const MMEEventKeyArrivalDate;
extern NSString * const MMEEventKeyDepartureDate;
extern NSString * const MMEEventKeyLatitude;
extern NSString * const MMEEventKeyLongitude;
extern NSString * const MMEEventKeyZoomLevel;
extern NSString * const MMEEventKeyMaxZoomLevel;
extern NSString * const MMEEventKeyMinZoomLevel;
extern NSString * const MMEEventKeyEvent;
extern NSString * const MMEEventKeyCreated;
extern NSString * const MMEEventKeyStyleURL;
extern NSString * const MMEEventKeyVendorId;
extern NSString * const MMEEventKeyModel;
extern NSString * const MMEEventKeyDevice;
extern NSString * const MMEEventKeySkuId;
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
extern NSString * const MMEEventKeyErrorDomain;
extern NSString * const MMEEventKeyErrorDescription;
extern NSString * const MMEEventKeyErrorFailureReason;
extern NSString * const MMEEventKeyErrorNoReason;
extern NSString * const MMEEventKeyErrorNoDomain;
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

/*! @brief SDK event source */
extern NSString * const MMEEventSource;

#pragma mark - mobile.crash Keys

extern NSString * const MMEEventMobileCrash;
extern NSString * const MMEEventKeyOSVersion;
extern NSString * const MMEEventKeyBuildType;
extern NSString * const MMEEventKeyIsSilentCrash;
extern NSString * const MMEEventKeyStackTrace;
extern NSString * const MMEEventKeyStackTraceHash;
extern NSString * const MMEEventKeyInstallationId;
extern NSString * const MMEEventKeyThreadDetails;
extern NSString * const MMEEventKeyAppId;
extern NSString * const MMEEventKeyAppVersion;
extern NSString * const MMEEventKeyAppStartDate;
extern NSString * const MMEEventKeyCustomData;

#pragma mark - MMEErrorDomain

/*! @brief NSErrorDomain for MapboxMobileEvents */
extern NSErrorDomain const MMEErrorDomain;

/*! @brief MMEErrorDomain Error Numbers
    - MMENoError: No Error
    - MMEErrorException for exceptions
    - MMEErrorEventInit for errors when initlizing events
    - MMEErrorEventInitMissingKey if the event attributes dictionary does not include the event key,
    - MMEErrorEventInitException if an exception occured durring initWithAttributes:error:,
    - MMEErrorEventInitInvalid if the provided eventAttributes cannot be converted to JSON objects
*/
typedef NS_ENUM(NSInteger, MMEErrorNumber) {
    MMENoError = 0,
    MMEErrorException = 10001,
    MMEErrorEventInit = 10002,
    MMEErrorEventInitMissingKey = 10003,
    MMEErrorEventInitException  = 10004,
    MMEErrorEventInitInvalid    = 10005,
    MMEErrorEventEncoding = 10006,
    MMEErrorEventCounting = 10007
};

/*! @brief key for MMEErrorEventInit userInfo dictionary containing the attributes which failed to create the event */
extern NSString * const MMEErrorEventAttributesKey;

/*! @brief key for MMEErrorDomain userInfo dictionary containing the underlying exception which triggered the error */
extern NSString * const MMEErrorUnderlyingExceptionKey;

#pragma mark - Deprecated

extern NSString * const MMEErrorDescriptionKey; MME_DEPRECATED_MSG("Use NSLocalizedDescriptionKey")

extern NSString * const MMEEventKeyVendorID MME_DEPRECATED_MSG("Use MMEEventKeyVendorId");
extern NSString * const MMEEventKeyInstallationID MME_DEPRECATED_MSG("Use MMEEventKeyInstallationId");
extern NSString * const MMEEventKeyAppID MME_DEPRECATED_MSG("Use MMEEventKeyInstallationId");

extern NSString * const MMELoggerHTML MME_DEPRECATED;
extern NSString * const MMELoggerShareableHTML MME_DEPRECATED;

extern NSString * const MMEEventKeyGestureId MME_DEPRECATED;
extern NSString * const MMEEventKeyGestureID MME_DEPRECATED;
extern NSString * const MMEEventGestureSingleTap MME_DEPRECATED;
extern NSString * const MMEEventGestureDoubleTap MME_DEPRECATED;
extern NSString * const MMEEventGestureTwoFingerSingleTap MME_DEPRECATED;
extern NSString * const MMEEventGestureQuickZoom MME_DEPRECATED;
extern NSString * const MMEEventGesturePanStart MME_DEPRECATED;
extern NSString * const MMEEventGesturePinchStart MME_DEPRECATED;
extern NSString * const MMEEventGestureRotateStart MME_DEPRECATED;
extern NSString * const MMEEventGesturePitchStart MME_DEPRECATED;
extern NSString * const MMEEventTypeMapTap MME_DEPRECATED;
extern NSString * const MMEEventTypeMapDragEnd MME_DEPRECATED;
