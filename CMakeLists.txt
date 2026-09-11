cmake_minimum_required(VERSION 3.20)
project(Syntax VERSION 1.1.0 LANGUAGES C CXX ASM)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_C_STANDARD 11)

# Warnings & optimization
if(MSVC)
    add_compile_options(/W4 /permissive- /Zc:__cplusplus)
    foreach(flag CMAKE_CXX_FLAGS CMAKE_CXX_FLAGS_DEBUG CMAKE_CXX_FLAGS_RELEASE CMAKE_C_FLAGS CMAKE_C_FLAGS_DEBUG CMAKE_C_FLAGS_RELEASE)
        string(REPLACE "/MD" "/MT" ${flag} "${${flag}}")
    endforeach()
else()
    add_compile_options(-Wall -Wextra -Wpedantic -Wno-unused-parameter)
endif()

include(FetchContent)

# ---- Zlib ----
FetchContent_Declare(zlib GIT_REPOSITORY https://github.com/madler/zlib.git GIT_TAG v1.3.1)
set(ZLIB_BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(zlib)

# ---- ZSTD ----
FetchContent_Declare(zstd GIT_REPOSITORY https://github.com/facebook/zstd.git GIT_TAG v1.5.7 SOURCE_SUBDIR build/cmake)
set(ZSTD_BUILD_PROGRAMS OFF CACHE BOOL "" FORCE)
set(ZSTD_BUILD_TESTS OFF CACHE BOOL "" FORCE)
set(ZSTD_BUILD_SHARED OFF CACHE BOOL "" FORCE)
set(ZSTD_BUILD_STATIC ON CACHE BOOL "" FORCE)
set(ZSTD_MULTITHREAD_SUPPORT OFF CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(zstd)

# ---- BLAKE3 ----
FetchContent_Declare(blake3 GIT_REPOSITORY https://github.com/BLAKE3-team/BLAKE3.git GIT_TAG 1.8.2 SOURCE_SUBDIR c)
FetchContent_MakeAvailable(blake3)

# ---- httplib ----
FetchContent_Declare(httplib GIT_REPOSITORY https://github.com/yhirose/cpp-httplib.git GIT_TAG v0.20.0)
set(HTTPLIB_COMPILE OFF CACHE BOOL "" FORCE)
set(HTTPLIB_USE_OPENSSL_IF_AVAILABLE OFF CACHE BOOL "" FORCE)
set(HTTPLIB_REQUIRE_OPENSSL OFF CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(httplib)

# ---- nlohmann/json ----
FetchContent_Declare(json GIT_REPOSITORY https://github.com/nlohmann/json.git GIT_TAG v3.12.0)
set(JSON_BuildTests OFF CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(json)

# ---- Luau ----
set(LUAU_BUILD_CLI OFF CACHE BOOL "" FORCE)
set(LUAU_BUILD_TESTS OFF CACHE BOOL "" FORCE)
set(LUAU_BUILD_WEB OFF CACHE BOOL "" FORCE)
if(EXISTS "${CMAKE_CURRENT_SOURCE_DIR}/third_party/luau/CMakeLists.txt")
    add_subdirectory(third_party/luau EXCLUDE_FROM_ALL)
else()
    FetchContent_Declare(luau GIT_REPOSITORY https://github.com/luau-lang/luau.git GIT_TAG 0.702)
    FetchContent_MakeAvailable(luau)
endif()

file(GLOB_RECURSE CORE_SOURCES CONFIGURE_DEPENDS
    "src/syscalls/*.cpp"
    "src/process/*.cpp"
    "src/memory/*.cpp"
    "src/execution/*.cpp"
    "src/exports.cpp"
)

if(WIN32)
    file(GLOB ASM_SOURCES "src/syscalls/*.asm")
    list(APPEND CORE_SOURCES ${ASM_SOURCES})
    set_source_files_properties(${ASM_SOURCES} PROPERTIES LANGUAGE ASM_MASM)
    enable_language(ASM_MASM)
endif()

add_library(Syntax SHARED ${CORE_SOURCES})
target_compile_definitions(Syntax PRIVATE RBLX_EXPORT WIN32_LEAN_AND_MEAN NOMINMAX UNICODE _UNICODE)
set_target_properties(Syntax PROPERTIES LINKER_LANGUAGE CXX OUTPUT_NAME "Syntax")

target_include_directories(Syntax PRIVATE
    src include
    ${CMAKE_CURRENT_SOURCE_DIR}/third_party/luau/Compiler/include
    ${CMAKE_CURRENT_SOURCE_DIR}/third_party/luau/Ast/include
    ${CMAKE_CURRENT_SOURCE_DIR}/third_party/luau/Common/include
    ${zlib_SOURCE_DIR} ${zlib_BINARY_DIR}
    ${zstd_SOURCE_DIR}/lib ${zstd_SOURCE_DIR}/lib/common
    ${blake3_SOURCE_DIR}/c
)

if(WIN32)
    target_link_libraries(Syntax PRIVATE kernel32 user32 winhttp ws2_32 Luau.Compiler Luau.Ast zlibstatic libzstd_static blake3 httplib::httplib nlohmann_json::nlohmann_json)
else()
    target_link_libraries(Syntax PRIVATE Luau.Compiler Luau.Ast zlibstatic libzstd_static blake3 httplib::httplib nlohmann_json::nlohmann_json)
    set_target_properties(Syntax PROPERTIES EXCLUDE_FROM_ALL TRUE)
endif()
