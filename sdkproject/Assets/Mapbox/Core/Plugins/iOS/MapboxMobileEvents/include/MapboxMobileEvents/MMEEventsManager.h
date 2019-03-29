#import <Foundation/Foundation.h>
#import "MMEEvent.h"
#import "MMETypes.h"
#import <CoreLocation/CoreLocation.h>

NS_ASSUME_NONNULL_BEGIN

@protocol MMEEventsManagerDelegate;

@interface MMEEventsManager : NSObject

@property (nonatomic, weak) id<MMEEventsManagerDelegate> delegate;
@property (nonatomic, getter=isMetricsEnabled) BOOL metricsEnabled;
@property (nonatomic, getter=isMetricsEnabledInSimulator) BOOL metricsEnabledInSimulator;
@property (nonatomic, getter=isMetricsEnabledForInUsePermissions) BOOL metricsEnabledForInUsePermissions;
@property (nonatomic, getter=isDebugLoggingEnabled) BOOL debugLoggingEnabled;
@property (nonatomic, readonly) NSString *userAgentBase;
@property (nonatomic, readonly) NSString *hostSDKVersion;
@property (nonatomic, copy) NSString *accessToken;
@property (nonatomic, null_resettable) NSURL *baseURL;
@property (nonatomic) NSInteger accountType;

#pragma mark -

+ (instancetype)sharedManager;

#pragma mark -

- (void)initializeWithAccessToken:(NSString *)accessToken userAgentBase:(NSString *)userAgentBase hostSDKVersion:(NSString *)hostSDKVersion;

- (void)pauseOrResumeMetricsCollectionIfRequired;
- (void)flush;
- (void)resetEventQueuing;
- (void)sendTurnstileEvent;
- (void)sendTelemetryMetricsEvent;
- (void)enqueueEventWithName:(NSString *)name;
- (void)enqueueEventWithName:(NSString *)name attributes:(MMEMapboxEventAttributes *)attributes;
- (void)postMetadata:(NSArray *)metadata filePaths:(NSArray *)filePaths completionHandler:(nullable void (^)(NSError * _Nullable error))completionHandler;
- (void)disableLocationMetrics;

- (void)displayLogFileFromDate:(NSDate *)logDate;

@end

#pragma mark -

@protocol MMEEventsManagerDelegate <NSObject>

@optional

- (void)eventsManager:(MMEEventsManager *)eventsManager didUpdateLocations:(NSArray<CLLocation *> *)locations;
- (void)eventsManager:(MMEEventsManager *)eventsManager didVisit:(CLVisit *)visit;

@end

NS_ASSUME_NONNULL_END
