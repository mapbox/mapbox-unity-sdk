#import <Foundation/Foundation.h>
#import "MMEEvent.h"

@class MMELocationManager;

NS_ASSUME_NONNULL_BEGIN

@interface MMEEventsManager : NSObject

@property (nonatomic, getter=isTelemetryDisabled) BOOL telemetryDisabled;

+ (nullable instancetype)sharedManager;

- (void)initializeWithAccessToken:(NSString *)accessToken userAgentBase:(NSString *)userAgentBase hostSDKVersion:(NSString *)hostSDKVersion;

- (void)sendTurnstileEvent;

@end

NS_ASSUME_NONNULL_END
