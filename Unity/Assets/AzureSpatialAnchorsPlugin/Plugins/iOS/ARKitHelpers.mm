//
//  SpatialServicesExtensions.mm
//  Unity-iPhone
//
//  Created by Patrick Cook on 12/3/18.
//

#import <Foundation/Foundation.h>
#import <ARKit/ARKit.h>

typedef struct
{
    float x,y,z,w;
} UnityARVector4;

typedef struct
{
    UnityARVector4 column0;
    UnityARVector4 column1;
    UnityARVector4 column2;
    UnityARVector4 column3;
} UnityARMatrix4x4;

inline UnityARMatrix4x4 ARKitMatrixToUnityARMatrix4x4(const matrix_float4x4& matrixIn)
{
    UnityARMatrix4x4 matrixOut;
    vector_float4 c0 = matrixIn.columns[0];
    matrixOut.column0.x = c0.x;
    matrixOut.column0.y = c0.y;
    matrixOut.column0.z = c0.z;
    matrixOut.column0.w = c0.w;

    vector_float4 c1 = matrixIn.columns[1];
    matrixOut.column1.x = c1.x;
    matrixOut.column1.y = c1.y;
    matrixOut.column1.z = c1.z;
    matrixOut.column1.w = c1.w;

    vector_float4 c2 = matrixIn.columns[2];
    matrixOut.column2.x = c2.x;
    matrixOut.column2.y = c2.y;
    matrixOut.column2.z = c2.z;
    matrixOut.column2.w = c2.w;

    vector_float4 c3 = matrixIn.columns[3];
    matrixOut.column3.x = c3.x;
    matrixOut.column3.y = c3.y;
    matrixOut.column3.z = c3.z;
    matrixOut.column3.w = c3.w;

    return matrixOut;
}

extern "C" void* SessionGetArAnchorPointerForId(void* nativeSession, const char * anchorIdentifier)
{
    // go through anchors and find the right one
    ARSession* session = (__bridge ARSession*)nativeSession;
    for (ARAnchor* a in session.currentFrame.anchors)
    {
        if ([[a.identifier UUIDString] isEqualToString:[NSString stringWithUTF8String:anchorIdentifier]])
        {
            return (__bridge void*) a;
        }
    }

    return nullptr;
}

extern "C" UnityARMatrix4x4 SessionGetUnityTransformFromAnchorPtr(void* nativeAnchor)
{
    ARAnchor* anchor = (__bridge ARAnchor*)nativeAnchor;
    return ARKitMatrixToUnityARMatrix4x4(anchor.transform);
}
