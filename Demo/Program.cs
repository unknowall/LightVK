using LightVK;
using SDL2;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using static LightVK.VulkanDevice;
using static LightVK.VulkanNative;

namespace LightVKMinimalDemo
{
    public class LightVKTriangleDemo : IDisposable
    {
        private VulkanDevice _device;
        private VkRenderPass _renderPass;
        private vkSwapchain _swapChain;
        private vkGraphicsPipeline _pipeline;
        private vkBuffer _vertexBuffer;
        private vkCMDS _cmdBuffers;
        private FrameFence[] _frameFences;
        private VkDescriptorSetLayout _descriptorLayout;
        private VkDescriptorPool _descriptorPool;
        private VkDescriptorSet _descriptorSet;

        // Synchronization object encapsulation
        private class FrameFence
        {
            public VkSemaphore ImageAvailable;
            public VkSemaphore RenderFinished;
            public VkFence InFlightFence;
        }

        // Triangle vertex data structure
        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex
        {
            public Vector2 Position;
            public Vector3 Color;

            public Vertex(float x, float y, float r, float g, float b)
            {
                Position = new Vector2(x, y);
                Color = new Vector3(r, g, b);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

        IntPtr window;
        IntPtr hinstance;
        IntPtr windowhwnd;

        public LightVKTriangleDemo()
        {
            File.Copy("../../../SDL2.dll", "./SDL2.dll", true);

            // 1. Create window
            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_AUDIO) != 0)
            {
                Console.Error.WriteLine("Couldn't initialize SDL");
                return;
            }

            window = SDL.SDL_CreateWindow(
                "LightVK",
                SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,
                800, 600,
                SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE
            );

            SDL.SDL_SysWMinfo wmInfo = new SDL.SDL_SysWMinfo();
            SDL.SDL_VERSION(out wmInfo.version);
            SDL.SDL_GetWindowWMInfo(window, ref wmInfo);
            windowhwnd = wmInfo.info.win.window;

            if (IntPtr.Size == 8)
            {
                hinstance = GetWindowLongPtr(windowhwnd, -6);
            }
            else
            {
                hinstance = GetWindowLong32(windowhwnd, -6);
            }

            // 2. Initialize VulkanDevice
            _device = new VulkanDevice();
            _device.VulkanInit(windowhwnd, hinstance, true); // Enable validation layers
        }

        public void Initialize()
        {
            // 3. Create RenderPass
            _renderPass = _device.CreateRenderPass(
                _device.ChooseSurfaceFormat().format,
                VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR,
                VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
                VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR);

            // 4. Create SwapChain
            _swapChain = _device.CreateSwapChain(_renderPass, 800, 600);

            // 5. Create Vertex Buffer
            CreateVertexBuffer();

            // 6. Create Descriptor Set Layout and Descriptor Set
            ConfigDescriptorSet();

            // 7. Create Graphics Pipeline
            CreatePipeline();

            // 8. Create Command Buffers
            _cmdBuffers = _device.CreateCommandBuffers(_swapChain.Images.Count);

            // 9. Create Synchronization Objects (Semaphore/Fence)
            CreateSyncObjects();

            Console.WriteLine("LightVK Demo initialization completed!");
        }

        private unsafe void CreateVertexBuffer()
        {
            // Triangle vertex data
            var vertices = new[]
            {
                new Vertex(-0.5f, -0.5f, 1.0f, 0.0f, 0.0f), // Red
                new Vertex(0.5f, -0.5f, 0.0f, 1.0f, 0.0f),  // Green
                new Vertex(0.0f, 0.5f, 0.0f, 0.0f, 1.0f)    // Blue
            };

            // Create vertex buffer
            _vertexBuffer = _device.CreateBuffer(
                (ulong)(vertices.Length * Marshal.SizeOf<Vertex>()),
                VkBufferUsageFlags.VK_BUFFER_USAGE_VERTEX_BUFFER_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT);

            // Map memory and write vertex data
            void* mappedData;
            vkMapMemory(_device.device, _vertexBuffer.stagingMemory, 0, VK_WHOLE_SIZE, 0, &mappedData);
            Marshal.Copy(
                Array.ConvertAll(vertices, v =>
                {
                    var bytes = new byte[Marshal.SizeOf<Vertex>()];
                    var ptr = Marshal.AllocHGlobal(bytes.Length);
                    Marshal.StructureToPtr(v, ptr, false);
                    Marshal.Copy(ptr, bytes, 0, bytes.Length);
                    Marshal.FreeHGlobal(ptr);
                    return bytes;
                }).SelectMany(b => b).ToArray(),
                0, (IntPtr)mappedData, vertices.Length * Marshal.SizeOf<Vertex>());
            vkUnmapMemory(_device.device, _vertexBuffer.stagingMemory);
        }

        public unsafe void ConfigDescriptorSet()
        {
            var DescriptorPool = _device.CreateDescriptorPool(
            maxSets: 1,
            new[]
            {
                    new VkDescriptorPoolSize
                    {
                        type = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
                        descriptorCount = 1
                    }
                }
            );

            _descriptorLayout = _device.CreateDescriptorSetLayout(new[]
            {
                new VkDescriptorSetLayoutBinding {
                    binding = 0,
                    descriptorCount = 0, // This will trigger a validation layer error. This DEMO only demonstrates the process. Please set the correct value and bind resources when using it in practice
                    descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
                    stageFlags = VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT
                }
            });

            var DescriptorSet = _device.AllocateDescriptorSet(_descriptorLayout, DescriptorPool);

            //VkDescriptorImageInfo imageInfo = new VkDescriptorImageInfo
            //{
            //    sampler = drawTexture.sampler,
            //    imageView = drawTexture.imageview,
            //    imageLayout = VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL
            //};

            _device.UpdateDescriptorSets(
                 new[]{
                    new VkWriteDescriptorSet
                    {
                        sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                        dstSet = DescriptorSet,
                        dstBinding = 0,
                        dstArrayElement = 0,
                        descriptorCount = 1,
                        descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
                        pImageInfo = null
                    }
                 },
                 DescriptorSet
            );
        }

        private void CreatePipeline()
        {
            // Vertex/Fragment shaders (SPIR-V bytecode)
            var vertShader = _device.LoadShaderFile("../../../triangle.vert.spv");
            var fragShader = _device.LoadShaderFile("../../../triangle.frag.spv");

            // Vertex attribute description (corresponding to Vertex struct)
            var vertexAttributes = new[]
            {
                new VkVertexInputAttributeDescription
                {
                    location = 0,
                    binding = 0,
                    format = VkFormat.VK_FORMAT_R32G32_SFLOAT,
                    offset = 0 // Position (Vector2)
                },
                new VkVertexInputAttributeDescription
                {
                    location = 1,
                    binding = 0,
                    format = VkFormat.VK_FORMAT_R32G32B32_SFLOAT,
                    offset = 8 // Color (Vector3)
                }
            };

            // Vertex binding description
            var vertexBinding = new VkVertexInputBindingDescription
            {
                binding = 0,
                stride = (uint)Marshal.SizeOf<Vertex>(),
                inputRate = VkVertexInputRate.VK_VERTEX_INPUT_RATE_VERTEX
            };

            // Blend mode (no blending)
            var blendAttachment = new VkPipelineColorBlendAttachmentState
            {
                blendEnable = VkBool32.False,
                colorWriteMask = VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT |
                                 VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT |
                                 VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT |
                                 VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT
            };

            // Create graphics pipeline
            _pipeline = _device.CreateGraphicsPipeline(
                _renderPass,
                new VkExtent2D(800, 600),
                _descriptorLayout,
                vertShader,
                fragShader,
                800, 600,
                false,
                VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
                vertexBinding,
                vertexAttributes,
                blendAttachment,
                default,
                VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST);
        }

        private void CreateSyncObjects()
        {
            _frameFences = new FrameFence[_swapChain.Images.Count];
            for (int i = 0; i < _swapChain.Images.Count; i++)
            {
                _frameFences[i] = new FrameFence
                {
                    ImageAvailable = _device.CreateSemaphore(),
                    RenderFinished = _device.CreateSemaphore(),
                    InFlightFence = _device.CreateFence(true)
                };
            }
        }

        static uint frameIndex = 0;

        public unsafe void RenderFrame()
        {
            // 1. Wait for Fence to be idle
            FrameFence currentFrame = _frameFences[frameIndex];

            fixed (VkFence* currentFramePtr = &currentFrame.InFlightFence)
            {
                vkWaitForFences(_device.device, 1, currentFramePtr, true, ulong.MaxValue);
                vkResetFences(_device.device, 1, currentFramePtr);
            }

            // 2. Acquire swap chain image
            uint imageIndex;

            vkAcquireNextImageKHR(_device.device, _swapChain.Chain, uint.MaxValue,
            _frameFences[frameIndex].ImageAvailable, VkFence.Null, &imageIndex);

            // 3. Reset command buffer and start recording
            VkCommandBuffer presentCmd = _cmdBuffers.CMD[frameIndex];

            vkResetCommandBuffer(presentCmd, 0);
            _device.BeginCommandBuffer(presentCmd);

            // 4. Begin render pass
            _device.BeginRenderPass(presentCmd, _renderPass,
             _swapChain.framebuffes[(int)frameIndex],
             (int)_swapChain.Extent.width,
             (int)_swapChain.Extent.height,
             true, 0, 0, 0, 1);

            // 5. Bind pipeline and vertex buffer, set viewport and scissor
            _device.BindGraphicsPipeline(presentCmd, _pipeline);

            var buffer = _vertexBuffer.stagingBuffer;
            var offset = 0ul;
            vkCmdBindVertexBuffers(presentCmd, 0, 1, &buffer, &offset);

            // 6. Draw triangle
            vkCmdDraw(presentCmd, 3, 1, 0, 0);

            // 7. End render pass and command buffer
            vkCmdEndRenderPass(presentCmd);

            _device.EndCommandBuffer(presentCmd);

            // 9. Submit command buffer to queue
            VkPipelineStageFlags waitStages = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;
            VkSemaphore ws = currentFrame.ImageAvailable;
            VkSemaphore ss = currentFrame.RenderFinished;
            VkSwapchainKHR chain = _swapChain.Chain;

            VkSubmitInfo submitInfo = new VkSubmitInfo();
            submitInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO;
            submitInfo.waitSemaphoreCount = 1;
            submitInfo.pWaitSemaphores = &ws;
            submitInfo.pWaitDstStageMask = &waitStages;
            submitInfo.commandBufferCount = 1;
            submitInfo.pCommandBuffers = &presentCmd;
            submitInfo.signalSemaphoreCount = 1;
            submitInfo.pSignalSemaphores = &ss;

            vkQueueSubmit(_device.graphicsQueue, 1, &submitInfo, currentFrame.InFlightFence);

            // 10. Present image
            VkPresentInfoKHR presentInfo = new VkPresentInfoKHR
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_PRESENT_INFO_KHR,
                waitSemaphoreCount = 1,
                pWaitSemaphores = &ss,
                swapchainCount = 1,
                pSwapchains = &chain,
                pImageIndices = &imageIndex
            };

            vkQueuePresentKHR(_device.presentQueue, &presentInfo);

            frameIndex = (frameIndex + 1) % _swapChain.Images.Count;
        }

        public unsafe void Dispose()
        {
            // Wait for queues to be idle
            vkQueueWaitIdle(_device.graphicsQueue);
            vkQueueWaitIdle(_device.presentQueue);

            // Destroy synchronization objects
            foreach (var fence in _frameFences)
            {
                vkDestroySemaphore(_device.device, fence.ImageAvailable, null);
                vkDestroySemaphore(_device.device, fence.RenderFinished, null);
                vkDestroyFence(_device.device, fence.InFlightFence, null);
            }

            // Destroy pipeline/buffers/render pass/swap chain
            _device.DestroyGraphicsPipeline(_pipeline);
            _device.DestoryBuffer(_vertexBuffer);
            _device.DestoryCommandBuffers(_cmdBuffers);
            _device.CleanupSwapChain(_swapChain);
            vkDestroyRenderPass(_device.device, _renderPass, null);

            // Destroy Device
            _device.VulkanDispose();

            Console.WriteLine("LightVK Demo resource cleanup completed!");
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            using var demo = new LightVKTriangleDemo();

            demo.Initialize();

            for (int i = 0; i < 1000; i++)
            {
                demo.RenderFrame();
                System.Threading.Thread.Sleep(16);
            }

            Console.WriteLine("Demo execution completed!");
        }
    }
}