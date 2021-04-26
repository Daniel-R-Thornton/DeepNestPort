# CMakeList.txt : CMake project for Minkowski, include source and define
# project specific logic here.
#
cmake_minimum_required (VERSION 3.8)

FIND_PACKAGE(Boost COMPONENTS REQUIRED)

if(Boost_FOUND)
include_directories(${Boost_INCLUDE_DIR})
ADD_DEFINITIONS( "-DHAS_BOOST" )
link_directories(${Boost_LIBRARY_DIR})
add_library (minkowski SHARED "minkowski.cc" "minkowski.h")
TARGET_LINK_LIBRARIES(minkowski LINK_PUBLIC ${Boost_LIBRARIES} )
endif()