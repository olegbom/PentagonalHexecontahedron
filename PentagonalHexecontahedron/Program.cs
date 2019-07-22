using System;
using System.Numerics;
using System.Text;
using PentagonalHexecontahedron.Properties;

using SharpDX.Text;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;
using Vulkan;
using Vulkan.Xlib;
using Encoding = System.Text.Encoding;

namespace PentagonalHexecontahedron
{
    class Program
    {
        
        private static GraphicsDevice _graphicsDevice;
        private static CommandList _commandList;
        private static DeviceBuffer _vertexBuffer;
        private static DeviceBuffer _indexBuffer;
        private static DeviceBuffer _projMatrixBuffer;
        private static ResourceLayout _layout;
        private static ResourceSet _mainResourceSet;
        private static Shader[] _shaders;
        private static Pipeline _pipeline;
        private static Sdl2Window _window;



        private static bool _isResized;
        private static float rotate;

        static void Main(string[] args)
        {
            
            WindowCreateInfo windowCi = new WindowCreateInfo()
            {
                X = 0,
                Y = 0,
                WindowWidth = 1920,
                WindowHeight = 1080,
                WindowTitle= "Pentagonal Hexecontahedron",
                WindowInitialState = WindowState.FullScreen
            };
            _window = VeldridStartup.CreateWindow(ref windowCi);
            _window.Resized += () => { _isResized = true; };
            _window.KeyUp += keyArg =>
            {
                if (keyArg.Key == Key.Escape)
                {
                    _window.Close();
                }
            };
            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(_window);

            CreateResources();

            while (_window.Exists)
            {
                InputSnapshot snapshot = _window.PumpEvents();
                if (snapshot.IsMouseDown(MouseButton.Left))
                {
                    rotate += 0.001f;
                    ViewProjectionUpdate();
                }
                Draw();
            }

            DisposeResources();
        }

        private static void CreateResources()
        {
            ResourceFactory factory = _graphicsDevice.ResourceFactory;
            VertexPositionColor[] quadVertices = IrregularPentagon.CreateVertices();/*
            {
                new VertexPositionColor(new Vector2(-.75f, .75f), RgbaFloat.Blue),
                new VertexPositionColor(new Vector2(.75f, .75f), RgbaFloat.Green),
                new VertexPositionColor(new Vector2(-.75f, -.75f), RgbaFloat.Red),
                new VertexPositionColor(new Vector2(.75f, -.75f), RgbaFloat.Yellow),
            };*/

            ushort[] quadIndices = IrregularPentagon.CreateIndices();// { 0, 1, 2, 3 };

            _vertexBuffer = factory.CreateBuffer(
                new BufferDescription(IrregularPentagon.VerticesCount * VertexPositionColor.SizeInBytes,
                    BufferUsage.VertexBuffer));
            _indexBuffer = factory.CreateBuffer(
                new BufferDescription(IrregularPentagon.IndicesCount * sizeof(ushort), 
                    BufferUsage.IndexBuffer));
            _projMatrixBuffer = factory.CreateBuffer(
                new BufferDescription(sizeof(float)*4*4, 
                BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

            _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, quadVertices);
            _graphicsDevice.UpdateBuffer(_indexBuffer, 0, quadIndices);

            ViewProjectionUpdate();

            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float2),
                new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float4));

            _layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));


            ShaderDescription vertexShaderDesc = new ShaderDescription(ShaderStages.Vertex,
                Resources.VertexShader,
                "main");
            ShaderDescription fragmentShaderDesc = new ShaderDescription(ShaderStages.Fragment,
                Resources.FragmentShader,
                "main");
            _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleOverrideBlend,
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
                ShaderSet = new ShaderSetDescription(new[] {vertexLayout}, _shaders),
                Outputs =  _graphicsDevice.SwapchainFramebuffer.OutputDescription,
                
            };
            _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

            _mainResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_layout,
                _projMatrixBuffer));

            _commandList = factory.CreateCommandList();

        }

        private static void ViewProjectionUpdate()
        {
            float aspectRatio = _window.Width / (float)_window.Height;

            var matrix = aspectRatio > 1 
                ? Matrix4x4.CreateOrthographicOffCenter(-2 * aspectRatio, 2 * aspectRatio, -2, 2, -1, 1) 
                : Matrix4x4.CreateOrthographicOffCenter(-2, 2, -2 / aspectRatio, 2 / aspectRatio, -1, 1);
            _graphicsDevice.UpdateBuffer(_projMatrixBuffer, 0, Matrix4x4.CreateRotationZ(rotate) * matrix );
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
            _commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
            _commandList.ClearColorTarget(0, RgbaFloat.Black);
            _commandList.SetVertexBuffer(0, _vertexBuffer);
            _commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            _commandList.SetPipeline(_pipeline);
            _commandList.SetGraphicsResourceSet(0, _mainResourceSet);

            _commandList.DrawIndexed(IrregularPentagon.IndicesCount, 1,0,0,0);

            _commandList.End();

            _graphicsDevice.SubmitCommands(_commandList);
            _graphicsDevice.SwapBuffers();
        }

        private static void DisposeResources()
        {
            _pipeline.Dispose();
            for (int i = 0; i < _shaders.Length; i++)
            {
                _shaders[i].Dispose();
            }
            _commandList.Dispose();
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _layout.Dispose();
            _graphicsDevice.Dispose();
        }
    }
}
