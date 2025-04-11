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