# LightVK: Lightweight Vulkan C# Low-Level Bindings
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.txt)
[![NuGet](https://img.shields.io/nuget/v/LightVK)](https://www.nuget.org/packages/LightVK/)
[![GitHub NuGet Package](https://img.shields.io/badge/GitHub%20Package-LightVK-blue)](https://github.com/unknowall/LightVK/pkgs/nuget/LightVK)

LightVK is a lightweight set of low-level Vulkan bindings for C#, automatically generated from the official `vk.xml` specification. It provides C# developers with near-native performance access to Vulkan APIs.  

For the complex procedural steps of Vulkan, LightVK offers lightweight abstract encapsulation that simplifies development while preserving native flexibility.

Wiki: https://deepwiki.com/unknowall/LightVK

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

