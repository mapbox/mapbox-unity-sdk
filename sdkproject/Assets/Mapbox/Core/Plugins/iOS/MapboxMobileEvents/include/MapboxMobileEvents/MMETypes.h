#import <Foundation/Foundation.h>

#ifndef NS_ARRAY_OF
    // Foundation collection classes adopted lightweight generics in iOS 9.0 and OS X 10.11 SDKs.
    #if __has_feature(objc_generics) && (__IPHONE_OS_VERSION_MAX_ALLOWED >= 90000 || __MAC_OS_X_VERSION_MAX_ALLOWED >= 101100)
    /** Inserts a type specifier for a pointer to a lightweight generic with the given collection and object classes. Use a `*` for any non-`id` object classes but no `*` for the collection class. */
        #define NS_ARRAY_OF(ObjectClass...)                 NSArray <ObjectClass>
        #define NS_MUTABLE_ARRAY_OF(ObjectClass...)         NSMutableArray <ObjectClass>
        #define NS_SET_OF(ObjectClass...)                   NSSet <ObjectClass>
        #define NS_MUTABLE_SET_OF(ObjectClass...)           NSMutableSet <ObjectClass>
        #define NS_DICTIONARY_OF(ObjectClass...)            NSDictionary <ObjectClass>
        #define NS_MUTABLE_DICTIONARY_OF(ObjectClass...)    NSMutableDictionary <ObjectClass>
    #else
        #define NS_ARRAY_OF(ObjectClass...)                 NSArray
        #define NS_MUTABLE_ARRAY_OF(ObjectClass...)         NSMutableArray
        #define NS_SET_OF(ObjectClass...)                   NSSet
        #define NS_MUTABLE_SET_OF(ObjectClass...)           NSMutableSet
        #define NS_DICTIONARY_OF(ObjectClass...)            NSDictionary
        #define NS_MUTABLE_DICTIONARY_OF(ObjectClass...)    NSMutableDictionary
    #endif
#endif

typedef NS_DICTIONARY_OF(NSString *, id) MMEMapboxEventAttributes;
typedef NS_MUTABLE_DICTIONARY_OF(NSString *, id) MMEMutableMapboxEventAttributes;

#ifdef MME_DEPRECATION_WARNINGS

#ifndef MME_DEPRECATED
    #define MME_DEPRECATED __attribute__((deprecated))
#endif

#ifndef MME_DEPRECATED_MSG
    #define MME_DEPRECATED_MSG(msg) __attribute((deprecated((msg))))
#endif

#ifndef MME_DEPRECATED_GOTO
    #define MME_DEPRECATED_GOTO(msg,label) __attribute((deprecated((msg),(label))))
#endif

#else

#ifndef MME_DEPRECATED
    #define MME_DEPRECATED
#endif

#ifndef MME_DEPRECATED_MSG
    #define MME_DEPRECATED_MSG(msg)
#endif

#ifndef MME_DEPRECATED_GOTO
    #define MME_DEPRECATED_GOTO(msg,label)
#endif

#endif // MME_DEPRECATION_WARNINGS
