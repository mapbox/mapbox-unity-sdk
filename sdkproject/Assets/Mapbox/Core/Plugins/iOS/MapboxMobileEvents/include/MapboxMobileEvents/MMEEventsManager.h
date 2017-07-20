#import <Foundation/Foundation.h>
#import "MMEEvent.h"
#import "MMETypes.h"

@class MMELocationManager;

NS_ASSUME_NONNULL_BEGIN

@interface MMEEventsManager : NSObject

@property (nonatomic, getter=isMetricsEnabled) BOOL metricsEnabled;
@property (nonatomic, getter=isMetricsEnabledInSimulator) BOOL metricsEnabledInSimulator;
@property (nonatomic, getter=isMetricsEnabledForInUsePermissions) BOOL metricsEnabledForInUsePermissions;
@property (nonatomic, getter=isDebugLoggingEnabled) BOOL debugLoggingEnabled;
@property (nonatomic, readonly) NSString *accessToken;
@property (nonatomic, readonly) NSString *userAgentBase;
@property (nonatomic, readonly) NSString *hostSDKVersion;
@property (nonatomic) NSInteger accountType;

+ (instancetype)sharedManager;

- (void)initializeWithAccessToken:(NSString *)accessToken userAgentBase:(NSString *)userAgentBase hostSDKVersion:(NSString *)hostSDKVersion;

- (void)pauseOrResumeMetricsCollectionIfRequired;
- (void)flush;
- (void)sendTurnstileEvent;
- (void)enqueueEventWithName:(NSString *)name;
- (void)enqueueEventWithName:(NSString *)name attributes:(MMEMapboxEventAttributes *)attributes;
- (void)disableLocationMetrics;

@end

NS_ASSUME_NONNULL_END
