// Unity Technologies Inc (c) 2017
// ARKitDefines.h

#import <ARKit/ARKit.h>


typedef struct
{
    float x,y,z,w;
} UnityARVector4;

typedef struct
{
    float x,y,z;
} UnityARVector3;

typedef struct
{
    UnityARVector4 column0;
    UnityARVector4 column1;
    UnityARVector4 column2;
    UnityARVector4 column3;
} UnityARMatrix4x4;

enum UnityARAlignment
{
    UnityARAlignmentGravity,
    UnityARAlignmentGravityAndHeading,
    UnityARAlignmentCamera
};

enum UnityARPlaneDetection
{
    UnityARPlaneDetectionNone = 0,
    UnityARPlaneDetectionHorizontal = (1 << 0),
    UnityARPlaneDetectionVertical = (1 << 1)
};

enum UnityAREnvironmentTexturing
{
    UnityAREnvironmentTexturingNone,
    UnityAREnvironmentTexturingManual,
    UnityAREnvironmentTexturingAutomatic
};

typedef enum
{
    UnityARWorldMappingStatusNotAvailable,
    UnityARWorldMappingStatusLimited,
    UnityARWorldMappingStatusExtending,
    UnityARWorldMappingStatusMapped
}UnityARWorldMappingStatus;

typedef enum
{
    kUnityARKitSupportedFeaturesNone = 0,
    kUnityARKitSupportedFeaturesWorldMap = 1 << 0,
    kUnityARKitSupportedFeaturesReferenceObject = 1 << 1,
} UnityARKitSupportedFeatures;

typedef struct
{
    UnityARAlignment alignment;
    uint32_t getPointCloudData;
    uint32_t enableLightEstimation;
    
} ARKitSessionConfiguration;

typedef struct
{
    UnityARAlignment alignment;
    UnityARPlaneDetection planeDetection;
    uint32_t getPointCloudData;
    uint32_t enableLightEstimation;
    uint32_t enableAutoFocus;
    UnityAREnvironmentTexturing environmentTexturing;
    int maximumNumberOfTrackedImages;
    void *ptrVideoFormat;
    char *referenceImagesResourceGroup;
    char *referenceObjectsResourceGroup;
    void *ptrDynamicReferenceObjects;
    void *ptrWorldMap;
} ARKitWorldTrackingSessionConfiguration;

typedef struct
{
    UnityARAlignment alignment;
    uint32_t enableLightEstimation;
    void *ptrVideoFormat;
} ARKitFaceTrackingConfiguration;

enum UnityARSessionRunOptions
{
    UnityARSessionRunOptionsNone = 0,
    UnityARSessionRunOptionResetTracking           = (1 << 0),
    UnityARSessionRunOptionRemoveExistingAnchors   = (1 << 1)

};

typedef struct
{
    NSUInteger vertexCount;
    float *vertices;
    NSUInteger textureCoordinateCount;
    float *textureCoordinates;
    NSUInteger triangleCount;
    int *triangleIndices;
    NSUInteger boundaryVertexCount;
    float *boundaryVertices;
} UnityARPlaneGeometry;


typedef struct
{
    void* identifier;
    UnityARMatrix4x4 transform;
    ARPlaneAnchorAlignment alignment;
    UnityARVector4 center;
    UnityARVector4 extent;
    UnityARPlaneGeometry planeGeometry;
} UnityARAnchorData;

typedef struct
{
    void* identifier;
    UnityARMatrix4x4 transform;
} UnityARUserAnchorData;

typedef struct
{
    NSUInteger vertexCount;
    float *vertices;
    NSUInteger textureCoordinateCount;
    float *textureCoordinates;
    NSUInteger triangleCount;
    int *triangleIndices;
} UnityARFaceGeometry;

typedef struct
{
    void *identifier;
    UnityARMatrix4x4 transform;
    UnityARFaceGeometry faceGeometry;
    void *blendShapes;  //NSDictionary<ARBlendShapeLocation, NSNumber*> *
    UnityARMatrix4x4 leftEyeTransform;
    UnityARMatrix4x4 rightEyeTransform;
    UnityARVector3 lookAtPoint;
    uint32_t isTracked;
} UnityARFaceAnchorData;

typedef struct
{
    void* identifier;
    UnityARMatrix4x4 transform;
    void* referenceImageName;
    float referenceImageSize;
    int isTracked;
} UnityARImageAnchorData;



enum UnityARTrackingState
{
    UnityARTrackingStateNotAvailable,
    UnityARTrackingStateLimited,
    UnityARTrackingStateNormal,
};

enum UnityARTrackingReason
{
    UnityARTrackingStateReasonNone,
    UnityARTrackingStateReasonInitializing,
    UnityARTrackingStateReasonExcessiveMotion,
    UnityARTrackingStateReasonInsufficientFeatures,
    UnityARTrackingStateReasonRelocalizing,
};

typedef struct
{
    uint32_t yWidth;
    uint32_t yHeight;
    uint32_t screenOrientation;
    float texCoordScale;
    void* cvPixelBufferPtr;
}UnityVideoParams;

typedef struct
{
    float ambientIntensity;
    float ambientColorTemperature;
}UnityARLightEstimation;

typedef struct
{
    UnityARVector4 primaryLightDirectionAndIntensity;
    float *sphericalHarmonicsCoefficients;
}UnityARDirectionalLightEstimate;

enum UnityLightDataType
{
    LightEstimate,
    DirectionalLightEstimate
};

typedef struct
{
    UnityLightDataType arLightingType;
    UnityARLightEstimation arLightEstimate;
    UnityARDirectionalLightEstimate arDirectionalLightEstimate;
}UnityLightData;

typedef struct
{
    UnityARMatrix4x4 worldTransform;
    UnityARMatrix4x4 projectionMatrix;
    UnityARTrackingState trackingState;
    UnityARTrackingReason trackingReason;
    UnityVideoParams videoParams;
    UnityLightData lightData;
    UnityARMatrix4x4 displayTransform;
    void* ptrPointCloud;
    uint32_t getLightEstimation;
    UnityARWorldMappingStatus worldMappingStatus;
} UnityARCamera;

typedef struct
{
    vector_float3* pointCloud;
    NSUInteger pointCloudSize;
} UnityARPointCloudData;

typedef struct
{
    void* pYPixelBytes;
    void* pUVPixelBytes;
    BOOL bEnable;
}UnityPixelBuffer;

typedef struct
{
    void* ptrVideoFormat;
    float imageResolutionWidth;
    float imageResolutionHeight;
    int framesPerSecond;
}UnityARVideoFormat;

typedef void (*UNITY_AR_FRAME_CALLBACK)(UnityARCamera camera);
typedef void (*UNITY_AR_ANCHOR_CALLBACK)(UnityARAnchorData anchorData);
typedef void (*UNITY_AR_USER_ANCHOR_CALLBACK)(UnityARUserAnchorData anchorData);
typedef void (*UNITY_AR_FACE_ANCHOR_CALLBACK)(UnityARFaceAnchorData anchorData);
typedef void (*UNITY_AR_IMAGE_ANCHOR_CALLBACK)(UnityARImageAnchorData anchorData);
typedef void (*UNITY_AR_SESSION_FAILED_CALLBACK)(const void* error);
typedef void (*UNITY_AR_SESSION_VOID_CALLBACK)(void);
typedef bool (*UNITY_AR_SESSION_RELOCALIZE_CALLBACK)(void);
typedef void (*UNITY_AR_SESSION_TRACKING_CHANGED)(UnityARCamera camera);
typedef void (*UNITY_AR_VIDEOFORMAT_CALLBACK)(UnityARVideoFormat format);
typedef void (*UNITY_AR_SESSION_WORLD_MAP_COMPLETION_CALLBACK)(const void*, void*);
typedef void (*UNITY_AR_SESSION_REF_OBJ_EXTRACT_COMPLETION_CALLBACK)(const void*, void*);

@protocol UnityARAnchorEventDispatcher
@required
    -(void)sendAnchorAddedEvent:(ARAnchor*)anchor;
    -(void)sendAnchorRemovedEvent:(ARAnchor*)anchor;
    -(void)sendAnchorUpdatedEvent:(ARAnchor*)anchor;
@end

@interface UnityARSession : NSObject <ARSessionDelegate>
{
@public
    ARSession* _session;
    UNITY_AR_FRAME_CALLBACK _frameCallback;
    UNITY_AR_SESSION_FAILED_CALLBACK _arSessionFailedCallback;
    UNITY_AR_SESSION_VOID_CALLBACK _arSessionInterrupted;
    UNITY_AR_SESSION_VOID_CALLBACK _arSessionInterruptionEnded;
    UNITY_AR_SESSION_RELOCALIZE_CALLBACK _arSessionShouldRelocalize;
    UNITY_AR_SESSION_TRACKING_CHANGED _arSessionTrackingChanged;
    UNITY_AR_SESSION_WORLD_MAP_COMPLETION_CALLBACK _arSessionWorldMapCompletionHandler;
    UNITY_AR_SESSION_REF_OBJ_EXTRACT_COMPLETION_CALLBACK _arSessionRefObjExtractCompletionHandler;

    NSMutableDictionary* _classToCallbackMap;
    
    id <MTLDevice> _device;
    CVMetalTextureCacheRef _textureCache;
    BOOL _getPointCloudData;
    BOOL _getLightEstimation;
}
- (void) setupMetal;
@end


static inline bool UnityIsARKit_1_5_Supported()
{
    if (@available(iOS 11.3, *))
    {
        return [ARImageAnchor class];
    }
    else
    {
        return  false;
    }
}

static inline bool UnityAreFeaturesSupported(int features)
{
    bool featuresSupported = true;
    if (features & kUnityARKitSupportedFeaturesWorldMap)
    {
        if (@available(iOS 12.0, *))
        {
            featuresSupported &= (bool)[ARWorldMap class];
        }
        else
        {
            featuresSupported = false;
        }
    }
    if (features & kUnityARKitSupportedFeaturesReferenceObject)
    {
        if (@available(iOS 12.0, *))
        {
            featuresSupported &= (bool)[ARReferenceObject class];
        }
        else
        {
            featuresSupported = false;
        }
    }
    
    return featuresSupported;
}

static inline bool UnityIsARKit_2_0_Supported()
{
    if (@available(iOS 12.0, *))
    {
        if ([ARReferenceObject class])
        {
            return true;
        }
        else
        {
            return  false;
        }
    }
    else
    {
        return  false;
    }
}

inline void ARKitMatrixToUnityARMatrix4x4(const matrix_float4x4& matrixIn, UnityARMatrix4x4* matrixOut)
{
    vector_float4 c0 = matrixIn.columns[0];
    matrixOut->column0.x = c0.x;
    matrixOut->column0.y = c0.y;
    matrixOut->column0.z = c0.z;
    matrixOut->column0.w = c0.w;

    vector_float4 c1 = matrixIn.columns[1];
    matrixOut->column1.x = c1.x;
    matrixOut->column1.y = c1.y;
    matrixOut->column1.z = c1.z;
    matrixOut->column1.w = c1.w;

    vector_float4 c2 = matrixIn.columns[2];
    matrixOut->column2.x = c2.x;
    matrixOut->column2.y = c2.y;
    matrixOut->column2.z = c2.z;
    matrixOut->column2.w = c2.w;

    vector_float4 c3 = matrixIn.columns[3];
    matrixOut->column3.x = c3.x;
    matrixOut->column3.y = c3.y;
    matrixOut->column3.z = c3.z;
    matrixOut->column3.w = c3.w;
}

inline void UnityARMatrix4x4ToARKitMatrix(const UnityARMatrix4x4& matrixIn, matrix_float4x4* matrixOut)
{
    matrixOut->columns[0].x = matrixIn.column0.x;
    matrixOut->columns[0].y = matrixIn.column0.y;
    matrixOut->columns[0].z = matrixIn.column0.z;
    matrixOut->columns[0].w = matrixIn.column0.w;
    
    matrixOut->columns[1].x = matrixIn.column1.x;
    matrixOut->columns[1].y = matrixIn.column1.y;
    matrixOut->columns[1].z = matrixIn.column1.z;
    matrixOut->columns[1].w = matrixIn.column1.w;
    
    matrixOut->columns[2].x = matrixIn.column2.x;
    matrixOut->columns[2].y = matrixIn.column2.y;
    matrixOut->columns[2].z = matrixIn.column2.z;
    matrixOut->columns[2].w = matrixIn.column2.w;
    
    matrixOut->columns[3].x = matrixIn.column3.x;
    matrixOut->columns[3].y = matrixIn.column3.y;
    matrixOut->columns[3].z = matrixIn.column3.z;
    matrixOut->columns[3].w = matrixIn.column3.w;
}

inline ARSessionRunOptions GetARSessionRunOptionsFromUnityARSessionRunOptions(UnityARSessionRunOptions runOptions)
{
    ARSessionRunOptions ret = 0;
    if ((runOptions & UnityARSessionRunOptionResetTracking) != 0)
        ret |= ARSessionRunOptionResetTracking;
    if ((runOptions & UnityARSessionRunOptionRemoveExistingAnchors) != 0)
        ret |= ARSessionRunOptionRemoveExistingAnchors;
    return ret;
}

static inline ARWorldAlignment GetARWorldAlignmentFromUnityARAlignment(UnityARAlignment& unityAlignment)
{
    switch (unityAlignment)
    {
        case UnityARAlignmentGravity:
            return ARWorldAlignmentGravity;
        case UnityARAlignmentGravityAndHeading:
            return ARWorldAlignmentGravityAndHeading;
        case UnityARAlignmentCamera:
            return ARWorldAlignmentCamera;
    }
}

static inline ARPlaneDetection GetARPlaneDetectionFromUnityARPlaneDetection(UnityARPlaneDetection planeDetection)
{
    ARPlaneDetection ret = ARPlaneDetectionNone;
    if ((planeDetection & UnityARPlaneDetectionNone) != 0)
        ret |= ARPlaneDetectionNone;
    if ((planeDetection & UnityARPlaneDetectionHorizontal) != 0)
        ret |= ARPlaneDetectionHorizontal;
    if ((planeDetection & UnityARPlaneDetectionVertical) != 0)
    {
        if (@available(iOS 11.3, *)) {
            ret |= ARPlaneDetectionVertical;
        } 
    }
    return ret;
}
