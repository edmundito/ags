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

#ifndef __AGS_EE_GFX__ALI3DMTL_H
#define __AGS_EE_GFX__ALI3DMTL_H

#include "core/platform.h"

#if AGS_PLATFORM_OS_MACOS || AGS_PLATFORM_OS_IOS

#include <memory>
#include "gfx/graphicsdriver.h"
#include "util/geometry.h"

namespace AGS
{
namespace Engine
{
namespace MTL
{

using namespace AGS::Common;

// Metal texture wrapper
class MTLTexture {
public:
    ~MTLTexture();
    size_t GetMemSize() const;
    
private:
    id<MTLTexture> _texture;
    size_t _width;
    size_t _height;
    size_t _numTiles;
    MTLPixelFormat _format;
};

// Metal bitmap wrapper
class MTLBitmap {
public:
    ~MTLBitmap();
    
private:
    id<MTLTexture> _renderTarget;
};


class MetalGraphicsFactory : public IGfxDriverFactory
{
public:
    virtual ~MetalGraphicsFactory() = default;
    virtual size_t GetFilterCount() const;
    virtual const GfxFilterInfo *GetFilterInfo(size_t index) const;
    virtual String GetDefaultFilterID() const;
    virtual IGraphicsDriver *CreateDriver(const String &id);
};

// Metal graphics driver
class MetalGraphicsDriver : public IGraphicsDriver {
public:
    MetalGraphicsDriver();
    virtual ~MetalGraphicsDriver();

    virtual const char* GetDriverName() override { return "Metal"; }
    virtual const char* GetDriverID() override { return "Metal"; }
    virtual void SetTintMethod(TintMethod method) override;
    virtual bool SetDisplayMode(const DisplayMode &mode) override;
    virtual void UpdateDeviceScreen(const Size &screen_size) override;
    virtual bool SetNativeSize(const Size &src_size) override;
    virtual bool SetRenderFrame(const Rect &dst_rect) override;
    virtual bool IsModeSupported(const DisplayMode &mode) override;
    virtual IGfxModeList *GetSupportedModeList(int color_depth) override;
    virtual bool SupportsGammaControl() override;
    virtual void SetGamma(int newGamma) override;
    virtual void RenderSpritesAtScreenResolution(bool enabled) override;

private:
    bool FirstTimeInit();
    void InitMetalState(const DisplayMode &mode);
    void CreateDefaultRenderPipeline();
    void CreateShaders();
    
    id<MTLDevice> _device;
    id<MTLCommandQueue> _commandQueue;
    MTKView* _metalView;
    id<MTLRenderPipelineState> _pipelineState;
    
    bool _firstTimeInit;
    DisplayMode _mode;
    Size _srcRect;
    Rect _dstRect;
};

} // namespace MTL
} // namespace Engine
} // namespace AGS

#endif // AGS_PLATFORM_OS_MACOS || AGS_PLATFORM_OS_IOS

#endif // __AGS_EE_GFX__ALI3DMTL_H 