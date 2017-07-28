#import <Foundation/Foundation.h>

extern NSString * const MMEAPIClientBaseURL;
extern NSString * const MMETelemetryTestServerURL;
extern NSString * const MMETelemetryStagingAccessToken;
extern NSString * const MMEAPIClientEventsPath;
extern NSString * const MMEAPIClientHeaderFieldUserAgentKey;
extern NSString * const MMEAPIClientHeaderFieldContentTypeKey;
extern NSString * const MMEAPIClientHeaderFieldContentTypeValue;
extern NSString * const MMEAPIClientHeaderFieldContentEncodingKey;
extern NSString * const MMEAPIClientHTTPMethodPost;
extern NSString * const MMEErrorDomain;

// Event types
extern NSString * const MMEEventTypeAppUserTurnstile;
extern NSString * const MMEEventTypeMapLoad;
extern NSString * const MMEEventTypeMapTap;
extern NSString * const MMEEventTypeMapDragEnd;
extern NSString * const MMEEventTypeLocation;
extern NSString * const MMEEventTypeLocalDebug;

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
extern NSString * const MMEEventKeyLatitude;
extern NSString * const MMEEventKeyLongitude;
extern NSString * const MMEEventKeyZoomLevel;
extern NSString * const MMEEventKeyGestureID;
extern NSString * const MMEEventKeyEvent;
extern NSString * const MMEEventKeyCreated;
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
extern NSString * const MMEEventKeySource;
extern NSString * const MMEEventKeySessionId;
extern NSString * const MMEEventKeyApplicationState;
extern NSString * const MMEEventKeyAltitude;
extern NSString * const MMEEventHorizontalAccuracy;
extern NSString * const MMEEventSDKIdentifier;
extern NSString * const MMEEventSDKVersion;
extern NSString * const MMEEventKeyLocalDebugDescription;
extern NSString * const MMENavigationEventPrefix;
extern NSString * const MMEEventTypeNavigationDepart;
extern NSString * const MMEEventTypeNavigationArrive;
extern NSString * const MMEEventTypeNavigationCancel;
extern NSString * const MMEEventTypeNavigationFeedback;
extern NSString * const MMEEventTypeNavigationReroute;

// SDK event source
extern NSString * const MMEEventSource;

@interface MMEConstants: NSObject

@end
