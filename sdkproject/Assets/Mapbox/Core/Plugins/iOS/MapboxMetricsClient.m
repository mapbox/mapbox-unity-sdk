#import <MapboxMobileEvents/MapboxMobileEvents.h>

void initialize(const char* accessToken, const char* userAgentBase) {    
    [[MMEEventsManager sharedManager] initializeWithAccessToken:[NSString stringWithUTF8String:accessToken] 
                                                  userAgentBase:[NSString stringWithUTF8String:userAgentBase]];
}

void sendTurnstyleEvent() {
    [[MMEEventsManager sharedManager] sendTurnstileEvent];    
}
