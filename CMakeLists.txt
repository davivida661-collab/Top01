cmake_minimum_required(VERSION 3.20)
project(Syntax VERSION 1.1.0 LANGUAGES C CXX)

if(WIN32)
  enable_language(ASM_MASM)
endif()

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_C_STANDARD 11)

include(FetchContent)

FetchContent_Declare(zlib GIT_REPOSITORY https://github.com/madler/zlib.git GIT_TAG v1.3.1)
set(ZLIB_BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(zlib)

FetchContent_Declare(zstd GIT_REPOSITORY https://github.com/facebook/zstd.git GIT_TAG v1.5.7 SOURCE_SUBDIR build/cmake)
set(ZSTD_BUILD_PROGRAMS OFF CACHE BOOL "" FORCE)
set(ZSTD_BUILD_TESTS OFF CACHE BOOL "" FORCE)
set(ZSTD_BUILD_SHARED OFF CACHE BOOL "" FORCE)
set(ZSTD_BUILD_STATIC ON CACHE BOOL "" FORCE)
set(ZSTD_MULTITHREAD_SUPPORT OFF CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(zstd)

FetchContent_Declare(blake3 GIT_REPOSITORY https://github.com/BLAKE3-team/BLAKE3.git GIT_TAG 1.8.2)
FetchContent_MakeAvailable(blake3)
add_library(blake3 STATIC ${blake3_SOURCE_DIR}/c/blake3.c ${blake3_SOURCE_DIR}/c/blake3_dispatch.c ${blake3_SOURCE_DIR}/c/blake3_portable.c)
target_include_directories(blake3 PUBLIC ${blake3_SOURCE_DIR}/c)
target_compile_definitions(blake3 PUBLIC BLAKE3_NO_AVX2 BLAKE3_NO_AVX512 BLAKE3_NO_SSE41 BLAKE3_NO_SSE2)

FetchContent_Declare(httplib GIT_REPOSITORY https://github.com/yhirose/cpp-httplib.git GIT_TAG v0.20.0)
set(HTTPLIB_COMPILE OFF CACHE BOOL "" FORCE)
set(HTTPLIB_USE_OPENSSL_IF_AVAILABLE OFF CACHE BOOL "" FORCE)
set(HTTPLIB_REQUIRE_OPENSSL OFF CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(httplib)

FetchContent_Declare(json GIT_REPOSITORY https://github.com/nlohmann/json.git GIT_TAG v3.12.0)
set(JSON_BuildTests OFF CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(json)

set(LUAU_BUILD_CLI OFF CACHE BOOL "" FORCE)
set(LUAU_BUILD_TESTS OFF CACHE BOOL "" FORCE)
set(LUAU_BUILD_WEB OFF CACHE BOOL "" FORCE)
FetchContent_Declare(luau GIT_REPOSITORY https://github.com/luau-lang/luau.git GIT_TAG 0.702)
FetchContent_MakeAvailable(luau)

# ---- Sources ----
file(GLOB_RECURSE CORE_CPP CONFIGURE_DEPENDS
  "src/process/*.cpp"
  "src/memory/*.cpp"
  "src/execution/*.cpp"
  "src/exports.cpp"
  "src/syscalls/syscalls.cpp"
)

# On Windows the real syscalls are in .asm — remove the Linux stub cpp
if(WIN32)
  list(FILTER CORE_CPP EXCLUDE REGEX ".*syscalls\\.cpp$")
  file(GLOB ASM_SOURCES "${CMAKE_CURRENT_SOURCE_DIR}/src/syscalls/*.asm")
  set(CORE_SOURCES ${CORE_CPP} ${ASM_SOURCES})
  set_source_files_properties(${ASM_SOURCES} PROPERTIES LANGUAGE ASM_MASM)
  # Prevent C++ flags from leaking into MASM
  set(CMAKE_ASM_MASM_FLAGS "")
else()
  set(CORE_SOURCES ${CORE_CPP})
endif()

add_library(Syntax SHARED ${CORE_SOURCES})
target_compile_definitions(Syntax PRIVATE RBLX_EXPORT WIN32_LEAN_AND_MEAN NOMINMAX UNICODE _UNICODE)
set_target_properties(Syntax PROPERTIES OUTPUT_NAME "Syntax")

# Only C/C++ get these flags — not MASM
target_compile_options(Syntax PRIVATE
  $<$<COMPILE_LANGUAGE:CXX>:/W4>
  $<$<COMPILE_LANGUAGE:C>:/W4>
  $<$<COMPILE_LANGUAGE:CXX>:/permissive->
  $<$<COMPILE_LANGUAGE:CXX>:/Zc:__cplusplus>
)

if(NOT WIN32)
  target_compile_options(Syntax PRIVATE $<$<COMPILE_LANGUAGE:CXX>:-Wall> $<$<COMPILE_LANGUAGE:CXX>:-Wextra> $<$<COMPILE_LANGUAGE:C>:-Wall>)
endif()

target_include_directories(Syntax PRIVATE src include ${luau_SOURCE_DIR}/Compiler/include ${luau_SOURCE_DIR}/Ast/include ${luau_SOURCE_DIR}/Common/include ${zlib_SOURCE_DIR} ${zlib_BINARY_DIR} ${zstd_SOURCE_DIR}/lib ${zstd_SOURCE_DIR}/lib/common ${blake3_SOURCE_DIR}/c)

if(WIN32)
  target_link_libraries(Syntax PRIVATE kernel32 user32 winhttp ws2_32 Luau.Compiler Luau.Ast zlibstatic libzstd_static blake3 httplib::httplib nlohmann_json::nlohmann_json)
else()
  target_link_libraries(Syntax PRIVATE Luau.Compiler Luau.Ast zlibstatic libzstd_static blake3 httplib::httplib nlohmann_json::nlohmann_json)
  set_target_properties(Syntax PROPERTIES EXCLUDE_FROM_ALL TRUE)
endif()
