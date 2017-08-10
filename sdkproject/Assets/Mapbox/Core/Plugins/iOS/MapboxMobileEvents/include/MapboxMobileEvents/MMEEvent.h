#import <Foundation/Foundation.h>

@class MMECommonEventData;

@interface MMEEvent : NSObject

@property (nonatomic, copy) NSString *name;
@property (nonatomic, copy) NSDictionary *attributes;

+ (instancetype)turnstileEventWithAttributes:(NSDictionary *)attributes;
+ (instancetype)locationEventWithAttributes:(NSDictionary *)attributes instanceIdentifer:(NSString *)instanceIdentifer commonEventData:(MMECommonEventData *)commonEventData;
+ (instancetype)mapLoadEventWithDateString:(NSString *)dateString commonEventData:(MMECommonEventData *)commonEventData;
+ (instancetype)mapTapEventWithDateString:(NSString *)dateString attributes:(NSDictionary *)attributes;
+ (instancetype)mapDragEndEventWithDateString:(NSString *)dateString attributes:(NSDictionary *)attributes;
+ (instancetype)navigationEventWithName:(NSString *)name attributes:(NSDictionary *)attributes;
+ (instancetype)debugEventWithAttributes:(NSDictionary *)attributes;

@end
