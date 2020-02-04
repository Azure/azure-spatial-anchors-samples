//
//  SpatialServicesExtensions.mm
//  Unity-iPhone
//
//  Created by Patrick Cook on 12/3/18.
//

#import <Foundation/Foundation.h>
#import <ARKit/ARKit.h>

extern "C" void* GetArkitAnchorId(void* nativeAnchor)
{
    ARAnchor* anchor = (__bridge ARAnchor*)nativeAnchor;
    return (void*)[anchor.identifier.UUIDString UTF8String];
}
