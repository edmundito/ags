//=============================================================================
//
// Adventure Game Studio (AGS)
//
// Copyright (C) 1999-2011 Chris Jones and 2011-2025 various contributors
// The full list of copyright holders can be found in the Copyright.txt
// file, which is part of this source code distribution.
//
// The AGS source code is provided under the Artistic License 2.0.
// A copy of this license can be found in the file License.txt and at
// https://opensource.org/license/artistic-2-0/
//
//=============================================================================

#import <Metal/Metal.h>
#import <MetalKit/MetalKit.h>
#include "gfx/ali3dmtl.h"
#include <algorithm>
#include <stack>
#include "ac/sys_events.h"
#include "ac/timer.h"
#include "debug/out.h"
#include "gfx/ali3dexception.h"
#include "gfx/gfx_def.h"
#include "platform/base/agsplatformdriver.h"
#include "platform/base/sys_main.h"
#include "util/matrix.h"

// Necessary to update textures from 8-bit bitmaps
extern RGB palette[256];

namespace AGS
{
namespace Engine
{
namespace MTL
{

using namespace AGS::Common;

MTLTexture::~MTLTexture()
{
    if (_texture) {
        [_texture release];
    }
}

size_t MTLTexture::GetMemSize() const
{
    return _width * _height * 4; // Assuming RGBA8Unorm format
}

MTLBitmap::~MTLBitmap()
{
    if (_renderTarget) {
        [_renderTarget release];
    }
}

MetalGraphicsDriver::MetalGraphicsDriver()
    : _device(nil)
    , _commandQueue(nil)
    , _metalView(nil)
    , _pipelineState(nil)
    , _firstTimeInit(false)
{
}

MetalGraphicsDriver::~MetalGraphicsDriver()
{
    if (_pipelineState) {
        [_pipelineState release];
    }
    if (_commandQueue) {
        [_commandQueue release];
    }
    if (_device) {
        [_device release];
    }
}

bool MetalGraphicsDriver::FirstTimeInit()
{
    // Get default Metal device
    _device = MTLCreateSystemDefaultDevice();
    if (!_device) {
        Debug::Printf(kDbgMsg_Error, "Failed to create Metal device");
        return false;
    }
    
    // Create command queue
    _commandQueue = [_device newCommandQueue];
    if (!_commandQueue) {
        Debug::Printf(kDbgMsg_Error, "Failed to create Metal command queue");
        return false;
    }
    
    CreateDefaultRenderPipeline();
    CreateShaders();
    
    _firstTimeInit = true;
    return true;
}

bool MetalGraphicsDriver::CreateWindowAndMetalContext(const DisplayMode &mode)
{
    // First setup SDL attributes for Metal
    if (SDL_SetHint(SDL_HINT_RENDER_DRIVER, "metal") != SDL_TRUE) {
        Debug::Printf(kDbgMsg_Warn, "Failed to set SDL render driver to Metal: %s", SDL_GetError());
    }

    // Create SDL window with Metal support
    SDL_Window *sdl_window = sys_window_create("", mode.DisplayIndex, mode.Width, mode.Height, mode.Mode, SDL_WINDOW_METAL);
    if (!sdl_window) {
        Debug::Printf(kDbgMsg_Error, "Failed to create SDL window for Metal: %s", SDL_GetError());
        return false;
    }

    // Get the Metal device from the window
    SDL_MetalView metalView = SDL_Metal_CreateView(sdl_window);
    if (!metalView) {
        Debug::Printf(kDbgMsg_Error, "Failed to create Metal view: %s", SDL_GetError());
        sys_window_destroy();
        return false;
    }

    // Get the Metal device and command queue
    _device = SDL_Metal_GetDevice(metalView);
    if (!_device) {
        Debug::Printf(kDbgMsg_Error, "Failed to get Metal device");
        SDL_Metal_DestroyView(metalView);
        sys_window_destroy();
        return false;
    }

    // Create command queue
    _commandQueue = [_device newCommandQueue];
    if (!_commandQueue) {
        Debug::Printf(kDbgMsg_Error, "Failed to create Metal command queue");
        SDL_Metal_DestroyView(metalView);
        sys_window_destroy();
        return false;
    }

    // Store the Metal view
    _metalView = metalView;

    // Create default render pipeline
    CreateDefaultRenderPipeline();

    // Log Metal device information
    const char *device_name = [[_device name] UTF8String];
    const char *device_vendor = [[_device vendorName] UTF8String];
    String adapter_info = String::FromFormat(
        "\tMetal: %s\n\tVendor: %s",
        device_name, device_vendor
    );
    Debug::Printf(kDbgMsg_Info, "Metal adapter info:\n%s", adapter_info.GetCStr());

    return true;
}

void MetalGraphicsDriver::DeleteWindowAndMetalContext()
{
    if (_metalView) {
        SDL_Metal_DestroyView(_metalView);
        _metalView = nullptr;
    }
    
    if (_commandQueue) {
        [_commandQueue release];
        _commandQueue = nil;
    }
    
    if (_device) {
        [_device release];
        _device = nil;
    }
    
    sys_window_destroy();
}

void MetalGraphicsDriver::CreateDefaultRenderPipeline()
{
    // Create render pipeline descriptor
    MTLRenderPipelineDescriptor *pipelineDescriptor = [[MTLRenderPipelineDescriptor alloc] init];
    
    // Configure pipeline settings
    pipelineDescriptor.colorAttachments[0].pixelFormat = MTLPixelFormatBGRA8Unorm;
    pipelineDescriptor.colorAttachments[0].blendingEnabled = YES;
    pipelineDescriptor.colorAttachments[0].sourceRGBBlendFactor = MTLBlendFactorSourceAlpha;
    pipelineDescriptor.colorAttachments[0].destinationRGBBlendFactor = MTLBlendFactorOneMinusSourceAlpha;
    pipelineDescriptor.colorAttachments[0].sourceAlphaBlendFactor = MTLBlendFactorSourceAlpha;
    pipelineDescriptor.colorAttachments[0].destinationAlphaBlendFactor = MTLBlendFactorOneMinusSourceAlpha;
    
    // Load shader library
    NSError *error = nil;
    id<MTLLibrary> defaultLibrary = [_device newLibraryWithFile:@"Shaders.metallib" error:&error];
    if (!defaultLibrary) {
        Debug::Printf(kDbgMsg_Error, "Failed to load Metal shader library: %s", error.localizedDescription.UTF8String);
        return;
    }
    
    // Set vertex and fragment functions
    pipelineDescriptor.vertexFunction = [defaultLibrary newFunctionWithName:@"vertexShader"];
    pipelineDescriptor.fragmentFunction = [defaultLibrary newFunctionWithName:@"textureShader"];
    
    // Create pipeline state
    _pipelineState = [_device newRenderPipelineStateWithDescriptor:pipelineDescriptor error:&error];
    if (!_pipelineState) {
        Debug::Printf(kDbgMsg_Error, "Failed to create pipeline state: %s", error.localizedDescription.UTF8String);
        return;
    }
    
    [pipelineDescriptor release];
    [defaultLibrary release];
}

void MetalGraphicsDriver::CreateShaders()
{
    // Shader creation is handled in the Metal shader file and pipeline setup
}

bool MetalGraphicsDriver::SetDisplayMode(const DisplayMode &mode)
{
    if (!IsModeSupported(mode)) {
        return false;
    }
    
    if (!_firstTimeInit && !FirstTimeInit()) {
        return false;
    }
    
    _mode = mode;
    
    // Create Metal view
    NSWindow *window = (__bridge NSWindow*)sys_get_window();
    if (!window) {
        Debug::Printf(kDbgMsg_Error, "Failed to get window for Metal view");
        return false;
    }
    
    _metalView = [[MTKView alloc] initWithFrame:window.contentView.bounds device:_device];
    _metalView.colorPixelFormat = MTLPixelFormatBGRA8Unorm;
    _metalView.clearColor = MTLClearColorMake(0.0, 0.0, 0.0, 1.0);
    
    window.contentView = _metalView;
    
    InitMetalState(mode);
    return true;
}

void MetalGraphicsDriver::InitMetalState(const DisplayMode &mode)
{
    // Set up viewport
    _metalView.drawableSize = CGSizeMake(mode.Width, mode.Height);
    
    // Set up vsync
    _metalView.enableSetNeedsDisplay = !mode.Vsync;
    _metalView.displaySyncEnabled = mode.Vsync;
}

bool MetalGraphicsDriver::IsModeSupported(const DisplayMode &mode)
{
    if (mode.Width <= 0 || mode.Height <= 0) {
        Debug::Printf(kDbgMsg_Error, "Invalid resolution parameters: %d x %d", mode.Width, mode.Height);
        return false;
    }
    if (mode.ColorDepth != 32) {
        Debug::Printf(kDbgMsg_Error, "Display colour depth not supported: %d", mode.ColorDepth);
        return false;
    }
    return true;
}

void MetalGraphicsDriver::UpdateDeviceScreen(const Size &screen_size)
{
    if (_metalView) {
        _metalView.drawableSize = CGSizeMake(screen_size.Width, screen_size.Height);
        _mode.Width = screen_size.Width;
        _mode.Height = screen_size.Height;
    }
}

bool MetalGraphicsDriver::SetNativeSize(const Size &src_size)
{
    _srcRect = src_size;
    return true;
}

bool MetalGraphicsDriver::SetRenderFrame(const Rect &dst_rect)
{
    _dstRect = dst_rect;
    return true;
}

void MetalGraphicsDriver::SetTintMethod(TintMethod method)
{
    // Update shader selection based on tint method
}

bool MetalGraphicsDriver::SupportsGammaControl()
{
    return false;
}

void MetalGraphicsDriver::SetGamma(int newGamma)
{
    // Metal doesn't support direct gamma control
}

void MetalGraphicsDriver::RenderSpritesAtScreenResolution(bool enabled)
{
    // Implement sprite resolution handling
}

void MetalGraphicsDriver::RenderTexture(MTLBitmap *bmpToDraw, int draw_x, int draw_y,
    const glm::mat4 &projection, const glm::mat4 &matGlobal,
    const SpriteColorTransform &color, const Size &rend_sz)
{
    const int alpha = (color.Alpha * bmpToDraw->_alpha) / 255;
    
    // Create command buffer and encoder
    id<MTLCommandBuffer> commandBuffer = [_commandQueue commandBuffer];
    id<MTLRenderCommandEncoder> renderEncoder = [commandBuffer renderCommandEncoderWithDescriptor:_metalView.currentRenderPassDescriptor];
    
    // Set pipeline state
    [renderEncoder setRenderPipelineState:_pipelineState];
    
    // Set up vertex buffer
    const float width = bmpToDraw->GetWidthToRender();
    const float height = bmpToDraw->GetHeightToRender();
    const float xProportion = width / (float)bmpToDraw->_width;
    const float yProportion = height / (float)bmpToDraw->_height;
    
    // Create vertex data
    struct Vertex {
        float2 position;
        float2 texCoord;
    };
    
    const Vertex vertices[] = {
        {{draw_x, draw_y}, {0.0f, 0.0f}},
        {{draw_x + width, draw_y}, {1.0f, 0.0f}},
        {{draw_x, draw_y + height}, {0.0f, 1.0f}},
        {{draw_x + width, draw_y + height}, {1.0f, 1.0f}}
    };
    
    id<MTLBuffer> vertexBuffer = [_device newBufferWithBytes:vertices
                                                    length:sizeof(vertices)
                                                   options:MTLResourceStorageModeShared];
    
    [renderEncoder setVertexBuffer:vertexBuffer offset:0 atIndex:0];
    
    // Set up uniforms
    struct Uniforms {
        float4x4 modelViewProjection;
    } uniforms;
    
    // Calculate transformation matrix
    glm::mat4 transform = projection;
    transform = glmex::translate(transform, rend_sz.Width / 2.0f, rend_sz.Height / 2.0f);
    transform = transform * matGlobal;
    transform = glmex::transform2d(transform, draw_x, draw_y, width, height, 0.f);
    
    // Convert GLM matrix to Metal matrix
    for (int i = 0; i < 4; i++) {
        for (int j = 0; j < 4; j++) {
            uniforms.modelViewProjection.columns[i][j] = transform[i][j];
        }
    }
    
    id<MTLBuffer> uniformBuffer = [_device newBufferWithBytes:&uniforms
                                                     length:sizeof(uniforms)
                                                    options:MTLResourceStorageModeShared];
    
    [renderEncoder setVertexBuffer:uniformBuffer offset:0 atIndex:1];
    
    // Set up texture
    [renderEncoder setFragmentTexture:bmpToDraw->_renderTarget atIndex:0];
    
    // Set up fragment shader parameters based on tint/lighting
    if (bmpToDraw->_tintSaturation > 0) {
        // Use tint shader
        struct TintParams {
            float3 tintHSV;
            float tintAmount;
            float tintLuminance;
            float alpha;
        } tintParams;
        
        // Convert RGB to HSV
        float rgb[3] = {
            bmpToDraw->_red / 255.0f,
            bmpToDraw->_green / 255.0f,
            bmpToDraw->_blue / 255.0f
        };
        
        // Calculate HSV values
        float max = std::max(std::max(rgb[0], rgb[1]), rgb[2]);
        float min = std::min(std::min(rgb[0], rgb[1]), rgb[2]);
        float delta = max - min;
        
        tintParams.tintHSV.x = 0.0f; // Hue
        if (delta > 0.0f) {
            if (max == rgb[0]) {
                tintParams.tintHSV.x = (rgb[1] - rgb[2]) / delta;
            } else if (max == rgb[1]) {
                tintParams.tintHSV.x = 2.0f + (rgb[2] - rgb[0]) / delta;
            } else {
                tintParams.tintHSV.x = 4.0f + (rgb[0] - rgb[1]) / delta;
            }
            tintParams.tintHSV.x *= 60.0f;
            if (tintParams.tintHSV.x < 0.0f) {
                tintParams.tintHSV.x += 360.0f;
            }
        }
        
        tintParams.tintHSV.y = max > 0.0f ? delta / max : 0.0f; // Saturation
        tintParams.tintHSV.z = max; // Value
        tintParams.tintAmount = bmpToDraw->_tintSaturation / 255.0f;
        tintParams.tintLuminance = bmpToDraw->_lightLevel > 0 ? bmpToDraw->_lightLevel / 255.0f : 1.0f;
        tintParams.alpha = alpha / 255.0f;
        
        id<MTLBuffer> tintBuffer = [_device newBufferWithBytes:&tintParams
                                                      length:sizeof(tintParams)
                                                     options:MTLResourceStorageModeShared];
        [renderEncoder setFragmentBuffer:tintBuffer offset:0 atIndex:0];
    } else if (bmpToDraw->_lightLevel > 0) {
        // Use light shader
        struct LightParams {
            float4 lightColor;
            float lightAmount;
        } lightParams;
        
        float light_lev = 1.0f;
        if (bmpToDraw->_lightLevel < 256) {
            light_lev = -((bmpToDraw->_lightLevel * 192) / 256 + 64) / 255.0f;
        } else {
            light_lev = ((bmpToDraw->_lightLevel - 256) / 2) / 255.0f;
        }
        
        lightParams.lightColor = {1.0f, 1.0f, 1.0f, alpha / 255.0f};
        lightParams.lightAmount = light_lev;
        
        id<MTLBuffer> lightBuffer = [_device newBufferWithBytes:&lightParams
                                                       length:sizeof(lightParams)
                                                      options:MTLResourceStorageModeShared];
        [renderEncoder setFragmentBuffer:lightBuffer offset:0 atIndex:0];
    } else {
        // Use transparency shader
        float alphaValue = alpha / 255.0f;
        id<MTLBuffer> alphaBuffer = [_device newBufferWithBytes:&alphaValue
                                                       length:sizeof(float)
                                                      options:MTLResourceStorageModeShared];
        [renderEncoder setFragmentBuffer:alphaBuffer offset:0 atIndex:0];
    }
    
    // Draw the quad
    [renderEncoder drawPrimitives:MTLPrimitiveTypeTriangleStrip
                     vertexStart:0
                     vertexCount:4];
    
    // End encoding and commit
    [renderEncoder endEncoding];
    [commandBuffer commit];
    
    // Release resources
    [vertexBuffer release];
    [uniformBuffer release];
}

// Metal Graphics Factory Implementation
size_t MetalGraphicsFactory::GetFilterCount() const
{
    return 1; // Basic filter only for now
}

const GfxFilterInfo *MetalGraphicsFactory::GetFilterInfo(size_t index) const
{
    static const GfxFilterInfo filter = { "StdScale", "Standard", "Linear interpolation" };
    return index == 0 ? &filter : nullptr;
}

String MetalGraphicsFactory::GetDefaultFilterID() const
{
    return "StdScale";
}

IGraphicsDriver *MetalGraphicsFactory::CreateDriver(const String &id)
{
    if (id == "Metal")
        return new MetalGraphicsDriver();
    return nullptr;
}


} // namespace MTL
} // namespace Engine
} // namespace AGS