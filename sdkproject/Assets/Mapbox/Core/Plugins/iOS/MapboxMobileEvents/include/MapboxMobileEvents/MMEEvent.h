#import <Foundation/Foundation.h>

@class MMECommonEventData;

@interface MMEEvent : NSObject

@property (nonatomic, copy) NSString *name;
@property (nonatomic, copy) NSDictionary *attributes;

+ (instancetype)turnstileEventWithAttributes:(NSDictionary *)attributes;
+ (instancetype)locationEventWithAttributes:(NSDictionary *)attributes instanceIdentifer:(NSString *)instanceIdentifer commonEventData:(MMECommonEventData *)commonEventData;
+ (instancetype)debugEventWithAttributes:(NSDictionary *)attributes;

@end
