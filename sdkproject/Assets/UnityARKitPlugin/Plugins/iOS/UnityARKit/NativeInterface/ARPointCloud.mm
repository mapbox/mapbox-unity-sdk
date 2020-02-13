// Unity Technologies Inc (c) 2018
// ARPointCloud.mm
// Main implementation of ARKit plugin's ARPointCloud

#include "ARKitDefines.h"

#ifdef __cplusplus
extern "C" {
#endif
    
int pointCloud_GetCount(const void* pointCloudPtr)
{
    if (pointCloudPtr == nullptr)
        return 0;
    
    ARPointCloud* pointCloud = (__bridge ARPointCloud*)pointCloudPtr;
    
    return [pointCloud count];
}

void* pointCloud_GetPointsPtr(const void* pointCloudPtr)
{
    if (pointCloudPtr == nullptr)
    {
        return 0;
    }
    
    if (@available(iOS 11.0, *))
    {
        ARPointCloud* pointCloud = (__bridge ARPointCloud*)pointCloudPtr;
        if (![pointCloud isKindOfClass:[ARPointCloud class]])
        {
            return 0;
        }
        const vector_float3 *pointsPtr = [pointCloud points];
        return (void*) pointsPtr;
    }
    else
    {
        // Fallback on earlier versions
        return 0;
    }
}

void* pointCloud_GetIdentifiersPtr(const void* pointCloudPtr)
{
    if (pointCloudPtr == nullptr)
        return 0;
    
    ARPointCloud* pointCloud = (__bridge ARPointCloud*)pointCloudPtr;
    
    const UInt64 *identifiersPtr = [pointCloud identifiers];
    
    return (void*) identifiersPtr;
}
#ifdef __cplusplus
}
#endif

