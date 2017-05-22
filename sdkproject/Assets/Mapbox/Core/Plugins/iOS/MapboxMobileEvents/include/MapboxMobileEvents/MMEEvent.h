#import <Foundation/Foundation.h>
#import "MMETypes.h"

@class MMECommonEventData;

typedef NS_DICTIONARY_OF(NSString *, id) MGLMapboxEventAttributes;
typedef NS_MUTABLE_DICTIONARY_OF(NSString *, id) MGLMutableMapboxEventAttributes;

@interface MMEEvent : NSObject

@property (nonatomic, copy) NSString *name;
@property (nonatomic, copy) MGLMapboxEventAttributes *attributes;

+ (instancetype)turnstileEventWithAttributes:(MGLMapboxEventAttributes *)attributes;

+ (instancetype)locationEventWithAttributes:(MGLMapboxEventAttributes *)attributes instanceIdentifer:(NSString *)instanceIdentifer commonEventData:(MMECommonEventData *)commonEventData;

@end
