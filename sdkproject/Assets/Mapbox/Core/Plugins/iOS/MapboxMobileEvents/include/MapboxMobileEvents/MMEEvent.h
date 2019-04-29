#import <Foundation/Foundation.h>

@class MMEDate;
@class MMECommonEventData;

/*! @brief represents a telemetry event, with date, name and attributes */
@interface MMEEvent : NSObject <NSCopying,NSSecureCoding>

/*! @brief date on which the event occured, including the local time offset */
@property (nonatomic, copy) MMEDate *date;

/*! @brief name of the event */
@property (nonatomic, copy) NSString *name;

/*! @brief attributes of the event */
@property (nonatomic, copy) NSDictionary *attributes;

#pragma mark -

+ (instancetype)eventWithDate:(MMEDate *)eventDate name:(NSString *)name attributes:(NSDictionary *)attributes;
+ (instancetype)eventWithName:(NSString *)eventName attributes:(NSDictionary *)attributes;

+ (instancetype)turnstileEventWithAttributes:(NSDictionary *)attributes;
+ (instancetype)telemetryMetricsEventWithDateString:(NSString *)dateString attributes:(NSDictionary *)attributes;
+ (instancetype)locationEventWithAttributes:(NSDictionary *)attributes instanceIdentifer:(NSString *)instanceIdentifer commonEventData:(MMECommonEventData *)commonEventData;
+ (instancetype)visitEventWithAttributes:(NSDictionary *)attributes;
+ (instancetype)mapLoadEventWithDateString:(NSString *)dateString commonEventData:(MMECommonEventData *)commonEventData;
+ (instancetype)mapTapEventWithDateString:(NSString *)dateString attributes:(NSDictionary *)attributes;
+ (instancetype)mapDragEndEventWithDateString:(NSString *)dateString attributes:(NSDictionary *)attributes;
+ (instancetype)mapOfflineDownloadStartEventWithDateString:(NSString *)dateString attributes:(NSDictionary *)attributes;
+ (instancetype)mapOfflineDownloadEndEventWithDateString:(NSString *)dateString attributes:(NSDictionary *)attributes;
+ (instancetype)navigationEventWithName:(NSString *)name attributes:(NSDictionary *)attributes;
+ (instancetype)visionEventWithName:(NSString *)name attributes:(NSDictionary *)attributes;
+ (instancetype)debugEventWithAttributes:(NSDictionary *)attributes;
+ (instancetype)debugEventWithError:(NSError*) error;
+ (instancetype)debugEventWithException:(NSException*) except;
+ (instancetype)searchEventWithName:(NSString *)name attributes:(NSDictionary *)attributes;
+ (instancetype)carplayEventWithName:(NSString *)name attributes:(NSDictionary *)attributes;
+ (instancetype)eventWithDateString:(NSString *)dateString name:(NSString *)name attributes:(NSDictionary *)attributes;


@end
