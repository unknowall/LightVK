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

        // 同步对象封装
        private class FrameFence
        {
            public VkSemaphore ImageAvailable;
            public VkSemaphore RenderFinished;
            public VkFence InFlightFence;
        }

        // 三角形顶点数据
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

            // 1. 创建窗口
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

            // 2. 初始化VulkanDevice
            _device = new VulkanDevice();
            _device.VulkanInit(windowhwnd, hinstance, true); // 开启验证层
        }

        public void Initialize()
        {
            // 3. 创建渲染通道（RenderPass）
            _renderPass = _device.CreateRenderPass(
                _device.ChooseSurfaceFormat().format,
                VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR,
                VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
                VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR);

            // 4. 创建交换链（SwapChain）
            _swapChain = _device.CreateSwapChain(_renderPass, 800, 600);

            // 5. 创建顶点缓冲区（Buffer）
            CreateVertexBuffer();

            // 6. 创建描述符集布局和描述符集
            ConfigDescriptorSet();

            // 7. 创建图形管线（Pipeline）
            CreatePipeline();

            // 8. 创建命令缓冲区（CommandBuffer）
            _cmdBuffers = _device.CreateCommandBuffers(_swapChain.Images.Count);

            // 9. 创建同步对象（Semaphore/Fence）
            CreateSyncObjects();

            Console.WriteLine("LightVK Demo 初始化完成！");
        }

        private unsafe void CreateVertexBuffer()
        {
            // 三角形顶点数据
            var vertices = new[]
            {
                new Vertex(-0.5f, -0.5f, 1.0f, 0.0f, 0.0f), // 红
                new Vertex(0.5f, -0.5f, 0.0f, 1.0f, 0.0f),  // 绿
                new Vertex(0.0f, 0.5f, 0.0f, 0.0f, 1.0f)    // 蓝
            };

            // 创建顶点缓冲区
            _vertexBuffer = _device.CreateBuffer(
                (ulong)(vertices.Length * Marshal.SizeOf<Vertex>()),
                VkBufferUsageFlags.VK_BUFFER_USAGE_VERTEX_BUFFER_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT);

            // 映射内存并写入顶点数据
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
                    descriptorCount = 0, //此处会触发验证层错误，DEMO仅演示流程，实际使用时请设置正确的值，并绑定资源
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
            // 顶点/片段着色器（SPIR-V字节码）
            var vertShader = _device.LoadShaderFile("../../../triangle.vert.spv");
            var fragShader = _device.LoadShaderFile("../../../triangle.frag.spv");

            // 顶点属性描述（对应Vertex结构体）
            var vertexAttributes = new[]
            {
                new VkVertexInputAttributeDescription
                {
                    location = 0,
                    binding = 0,
                    format = VkFormat.VK_FORMAT_R32G32_SFLOAT,
                    offset = 0 // Position（Vector2）
                },
                new VkVertexInputAttributeDescription
                {
                    location = 1,
                    binding = 0,
                    format = VkFormat.VK_FORMAT_R32G32B32_SFLOAT,
                    offset = 8 // Color（Vector3）
                }
            };

            // 顶点绑定描述
            var vertexBinding = new VkVertexInputBindingDescription
            {
                binding = 0,
                stride = (uint)Marshal.SizeOf<Vertex>(),
                inputRate = VkVertexInputRate.VK_VERTEX_INPUT_RATE_VERTEX
            };

            // 混合模式（不混合）
            var blendAttachment = new VkPipelineColorBlendAttachmentState
            {
                blendEnable = VkBool32.False,
                colorWriteMask = VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT |
                                 VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT |
                                 VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT |
                                 VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT
            };

            // 创建图形管线
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
            // 1. 等待Fence空闲
            FrameFence currentFrame = _frameFences[frameIndex];

            fixed (VkFence* currentFramePtr = &currentFrame.InFlightFence)
            {
                vkWaitForFences(_device.device, 1, currentFramePtr, true, ulong.MaxValue);
                vkResetFences(_device.device, 1, currentFramePtr);
            }

            // 2. 获取交换链图像
            uint imageIndex;

            vkAcquireNextImageKHR(_device.device, _swapChain.Chain, uint.MaxValue,
            _frameFences[frameIndex].ImageAvailable, VkFence.Null, &imageIndex);

            // 3. 重置命令缓冲区并开始录制
            VkCommandBuffer presentCmd = _cmdBuffers.CMD[frameIndex];

            vkResetCommandBuffer(presentCmd, 0);
            _device.BeginCommandBuffer(presentCmd);

            // 4. 开始渲染通道
            _device.BeginRenderPass(presentCmd, _renderPass,
             _swapChain.framebuffes[(int)frameIndex],
             (int)_swapChain.Extent.width,
             (int)_swapChain.Extent.height,
             true, 0, 0, 0, 1);

            // 5. 绑定管线和顶点缓冲区并设置视口和裁剪
            _device.BindGraphicsPipeline(presentCmd, _pipeline);

            var buffer = _vertexBuffer.stagingBuffer;
            var offset = 0ul;
            vkCmdBindVertexBuffers(presentCmd, 0, 1, &buffer, &offset);

            // 6. 绘制三角形
            vkCmdDraw(presentCmd, 3, 1, 0, 0);

            // 7. 结束渲染通道和命令缓冲区
            vkCmdEndRenderPass(presentCmd);

            _device.EndCommandBuffer(presentCmd);

            // 9. 提交命令缓冲区到队列
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

            // 10. 呈现图像
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
            // 等待队列空闲
            vkQueueWaitIdle(_device.graphicsQueue);
            vkQueueWaitIdle(_device.presentQueue);

            // 销毁同步对象
            foreach (var fence in _frameFences)
            {
                vkDestroySemaphore(_device.device, fence.ImageAvailable, null);
                vkDestroySemaphore(_device.device, fence.RenderFinished, null);
                vkDestroyFence(_device.device, fence.InFlightFence, null);
            }

            // 销毁管线/缓冲区/渲染通道/交换链
            _device.DestroyGraphicsPipeline(_pipeline);
            _device.DestoryBuffer(_vertexBuffer);
            _device.DestoryCommandBuffers(_cmdBuffers);
            _device.CleanupSwapChain(_swapChain);
            vkDestroyRenderPass(_device.device, _renderPass, null);

            // 销毁Device
            _device.VulkanDispose();

            Console.WriteLine("LightVK Demo 资源销毁完成！");
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

            Console.WriteLine("Demo 运行完成！");
        }
    }
}