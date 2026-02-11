# LightVK: Lightweight Vulkan C# Low-Level Bindings
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.txt)
[![NuGet](https://img.shields.io/nuget/v/LightVK)](https://www.nuget.org/packages/LightVK/)
[![GitHub NuGet Package](https://img.shields.io/badge/GitHub%20Package-LightVK-blue)](https://github.com/unknowall/LightVK/pkgs/nuget/LightVK)

LightVK is a lightweight set of low-level Vulkan bindings for C#, automatically generated from the official `vk.xml` specification. It provides C# developers with near-native performance access to Vulkan APIs.  

For the complex procedural steps of Vulkan, LightVK offers lightweight abstract encapsulation that simplifies development while preserving native flexibility.

Wiki: https://deepwiki.com/unknowall/LightVK

<details>
<summary><h3> 🌐 中文版说明</h3></summary>

LightVK 是一套轻量级 Vulkan C# 底层绑定库，基于官方 `vk.xml` 规范自动生成，为 C# 开发者提供接近原生性能的 Vulkan API 访问能力。
针对 Vulkan 复杂的流程步骤，LightVK 提供了轻量化抽象封装，简化开发的同时保留原生灵活性。

## 核心特性

- **完整 Vulkan 支持**：覆盖 Vulkan 1.0 ~ 1.4 全版本核心功能
- **全扩展**：支持所有 Vulkan 官方扩展
- **跨平台**：适配 Windows、Linux、Android 三大平台
- **无依赖**：无第三方 NuGet 依赖，仅依赖系统 Vulkan 运行时
- **生产级**：已在 PS1 模拟器 [ScePSX](https://github.com/unknowall/ScePSX) 中实际应用

## 快速开始

### 环境要求

- **硬件**：支持 Vulkan 1.0+ 的 GPU（NVIDIA GTX 900+/AMD HD 7000+/Intel 11代核显及以上）
- **系统**：Windows 7+ / Linux (Ubuntu 14.04+/Fedora 23+) / Android 7.0+
- **运行时**：.NET 8.0+
- **依赖**：Vulkan SDK（开发环境）

### 安装与编译

```bash
# 克隆仓库
git clone https://github.com/unknowall/LightVK.git

# 进入项目目录
cd LightVK

# 编译
dotnet build LightVK.sln
```

### 基础使用示例（设备初始化）

```csharp
using LightVK;
using static LightVK.VulkanDevice;
using static LightVK.VulkanNative;

// 创建 Vulkan 设备实例
var device = new VulkanDevice();

// 初始化设备（Windows 平台示例）
IntPtr windowHandle = /* 你的窗口句柄 */;
IntPtr instanceHandle = /* 你的实例句柄 */;
...
//2 . Initialize VulkanDevice
device.VulkanInit(windowHandle, instanceHandle, enableValidation: true);
...
//3. Create RenderPass
_renderPass = _device.CreateRenderPass(
_device.ChooseSurfaceFormat().format,
VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR,
VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR);
...
// 4. Create SwapChain
_swapChain = _device.CreateSwapChain(_renderPass, 800, 600);
...
// 5. Create Vertex Buffer
var vertices = new[]
{
 new Vertex(-0.5f, -0.5f, 1.0f, 0.0f, 0.0f), // Red
 new Vertex(0.5f, -0.5f, 0.0f, 1.0f, 0.0f),  // Green
 new Vertex(0.0f, 0.5f, 0.0f, 0.0f, 1.0f)    // Blue
};
vertexBuffer = _device.CreateBuffer( (ulong)(vertices.Length * Marshal.SizeOf<Vertex>()),
 VkBufferUsageFlags.VK_BUFFER_USAGE_VERTEX_BUFFER_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT);
......
```

### 运行 Demo

项目内置 `VulkanDevice` 演示程序，展示基础渲染流程：

1. 编译解决方案后，将 `Demo` 项目设为启动项目
2. 运行程序，将看到基础渲染效果
3. Demo 包含：窗口创建、设备初始化、渲染通道/交换链配置、基础绘制流程
<img width="802" height="632" alt="DEMO" src="https://github.com/user-attachments/assets/565962ea-2793-438b-9a4a-dd7c7c1b502a" />

## 关键模块

| 模块          | 功能说明                                  |
|---------------|-------------------------------------------|
| VulkanNative  | 底层 API 绑定（命令、枚举、结构体等）     |
| VulkanTypes   | 基础数据类型封装（VkExtent2D、VkRect2D 等）|
| VulkanDevice  | 设备管理与资源封装（渲染管线、交换链等）  |
| VkGen         | 从 vk.xml 自动生成绑定代码的工具          |

## 许可证

本项目采用 MIT 许可证开源，详情见 [LICENSE.txt](LICENSE.txt)，允许自由使用、修改和分发（商业项目可用）。

## 相关项目

- 应用案例：[ScePSX（PS1 模拟器）](https://github.com/unknowall/ScePSX)
- 规范来源：[Vulkan-Docs（vk.xml）](https://github.com/KhronosGroup/Vulkan-Docs)

## 贡献

欢迎通过 PR 或 Issues 参与项目开发：
1. Fork 本仓库
2. 创建 feature/bugfix 分支
3. 提交代码并说明变更内容
4. 发起 Pull Request

遵循现有代码风格，确保跨平台兼容性，必要时添加注释和测试。

---
</details>

## Core Features
- **Full Vulkan Support**: Covers core functionality across all Vulkan versions from 1.0 to 1.4
- **Complete Extensions**: Supports all official Vulkan extensions
- **Cross-Platform**: Compatible with three major platforms: Windows, Linux, and Android
- **Dependency-Free**: No third-party NuGet dependencies, only relies on the system Vulkan runtime
- **Production-Grade**: Implemented in the PS1 emulator [ScePSX](https://github.com/unknowall/ScePSX)

## Quick Start
### Environment Requirements
- **Hardware**: GPU supporting Vulkan 1.0+ (NVIDIA GTX 900+/AMD HD 7000+/11th Gen Intel Core integrated graphics and above)
- **System**: Windows 7+ / Linux (Ubuntu 14.04+/Fedora 23+) / Android 7.0+
- **Runtime**: .NET 8.0+
- **Dependencies**: Vulkan SDK (development environment only)

### Installation & Compilation
```bash
# Clone the repository
git clone https://github.com/unknowall/LightVK.git

# Navigate to the project directory
cd LightVK

# Compile
dotnet build LightVK.sln
```

### Basic Usage Example (Device Initialization)
```csharp
using LightVK;
using static LightVK.VulkanDevice;
using static LightVK.VulkanNative;

// Create a Vulkan device instance
var device = new VulkanDevice();

// Initialize the device (Windows platform example)
IntPtr windowHandle = /* Your window handle */;
IntPtr instanceHandle = /* Your instance handle */;
...
// 2. Initialize VulkanDevice
device.VulkanInit(windowHandle, instanceHandle, enableValidation: true);
...
// 3. Create RenderPass
_renderPass = _device.CreateRenderPass(
_device.ChooseSurfaceFormat().format,
VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR,
VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR);
...
// 4. Create SwapChain
_swapChain = _device.CreateSwapChain(_renderPass, 800, 600);
...
// 5. Create Vertex Buffer
var vertices = new[]
{
 new Vertex(-0.5f, -0.5f, 1.0f, 0.0f, 0.0f), // Red
 new Vertex(0.5f, -0.5f, 0.0f, 1.0f, 0.0f),  // Green
 new Vertex(0.0f, 0.5f, 0.0f, 0.0f, 1.0f)    // Blue
};
vertexBuffer = _device.CreateBuffer( (ulong)(vertices.Length * Marshal.SizeOf<Vertex>()),
 VkBufferUsageFlags.VK_BUFFER_USAGE_VERTEX_BUFFER_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT);
......
```

### Run the Demo
The project includes a built-in `VulkanDevice` demo program that demonstrates the basic rendering process:
1. After compiling the solution, set the `Demo` project as the startup project
2. Run the program to see basic rendering effects
3. The demo includes: window creation, device initialization, render pass/swap chain configuration, and basic drawing flow
<img width="802" height="632" alt="DEMO" src="https://github.com/user-attachments/assets/565962ea-2793-438b-9a4a-dd7c7c1b502a" />

## Key Modules
| Module          | Description                                  |
|-----------------|----------------------------------------------|
| VulkanNative    | Low-level API bindings (commands, enums, structs, etc.) |
| VulkanTypes     | Basic data type encapsulation (VkExtent2D, VkRect2D, etc.) |
| VulkanDevice    | Device management and resource encapsulation (render pipelines, swap chains, etc.) |
| VkGen           | Tool to auto-generate binding code from vk.xml |

## License
This project is open-source under the MIT License. See [LICENSE.txt](LICENSE.txt) for details. Free for use, modification, and distribution (including commercial projects).

## Related Projects
- Application Example: [ScePSX (PS1 Emulator)](https://github.com/unknowall/ScePSX)
- Specification Source: [Vulkan-Docs (vk.xml)](https://github.com/KhronosGroup/Vulkan-Docs)

## Contribution
Contributions via PR or Issues are welcome:
1. Fork this repository
2. Create a feature/bugfix branch
3. Commit your code with clear descriptions of changes
4. Submit a Pull Request

Follow the existing code style, ensure cross-platform compatibility, and add comments/tests when necessary.

