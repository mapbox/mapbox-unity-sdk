// Unity Technologies Inc (c) 2018
// ARKitNativeObjectDetection.mm
// Main implementation of ARKit plugin object detection


#include "ARKitDefines.h"


bool referenceObject_GetSupported()
{
    return UnityAreFeaturesSupported(kUnityARKitSupportedFeaturesReferenceObject);
}

typedef struct
{
    void* identifier;
    UnityARMatrix4x4 transform;
    void* referenceObjectName;
    void* referenceObjectPtr;
} UnityARObjectAnchorData;

typedef struct
{
    UnityARAlignment alignment;
    UnityARPlaneDetection planeDetection;
    uint32_t enableLightEstimation;
    uint32_t enableAutoFocus;
    uint32_t getPointCloudData;
} ARKitObjectScanningSessionConfiguration;

API_AVAILABLE(ios(12.0))
inline void UnityARObjectAnchorDataFromARObjectAnchorPtr(UnityARObjectAnchorData& anchorData, ARObjectAnchor* nativeAnchor)
{
    anchorData.identifier = (void*)[nativeAnchor.identifier.UUIDString UTF8String];
    ARKitMatrixToUnityARMatrix4x4(nativeAnchor.transform, &anchorData.transform);
    anchorData.referenceObjectName = (void*)[nativeAnchor.referenceObject.name UTF8String];
    anchorData.referenceObjectPtr = (__bridge void*) nativeAnchor.referenceObject;
}

API_AVAILABLE(ios(12.0))
inline void GetARSessionConfigurationFromARKitObjectScanningSessionConfiguration(ARKitObjectScanningSessionConfiguration& unityConfig, ARObjectScanningConfiguration* appleConfig)
{
    appleConfig.planeDetection = GetARPlaneDetectionFromUnityARPlaneDetection(unityConfig.planeDetection);
    appleConfig.worldAlignment = GetARWorldAlignmentFromUnityARAlignment(unityConfig.alignment);
    appleConfig.lightEstimationEnabled = (BOOL)unityConfig.enableLightEstimation;
    
    if (UnityIsARKit_2_0_Supported())
    {
        appleConfig.autoFocusEnabled = (BOOL) unityConfig.enableAutoFocus;
    }
}

typedef void (*UNITY_AR_OBJECT_ANCHOR_CALLBACK)(UnityARObjectAnchorData anchorData);


@interface UnityARObjectAnchorCallbackWrapper : NSObject <UnityARAnchorEventDispatcher>
{
@public
    UNITY_AR_OBJECT_ANCHOR_CALLBACK _anchorAddedCallback;
    UNITY_AR_OBJECT_ANCHOR_CALLBACK _anchorUpdatedCallback;
    UNITY_AR_OBJECT_ANCHOR_CALLBACK _anchorRemovedCallback;
}
@end

@implementation UnityARObjectAnchorCallbackWrapper

-(void)sendAnchorAddedEvent:(ARAnchor*)anchor
{
    UnityARObjectAnchorData data;
    if (@available(iOS 12.0, *))
    {
        UnityARObjectAnchorDataFromARObjectAnchorPtr(data, (ARObjectAnchor*)anchor);
    }
    _anchorAddedCallback(data);
}

-(void)sendAnchorRemovedEvent:(ARAnchor*)anchor
{
    UnityARObjectAnchorData data;
    if (@available(iOS 12.0, *))
    {
        UnityARObjectAnchorDataFromARObjectAnchorPtr(data, (ARObjectAnchor*)anchor);
    }
    _anchorRemovedCallback(data);
}

-(void)sendAnchorUpdatedEvent:(ARAnchor*)anchor
{
    UnityARObjectAnchorData data;
    if (@available(iOS 12.0, *))
    {
        UnityARObjectAnchorDataFromARObjectAnchorPtr(data, (ARObjectAnchor*)anchor);
    }
    _anchorUpdatedCallback(data);
}

@end

extern "C" void session_SetObjectAnchorCallbacks(const void* session, UNITY_AR_OBJECT_ANCHOR_CALLBACK objectAnchorAddedCallback,
                                                UNITY_AR_OBJECT_ANCHOR_CALLBACK objectAnchorUpdatedCallback,
                                                UNITY_AR_OBJECT_ANCHOR_CALLBACK objectAnchorRemovedCallback)
{
    if (UnityIsARKit_2_0_Supported())
    {
        UnityARSession* nativeSession = (__bridge UnityARSession*)session;
        UnityARObjectAnchorCallbackWrapper* objectAnchorCallbacks = [[UnityARObjectAnchorCallbackWrapper alloc] init];
        objectAnchorCallbacks->_anchorAddedCallback = objectAnchorAddedCallback;
        objectAnchorCallbacks->_anchorUpdatedCallback = objectAnchorUpdatedCallback;
        objectAnchorCallbacks->_anchorRemovedCallback = objectAnchorRemovedCallback;
        if (@available(iOS 12.0, *))
        {
            [nativeSession->_classToCallbackMap setObject:objectAnchorCallbacks forKey:[ARObjectAnchor class]];
        }
    }
}

#ifdef __cplusplus
extern "C" {
#endif

bool IsARKitObjectScanningConfigurationSupported()
{
    if (@available(iOS 12.0, *))
    {
        return ARObjectScanningConfiguration.isSupported;
    }
    else
    {
        // Fallback on earlier versions
        return false;
    }
}
    
void StartObjectScanningSessionWithOptions(void* nativeSession, ARKitObjectScanningSessionConfiguration unityConfig, UnityARSessionRunOptions runOptions)
{
    UnityARSession* session = (__bridge UnityARSession*)nativeSession;
    ARSessionRunOptions runOpts = GetARSessionRunOptionsFromUnityARSessionRunOptions(runOptions);
    session->_getLightEstimation = (BOOL) unityConfig.enableLightEstimation;
    session->_getPointCloudData = (BOOL) unityConfig.getPointCloudData;
    if (@available(iOS 12.0, *))
    {
        ARObjectScanningConfiguration* config = [ARObjectScanningConfiguration new];
        GetARSessionConfigurationFromARKitObjectScanningSessionConfiguration(unityConfig, config);
        if (runOptions == UnityARSessionRunOptionsNone)
            [session->_session runWithConfiguration:config];
        else
            [session->_session runWithConfiguration:config options:runOpts];
    }
    else
    {
        // Fallback on earlier versions
        NSLog(@"ARKit error: your device does not support ARKit 2.0");
        return;
    }
    
    [session setupMetal];
}


    
    
bool referenceObject_ExportObjectToURL(const void* ptr, const char* path)
{
    if (ptr == nullptr || path == nullptr)
        return false;

    if (@available(iOS 12.0, *))
    {
        NSError* error = nil;
        ARReferenceObject* referenceObject = (__bridge ARReferenceObject*)ptr;
        NSURL* url = [[NSURL alloc] initFileURLWithPath:[NSString stringWithUTF8String:path] isDirectory:false];
        [referenceObject exportObjectToURL:url previewImage:nil error:&error];
        
        if (error)
            NSLog(@"%@", error);

        return (error == nil);
    }
    else
    {
        // Fallback on earlier versions
        return false;
    }
}

bool referenceObject_Save(const void* referenceObjectPtr, const char* path)
{
    if (referenceObjectPtr == nullptr || path == nullptr || !referenceObject_GetSupported())
        return false;
    
    if (@available(iOS 12.0, *))
    {
        ARReferenceObject* referenceObject = (__bridge ARReferenceObject*)referenceObjectPtr;
        NSError* writeError = nil;
        NSURL* url = [[NSURL alloc] initFileURLWithPath:[NSString stringWithUTF8String:path] isDirectory:false];
        NSData *data = [NSKeyedArchiver archivedDataWithRootObject:referenceObject];
        [data writeToURL:url options:(NSDataWritingAtomic) error:&writeError];
        
        if (writeError)
            NSLog(@"%@", writeError);
        
        return (writeError == nil);
    }
    else
    {
        // Fallback on earlier versions
        return false;
    }
}

void* referenceObject_Load(const char* path)
{
    if (!referenceObject_GetSupported())
        return nullptr;
    
    if (@available(iOS 12.0, *))
    {
        NSError* error = nil;
        NSURL* url = [[NSURL alloc] initFileURLWithPath:[NSString stringWithUTF8String:path] isDirectory:false];
        NSData *rodata = [NSData dataWithContentsOfURL:url options:NSDataReadingMappedAlways error:&error];

        if (error)
            NSLog(@"%@", error);
        
        ARReferenceObject* referenceObject = [NSKeyedUnarchiver unarchiveObjectWithData:rodata];
        
        return (__bridge_retained void*)referenceObject;
    }
    else
    {
        // Fallback on earlier versions
        return nullptr;
    }
}

void* referenceObject_InitWithArchiveUrl(const char* path)
{
    if (!referenceObject_GetSupported())
        return nullptr;
    
    if (@available(iOS 12.0, *))
    {
        NSError* error = nil;
        NSURL* url = [[NSURL alloc] initFileURLWithPath:[NSString stringWithUTF8String:path] isDirectory:false];
        
        ARReferenceObject* referenceObject = [[ARReferenceObject alloc] initWithArchiveURL:url error:&error];
        
        if (error)
            NSLog(@"%@", error);
        
        return (__bridge_retained void*)referenceObject;
    }
    else
    {
        // Fallback on earlier versions
        return nullptr;
    }
}
    
long referenceObject_SerializedLength(const void* referenceObjectPtr)
{
    if (@available(iOS 12.0, *))
    {
        ARReferenceObject* referenceObject = (__bridge ARReferenceObject*)referenceObjectPtr;
        NSData *data = [NSKeyedArchiver archivedDataWithRootObject:referenceObject];
        return data.length;
    }
    else
    {
        // Fallback on earlier versions
        return 0;
    }
}

void referenceObject_SerializeToByteArray(const void*  referenceObjectPtr, void*  pinnedArray)
{
    if (@available(iOS 12.0, *))
    {
        ARReferenceObject* referenceObject = (__bridge ARReferenceObject*)referenceObjectPtr;
        NSData *data = [NSKeyedArchiver archivedDataWithRootObject:referenceObject];
        memcpy(pinnedArray, data.bytes, data.length);
    }
}

void* referenceObject_SerializeFromByteArray(const void*  pinnedArray, long lengthBytes)
{
    
    if (@available(iOS 12.0, *))
    {
        NSData *rodata = [NSData dataWithBytes:pinnedArray length:lengthBytes];
        ARReferenceObject* referenceObject = [NSKeyedUnarchiver unarchiveObjectWithData:rodata];
        return (__bridge_retained void*)referenceObject;
    }
    else
    {
        // Fallback on earlier versions
        return nullptr;
    }
    
}

UnityARVector3 referenceObject_GetCenter(const void* ptr)
{
    if (ptr == nullptr)
        return UnityARVector3{0, 0, 0};
    
    if (@available(iOS 12.0, *))
    {
        ARReferenceObject* referenceObject = (__bridge ARReferenceObject*)ptr;
        return UnityARVector3
        {
            referenceObject.center.x,
            referenceObject.center.y,
            referenceObject.center.z
        };
    }
    else
    {
        // Fallback on earlier versions
        return UnityARVector3{0, 0, 0};
    }
}

UnityARVector3 referenceObject_GetExtent(const void* ptr)
{
    if (ptr == nullptr)
        return UnityARVector3{0, 0, 0};
    
    if (@available(iOS 12.0, *))
    {
        ARReferenceObject* referenceObject = (__bridge ARReferenceObject*)ptr;
        return UnityARVector3
        {
            referenceObject.extent.x,
            referenceObject.extent.y,
            referenceObject.extent.z
        };
    }
    else
    {
        // Fallback on earlier versions
        return UnityARVector3{0, 0, 0};
    }
}

void referenceObject_SetName(void* ptr, const char* name)
{
    if (ptr == nullptr)
        return;
    
    if (@available(iOS 12.0, *))
    {
        ARReferenceObject* referenceObject = (__bridge ARReferenceObject*)ptr;
        referenceObject.name = [[NSString alloc] initWithUTF8String:name];
    }
}
    
const char* referenceObject_GetName(void* ptr)
{
    if (ptr == nullptr)
        return nullptr;
    
    if (@available(iOS 12.0, *))
    {
        ARReferenceObject* referenceObject = (__bridge ARReferenceObject*)ptr;
        const char* nameAsCStr = [referenceObject.name UTF8String];
        // Make a copy because IL2CPP is going to try to free what we return
        const size_t length = strlen(nameAsCStr);
        char* retVal = (char*)malloc(length + 1);
        strcpy(retVal, nameAsCStr);
        
        return retVal;
    }
    else
    {
        // Fallback on earlier versions
        return nullptr;
    }
}
    
void* referenceObject_GetPointCloud(const void* referenceObjectPtr)
{
    if (referenceObjectPtr == nullptr || !UnityAreFeaturesSupported(kUnityARKitSupportedFeaturesReferenceObject))
        return nullptr;
    
    if (@available(iOS 12.0, *))
    {
        ARReferenceObject* referenceObject = (__bridge ARReferenceObject*)referenceObjectPtr;
        ARPointCloud *pointCloud = [referenceObject rawFeaturePoints];
        
        return (__bridge_retained void*)pointCloud;
    }
    else
    {
        // Fallback on earlier versions
        return nullptr;
    }
    
}

void* referenceObjectsSet_Create()
{
    if (@available(iOS 12.0, *))
    {
        NSMutableSet<ARReferenceObject*> *referenceObjectsSet = [[NSMutableSet<ARReferenceObject*> alloc] init];
        return (__bridge_retained void*) referenceObjectsSet;
    }
    else
    {
        // Fallback on earlier versions
        return nullptr;
    }
}

void referenceObjectsSet_AddReferenceObject(void* roSet, void* referenceObject)
{
    if (@available(iOS 12.0, *))
    {
        NSMutableSet<ARReferenceObject*> *referenceObjectsSet = (__bridge NSMutableSet<ARReferenceObject*> *)roSet;
        ARReferenceObject* refObject = (__bridge ARReferenceObject*)referenceObject;
        [referenceObjectsSet addObject:refObject];
    }
}

#ifdef __cplusplus
}
#endif
