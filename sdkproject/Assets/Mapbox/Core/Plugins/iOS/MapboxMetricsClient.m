#import <MapboxMobileEvents/MapboxMobileEvents.h>

void initialize(const char* accessToken, const char* userAgentBase, const char* hostSDKVersion) {
    [[MMEEventsManager sharedManager] initializeWithAccessToken:[NSString stringWithUTF8String:accessToken] 
                                                  userAgentBase:[NSString stringWithUTF8String:userAgentBase]
                                                  hostSDKVersion:[NSString stringWithUTF8String:hostSDKVersion]];
}

void sendTurnstileEvent() {
    [[MMEEventsManager sharedManager] sendTurnstileEvent];    
}

void setLocationCollectionState(bool enable) {
	[MMEEventsManager sharedManager].metricsEnabled = enable;
}
void setSkuId(const char* skuId){
    [MMEEventsManager sharedManager].skuId = [NSString stringWithUTF8String:skuId];
}
