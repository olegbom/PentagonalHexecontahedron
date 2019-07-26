using System;
using System.Collections.Generic;
using System.Numerics;
using PentagonalHexecontahedron.Properties;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;

namespace PentagonalHexecontahedron
{
    class Program
    {
        
        private static GraphicsDevice _graphicsDevice;
        private static CommandList _commandList;
        private static DeviceBuffer _vertexBuffer;
        private static DeviceBuffer _indexBuffer;
        private static DeviceBuffer _projMatrixBuffer;
        private static DeviceBuffer _modelMatrixBuffer;
        private static DeviceBuffer _instanceVB;
        private static ResourceLayout _layout;
        private static ResourceSet _mainResourceSet;

        private static Shader[] _shaders;
        private static Pipeline _pipeline;
        private static Sdl2Window _window;

        private static List<IrregularPentagon> _pentagons = new List<IrregularPentagon>();
        private static uint _instanceCount;

        private static bool _isResized;
        private static float _rotate;

        static void Main(string[] args)
        {
            
            WindowCreateInfo windowCi = new WindowCreateInfo()
            {
                X = 100,
                Y = 100,
                WindowWidth = 800,
                WindowHeight = 600,
                WindowTitle= "Pentagonal Hexecontahedron",
                WindowInitialState = WindowState.Normal
            };
            GraphicsDeviceOptions options = new GraphicsDeviceOptions(
                debug: false,
                swapchainDepthFormat: PixelFormat.R16_UNorm,
                syncToVerticalBlank: true,
                resourceBindingModel: ResourceBindingModel.Improved,
                preferDepthRangeZeroToOne: true,
                preferStandardClipSpaceYDirection: true);

            _window = VeldridStartup.CreateWindow(ref windowCi);
            _window.Resized += () => { _isResized = true; };
            
            _window.KeyUp += keyArg =>
            {
                if (keyArg.Key == Key.Escape)
                {
                    _window.Close();
                }
            };
            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(_window, options);
            
            CreateResources();

            while (_window.Exists)
            {
                InputSnapshot snapshot = _window.PumpEvents();
                if (snapshot.IsMouseDown(MouseButton.Left))
                {
                    _rotate += 0.01f;
                    ViewProjectionUpdate();
                }
                Draw();
            }

            DisposeResources();
        }

        private static void CreateResources()
        {
            _instanceCount = 5;

            ResourceFactory factory = _graphicsDevice.ResourceFactory;
          
            _vertexBuffer = factory.CreateBuffer(
                new BufferDescription(IrregularPentagon.VerticesCount * VertexPositionColor.SizeInBytes,
                    BufferUsage.VertexBuffer));
            _indexBuffer = factory.CreateBuffer(
                new BufferDescription(IrregularPentagon.IndicesCount * sizeof(ushort), 
                    BufferUsage.IndexBuffer));
            _projMatrixBuffer = factory.CreateBuffer(
                new BufferDescription(sizeof(float)*4*4, 
                BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _projMatrixBuffer.Name = "Projection Buffer";
            _modelMatrixBuffer = factory.CreateBuffer(
                new BufferDescription(sizeof(float) * 4 * 4,
                    BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _modelMatrixBuffer.Name = "Model Buffer";


            _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, IrregularPentagon.Vertices);
            _graphicsDevice.UpdateBuffer(_indexBuffer, 0, IrregularPentagon.Indices);

            ViewProjectionUpdate();

            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float2),
                new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float4));

            _layout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ViewProjectionMatrix", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ModelMatrix", ResourceKind.UniformBuffer, ShaderStages.Vertex)));


            ShaderDescription vertexShaderDesc = new ShaderDescription(ShaderStages.Vertex,
                Resources.VertexShader,
                "main");
            ShaderDescription fragmentShaderDesc = new ShaderDescription(ShaderStages.Fragment,
                Resources.FragmentShader,
                "main");
            _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);


            VertexLayoutDescription vertexLayoutPerInstance = new VertexLayoutDescription(20, 1,
                new VertexElementDescription("InstanceSphericalCoordinates", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("InstanceRotation", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
                new VertexElementDescription("InstanceScale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1));

           // vertexLayoutPerInstance.InstanceStepRate = 3;

            _instanceVB = factory.CreateBuffer(new BufferDescription(InstanceInfo.Size * _instanceCount, BufferUsage.VertexBuffer));
            InstanceInfo[] infos = new InstanceInfo[_instanceCount];

            Random r = new Random();
            
            for (uint i = 0; i < _instanceCount; i++)
            {
                float angle = (float)(r.NextDouble() * Math.PI * 2);
                infos[i] = new InstanceInfo(
                    new Vector3(0,0,0),
                     i/0.1f,
                    1);
            }

            _graphicsDevice.UpdateBuffer(_instanceVB, 0, infos);

            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleAlphaBlend,
                DepthStencilState = new DepthStencilStateDescription(
                    true,
                    true,
                    ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription(
                    FaceCullMode.Back,
                    PolygonFillMode.Solid,
                    FrontFace.Clockwise,
                    true,
                    false),
                PrimitiveTopology = PrimitiveTopology.TriangleStrip,
                ResourceLayouts = new []{_layout},
                ShaderSet = new ShaderSetDescription(new[] {vertexLayout, vertexLayoutPerInstance}, _shaders),
                Outputs = _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription,
                
            };
            _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

            _mainResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_layout,
                _projMatrixBuffer, _modelMatrixBuffer));

            _commandList = factory.CreateCommandList();

            var firstPentagon = new IrregularPentagon();
            var secondPentagon = new IrregularPentagon(firstPentagon, 0);
            _pentagons.Add(firstPentagon);
            _pentagons.Add(secondPentagon);

        }

        private static void ViewProjectionUpdate()
        {
            float aspectRatio = _window.Width / (float)_window.Height;

            var matrix = aspectRatio > 1 
                ? Matrix4x4.CreateOrthographicOffCenter(-2 * aspectRatio, 2 * aspectRatio, -2, 2, -1, 1) 
                : Matrix4x4.CreateOrthographicOffCenter(-2, 2, -2 / aspectRatio, 2 / aspectRatio, -1, 1);
            _graphicsDevice.UpdateBuffer(_projMatrixBuffer, 0, Matrix4x4.CreateRotationZ(_rotate) * matrix );
        }

        private static void Draw()
        {

            if (_isResized)
            {
                _isResized = false;

                _graphicsDevice.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
                ViewProjectionUpdate();

            }

            _commandList.Begin();
            _graphicsDevice.UpdateBuffer(_modelMatrixBuffer, 0, Matrix4x4.Identity);
            _commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
            _commandList.ClearColorTarget(0, new RgbaFloat(239/255.0f, 211/255.0f, 169/255.0f, 1.0f));
            _commandList.ClearDepthStencil(1f);
            
          
                
            _commandList.SetPipeline(_pipeline);
            _commandList.SetGraphicsResourceSet(0, _mainResourceSet);
            _commandList.SetVertexBuffer(0, _vertexBuffer);
            _commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            _commandList.SetVertexBuffer(1, _instanceVB);

            _commandList.DrawIndexed(IrregularPentagon.IndicesCount, 1,0,0, 1);

            

            //_commandList.DrawIndexed(IrregularPentagon.IndicesCount, 1, 0, 0, 1);
            _commandList.End();

            _graphicsDevice.SubmitCommands(_commandList);
            _graphicsDevice.WaitForIdle();
            _graphicsDevice.SwapBuffers();
        }

        private static void DisposeResources()
        {
            _pipeline.Dispose();
            foreach (var s in _shaders)
            {
                s.Dispose();
            }
            _commandList.Dispose();
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _layout.Dispose();
            _graphicsDevice.Dispose();
        }
    }
}
