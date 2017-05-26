#import <MapboxMobileEvents/MapboxMobileEvents.h>

void initialize(const char* accessToken, const char* userAgentBase, const char* hostSDKVersion) {    
    [[MMEEventsManager sharedManager] initializeWithAccessToken:[NSString stringWithUTF8String:accessToken] 
                                                  userAgentBase:[NSString stringWithUTF8String:userAgentBase]
                                                  hostSDKVersion:[NSString stringWithUTF8String:hostSDKVersion]];
}

void sendTurnstyleEvent() {
    [[MMEEventsManager sharedManager] sendTurnstileEvent];    
}
