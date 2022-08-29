// Unity Technologies Inc (c) 2017
// AREnvironmentProbe.mm
// Main implementation of ARKit plugin native AREnvironmentProbeAnchor

#include "ARKitDefines.h"


enum UnityAREnvironmentTextureFormat : long
{
    // NOTE: Not a complete set, but an initial mapping that matches some internal texture readmap mappings.
    UnityAREnvironmentTextureFormatR16,
    UnityAREnvironmentTextureFormatRG16,
    UnityAREnvironmentTextureFormatBGRA32,
    UnityAREnvironmentTextureFormatRGBA32,
    UnityAREnvironmentTextureFormatRGBAFloat,
    UnityAREnvironmentTextureFormatRGBAHalf,
    UnityAREnvironmentTextureFormatDefault = UnityAREnvironmentTextureFormatBGRA32
};

typedef struct
{
    void* cubemapPtr;
    UnityAREnvironmentTextureFormat textureFormat;
    int width;
    int height;
    int mipmapCount;
} UnityAREnvironmentProbeCubemapData;

typedef struct
{
    void* identifier;
    UnityARMatrix4x4 transform;
    UnityAREnvironmentProbeCubemapData cubemapData;
    UnityARVector3 extent;
} UnityAREnvironmentProbeAnchorData;

inline UnityAREnvironmentTextureFormat GetUnityAREnvironmentTextureFormat (MTLPixelFormat mtlPixelFormat)
{
    // This mapping is based on a Unity internal runtime method metal::UnityTextureFormat() in TextureFormatMetal.mm that maps a subset of the Metal pixel formats to the Unity texture format
    switch (mtlPixelFormat)
    {
        case MTLPixelFormatRGBA16Float:
            return UnityAREnvironmentTextureFormatRGBAHalf;
        case MTLPixelFormatRGBA32Float:
            return UnityAREnvironmentTextureFormatRGBAFloat;
        case MTLPixelFormatRGBA8Unorm_sRGB:
        case MTLPixelFormatRGBA8Unorm:
            return UnityAREnvironmentTextureFormatRGBA32;
        case MTLPixelFormatBGRA8Unorm_sRGB:
        case MTLPixelFormatBGRA8Unorm:
            return UnityAREnvironmentTextureFormatBGRA32;
        case MTLPixelFormatR16Unorm:
            return UnityAREnvironmentTextureFormatR16;
        case MTLPixelFormatRG8Unorm:
            return UnityAREnvironmentTextureFormatRG16;
        default:
            return UnityAREnvironmentTextureFormatDefault;
    }
}

inline void UnityAREnvironmentProbeCubemapDataFromMTLTextureRef(UnityAREnvironmentProbeCubemapData& cubemapData, MTLTextureRef environmentTexture)
{
    cubemapData.cubemapPtr = (__bridge void*)environmentTexture;
    cubemapData.textureFormat = GetUnityAREnvironmentTextureFormat([environmentTexture pixelFormat]);
    cubemapData.width = (int)[environmentTexture width];
    cubemapData.height = (int)[environmentTexture height];
    cubemapData.mipmapCount = (int)[environmentTexture mipmapLevelCount];
}

API_AVAILABLE(ios(12.0))
inline void UnityAREnvironmentProbeAnchorDataFromAREnvironmentProbeAnchorPtr(UnityAREnvironmentProbeAnchorData& anchorData, AREnvironmentProbeAnchor* nativeAnchor)
{
    anchorData.identifier = (void*)[nativeAnchor.identifier.UUIDString UTF8String];
    ARKitMatrixToUnityARMatrix4x4(nativeAnchor.transform, &anchorData.transform);
    UnityAREnvironmentProbeCubemapDataFromMTLTextureRef(anchorData.cubemapData, [nativeAnchor environmentTexture]);
    anchorData.extent = UnityARVector3 {
        nativeAnchor.extent.x,
        nativeAnchor.extent.y,
        nativeAnchor.extent.z
    };
}

typedef void (*UNITY_AR_ENVPROBE_ANCHOR_CALLBACK)(UnityAREnvironmentProbeAnchorData anchorData);


@interface UnityAREnvironmentProbeAnchorCallbackWrapper : NSObject <UnityARAnchorEventDispatcher>
{
@public
    UNITY_AR_ENVPROBE_ANCHOR_CALLBACK _anchorAddedCallback;
    UNITY_AR_ENVPROBE_ANCHOR_CALLBACK _anchorUpdatedCallback;
    UNITY_AR_ENVPROBE_ANCHOR_CALLBACK _anchorRemovedCallback;
}
@end

@implementation UnityAREnvironmentProbeAnchorCallbackWrapper

-(void)sendAnchorAddedEvent:(ARAnchor*)anchor
{
    if (@available(iOS 12.0, *))
    {
        UnityAREnvironmentProbeAnchorData data;
        UnityAREnvironmentProbeAnchorDataFromAREnvironmentProbeAnchorPtr(data, (AREnvironmentProbeAnchor*)anchor);
        _anchorAddedCallback(data);
    }
}

-(void)sendAnchorRemovedEvent:(ARAnchor*)anchor
{
    if (@available(iOS 12.0, *)) {
        UnityAREnvironmentProbeAnchorData data;
        UnityAREnvironmentProbeAnchorDataFromAREnvironmentProbeAnchorPtr(data, (AREnvironmentProbeAnchor*)anchor);
        _anchorRemovedCallback(data);
    }
}

-(void)sendAnchorUpdatedEvent:(ARAnchor*)anchor
{
    if (@available(iOS 12.0, *))
    {
        UnityAREnvironmentProbeAnchorData data;
        UnityAREnvironmentProbeAnchorDataFromAREnvironmentProbeAnchorPtr(data, (AREnvironmentProbeAnchor*)anchor);
        _anchorUpdatedCallback(data);
    }
}

@end

extern "C" UnityAREnvironmentProbeAnchorData SessionAddEnvironmentProbeAnchor(void* nativeSession, UnityAREnvironmentProbeAnchorData anchorData)
{
    UnityAREnvironmentProbeAnchorData returnAnchorData;

    if (UnityIsARKit_2_0_Supported())
    {
        // create a native AREnvironmentProbeAnchor and add it to the session
        // then return the data back to the user that they will
        // need in case they want to remove it
        UnityARSession* session = (__bridge UnityARSession*)nativeSession;
        matrix_float4x4 initMat;
        UnityARMatrix4x4ToARKitMatrix(anchorData.transform, &initMat);
        if (@available(iOS 12.0, *))
        {
            AREnvironmentProbeAnchor *newAnchor = [[AREnvironmentProbeAnchor alloc] initWithTransform:initMat];
            [session->_session addAnchor:newAnchor];
            UnityAREnvironmentProbeAnchorDataFromAREnvironmentProbeAnchorPtr(returnAnchorData, newAnchor);
        }
    }
    
    return returnAnchorData;

 }


extern "C" void session_SetEnvironmentProbeAnchorCallbacks(const void* session, UNITY_AR_ENVPROBE_ANCHOR_CALLBACK envProbeAnchorAddedCallback,
                                                UNITY_AR_ENVPROBE_ANCHOR_CALLBACK envProbeAnchorUpdatedCallback,
                                                UNITY_AR_ENVPROBE_ANCHOR_CALLBACK envProbeAnchorRemovedCallback)
{
    if (UnityIsARKit_2_0_Supported())
    {
        UnityARSession* nativeSession = (__bridge UnityARSession*)session;
        UnityAREnvironmentProbeAnchorCallbackWrapper* envProbeAnchorCallbacks = [[UnityAREnvironmentProbeAnchorCallbackWrapper alloc] init];
        envProbeAnchorCallbacks->_anchorAddedCallback = envProbeAnchorAddedCallback;
        envProbeAnchorCallbacks->_anchorUpdatedCallback = envProbeAnchorUpdatedCallback;
        envProbeAnchorCallbacks->_anchorRemovedCallback = envProbeAnchorRemovedCallback;
        if (@available(iOS 12.0, *)) {
            [nativeSession->_classToCallbackMap setObject:envProbeAnchorCallbacks forKey:[AREnvironmentProbeAnchor class]];
        } 
    }
}


