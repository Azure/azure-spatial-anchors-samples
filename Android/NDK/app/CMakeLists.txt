# For more information about using CMake with Android Studio, read the
# documentation: https://d.android.com/studio/projects/add-native-code.html

# Sets the minimum version of CMake required to build the native library.

cmake_minimum_required(VERSION 3.4.1)

# Import the ARCore library.
add_library(arcore SHARED IMPORTED)
set_target_properties(arcore PROPERTIES IMPORTED_LOCATION
                      ${NATIVE_LIBPATH}/${ANDROID_ABI}/libarcore_sdk_c.so
                      INTERFACE_INCLUDE_DIRECTORIES ${NATIVE_INCLUDE}
)

# Import the SpatialServices library.
add_library(azurespatialanchorsndk SHARED IMPORTED)
set_target_properties(azurespatialanchorsndk PROPERTIES IMPORTED_LOCATION
                      ${NATIVE_LIBPATH}/${ANDROID_ABI}/libazurespatialanchorsndk.so
                      INTERFACE_INCLUDE_DIRECTORIES ${NATIVE_INCLUDE}
)

# Import the glm header file from the NDK.
add_library( glm INTERFACE )
set_target_properties( glm PROPERTIES
                       INTERFACE_INCLUDE_DIRECTORIES ${CMAKE_SOURCE_DIR}/../libraries/glm/glm
)

# This is the main app library.
add_library(azurespatialanchorsapplication SHARED
        src/main/cpp/CameraRenderer.cpp
        src/main/cpp/AzureSpatialAnchorsApplication.cpp
        src/main/cpp/JniInterface.cpp
        src/main/cpp/CubeRenderer.cpp
        src/main/cpp/Util.cpp)

target_include_directories(azurespatialanchorsapplication PRIVATE
           src/main/cpp)
target_link_libraries(azurespatialanchorsapplication
                      android
                      log
                      GLESv2
                      glm
                      arcore
                      azurespatialanchorsndk)
