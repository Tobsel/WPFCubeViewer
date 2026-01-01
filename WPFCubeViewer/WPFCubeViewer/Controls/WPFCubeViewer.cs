using HelixToolkit.Wpf;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using WPFCubeViewer.Interfaces;

namespace WPFCubeViewer.Controls
{
    public class WPFCubeViewer : HelixViewport3D
    {

        #region Privates

        IVoxelScript? script;
        Dictionary<int, List<ModelVisual3D>> frameModels = new();
        int frame = 0;

        DispatcherTimer? animationTimer;

        #endregion

        #region DependencyProperties

        /// <summary>
        /// Gets or sets the total extent of the content, typically representing the overall size or length in logical
        /// units.
        /// </summary>
        public int Extent
        {
            get { return (int)GetValue(ExtentProperty); }
            set { SetValue(ExtentProperty, value); }
        }

        public static readonly DependencyProperty ExtentProperty =
            DependencyProperty.Register(nameof(Extent), typeof(int), typeof(WPFCubeViewer), new PropertyMetadata(10, ExtentChanged));

        private static void ExtentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            WPFCubeViewer? viewer = d as WPFCubeViewer;
            viewer?.Reset();
            viewer?.Render();
        }

        /// <summary>
        /// Gets or sets the code associated with this instance.
        /// </summary>
        public String Code
        {
            get { return (String)GetValue(CodeProperty); }
            set { SetValue(CodeProperty, value); }
        }

        public static readonly DependencyProperty CodeProperty =
            DependencyProperty.Register(nameof(Code), typeof(String), typeof(WPFCubeViewer), new PropertyMetadata(null, CodeChanged));

        private static void CodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            WPFCubeViewer? viewer = d as WPFCubeViewer;
            viewer?.Compile();
            viewer?.Render();
        }

        /// <summary>
        /// Gets or sets the error message generated during the compilation process.
        /// </summary>
        public String CompileError
        {
            get { return (String)GetValue(CompileErrorProperty); }
            set { SetValue(CompileErrorProperty, value); }
        }

        public static readonly DependencyProperty CompileErrorProperty =
            DependencyProperty.Register(nameof(CompileError), typeof(String), typeof(WPFCubeViewer), new PropertyMetadata(null));


        /// <summary>
        /// Gets or sets a value that indicates whether animations are enabled for this control.
        /// </summary>
        public bool IsAnimationEnabled
        {
            get { return (bool)GetValue(IsAnimationEnabledProperty); }
            set { SetValue(IsAnimationEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsAnimationEnabledProperty =
            DependencyProperty.Register(nameof(IsAnimationEnabled), typeof(bool), typeof(WPFCubeViewer), new PropertyMetadata(false, IsAnimationEnabledChanged));

        private static void IsAnimationEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            WPFCubeViewer? viewer = d as WPFCubeViewer;
            viewer?.SetAnimation();
        }



        #endregion

        #region Constructor

        public WPFCubeViewer()
        {
            // Init Parameters
            InfiniteSpin = true;
            FixedRotationPoint = new Point3D(0, 0, 0);
            FixedRotationPointEnabled = true;
            IsRotationEnabled = true;
            CameraRotationMode = CameraRotationMode.Trackball;
            ShowCoordinateSystem = true;
            ClipToBounds = false;
            VisualEdgeMode = EdgeMode.Aliased;

            // Set Camera
            Camera = new PerspectiveCamera() { Position = new Point3D(30, 30, 30), LookDirection = new Vector3D(-1, -1, -1), FieldOfView = 45 };

            // Init Timer for Animation
            InitTimer();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Renders the 3D scene by evaluating the current script and updating the visual elements accordingly.
        /// </summary>
        /// <remarks>This method clears existing child visuals and regenerates the scene based on the
        /// current script, frame, and extent values. It creates one visual per color as determined by the script
        /// evaluation. If the script is null, the method performs no action. Call this method to refresh the scene
        /// after modifying the script or related parameters.</remarks>
        public void Render()
        {
            if (script == null) return;

            Children.Clear();
            Children.Add(new SunLight());

            if (IsAnimationEnabled)
            {
                var frm = frameModels[frame];
                if (frm != null)
                {
                    foreach (var m in frm)
                        Children.Add(m);
                }

            }
            else
            {
                var meshes = CreateMeshes();
                var model = CreateModel(meshes);

                foreach (var m in model)
                    Children.Add(m);
            }
        }

        Dictionary<int, MeshGeometry3D> CreateMeshes()
        {
            var meshes = new Dictionary<int, MeshGeometry3D>();

            for (int x = -Extent; x <= Extent; x++)
                for (int y = -Extent; y <= Extent; y++)
                    for (int z = -Extent; z <= Extent; z++)
                    {
                        int c = script?.Eval(x, y, z, frame, Extent) ?? 0;
                        if (c == 0) continue;

                        if (!meshes.TryGetValue(c, out var mesh))
                        {
                            mesh = new MeshGeometry3D();
                            meshes[c] = mesh;
                        }

                        AddCube(mesh, x, y, z);
                    }

            return meshes;
        }

        List<ModelVisual3D> CreateModel(Dictionary<int, MeshGeometry3D> meshes)
        {
            var models = new List<ModelVisual3D>();
            // Create ONE visual per color
            foreach (var kv in meshes)
            {
                var brush = Palette(kv.Key);
                var model = new GeometryModel3D
                {
                    Geometry = kv.Value,
                    Material = new DiffuseMaterial(brush)
                };

                models.Add(new ModelVisual3D { Content = model });
            }
            return models;
        }

        void Reset()
        {
            frame = 0;
            IsAnimationEnabled = false;
        }

        void CreateFrames()
        {
            for (int i = 1; i <= 20; i++)
            {
                var meshes = CreateMeshes();
                var models = CreateModel(meshes);
                frameModels.Add(i, models);
                frame++;
            }
            frame = 0;
        }

        /// <summary>
        /// Adds a cube to the specified mesh geometry at the given coordinates.
        /// </summary>
        /// <remarks>The cube is added with its origin at the specified coordinates and a fixed edge
        /// length of 0.8 units. The method updates the mesh by adding the necessary positions and triangle indices to
        /// represent the cube.</remarks>
        /// <param name="mesh">The mesh geometry to which the cube will be added. Must not be null.</param>
        /// <param name="x">The X-coordinate of the cube's origin.</param>
        /// <param name="y">The Y-coordinate of the cube's origin.</param>
        /// <param name="z">The Z-coordinate of the cube's origin.</param>
        void AddCube(MeshGeometry3D mesh, int x, int y, int z)
        {
            int i = mesh.Positions.Count;
            double add = 0.8;

            // 8 corners
            mesh.Positions.Add(new Point3D(x, y, z));
            mesh.Positions.Add(new Point3D(x + add, y, z));
            mesh.Positions.Add(new Point3D(x + add, y + add, z));
            mesh.Positions.Add(new Point3D(x, y + add, z));

            mesh.Positions.Add(new Point3D(x, y, z + add));
            mesh.Positions.Add(new Point3D(x + add, y, z + add));
            mesh.Positions.Add(new Point3D(x + add, y + add, z + add));
            mesh.Positions.Add(new Point3D(x, y + add, z + add));

            // 12 triangles (6 faces)
            int[] tris =
            {
                0,1,2, 0,2,3, // front
                4,6,5, 4,7,6, // back
                0,3,7, 0,7,4, // left
                1,5,6, 1,6,2, // right
                3,2,6, 3,6,7, // top
                0,4,5, 0,5,1  // bottom
            };

            foreach (var t in tris)
                mesh.TriangleIndices.Add(i + t);
        }

        /// <summary>
        /// Compiles the current script code and prepares it for execution.
        /// </summary>
        /// <remarks>If compilation fails, the error message is set in the CompileError property and the
        /// script is not updated. This method overwrites any previously compiled script instance.</remarks>
        public void Compile()
        {
            Reset();

            try
            {
                string source = $@"
using System;
using WPFCubeViewer.Interfaces;

public class Script : IVoxelScript
{{
    public int Eval(int x, int y, int z, int t, int extent)
    {{
        {Code}
    }}
}}
";

                var syntaxTree = CSharpSyntaxTree.ParseText(source);

                var refs = new[]
                {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IVoxelScript).Assembly.Location)
        };

                var compilation = CSharpCompilation.Create(
                    "VoxelScript",
                    new[] { syntaxTree },
                    refs,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                );

                using var ms = new System.IO.MemoryStream();
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    CompileError = "Compilation failed:\n" + string.Join("\n", result.Diagnostics);
                    return;
                }

                ms.Seek(0, System.IO.SeekOrigin.Begin);
                var asm = Assembly.Load(ms.ToArray());
                var type = asm.GetType("Script");

                if (type != null)
                {
                    script = Activator.CreateInstance(type) as IVoxelScript;
                    CompileError = String.Empty;
                }
            }
            catch (Exception ex)
            {
                CompileError = ex.Message;
                script = null;
            }
        }

        /// <summary>
        /// Returns a predefined brush corresponding to the specified palette index.    
        /// </summary>
        /// <remarks>The returned brush is one of several standard colors, such as white, gray, black, or
        /// various shades. If the specified index is outside the range 1 to 16, the method returns a white
        /// brush.</remarks>
        /// <param name="c">The palette index. Valid values are integers from 1 to 16; other values default to white.</param>
        /// <returns>A <see cref="Brush"/> representing the color associated with the specified palette index.</returns>
        public Brush Palette(int c) => c switch
        {
            1 => Brushes.White,
            2 => Brushes.Gray,
            3 => Brushes.Black,
            4 => Brushes.PeachPuff,
            5 => Brushes.Pink,
            6 => Brushes.Purple,
            7 => Brushes.Red,
            8 => Brushes.Orange,
            9 => Brushes.Yellow,
            10 => Brushes.LightGreen,
            11 => Brushes.Green,
            12 => Brushes.DarkBlue,
            13 => Brushes.Blue,
            14 => Brushes.LightSkyBlue,
            15 => Brushes.Brown,
            16 => Brushes.DarkOrange,
            _ => Brushes.White
        };

        /// <summary>
        /// Initializes and configures the animation timer used to control frame updates.   
        /// </summary>
        /// <remarks>This method sets up a timer to trigger frame rendering at regular intervals. It
        /// should be called before starting the animation to ensure the timer is properly configured.</remarks>
        private void InitTimer()
        {
            // Animation Timer (20 Frames)
            animationTimer = new DispatcherTimer();
            animationTimer.Interval = TimeSpan.FromMilliseconds(40);
            animationTimer.Tick += (s, e) =>
            {
                // Add Frame
                frame++;
                // Render Frame
                Render();
                // If Frame > 20, reset to 0
                if (frame >= 20) frame = 0;
            };
        }

        /// <summary>
        /// Initializes the animation state and starts or stops the animation timer based on the current animation
        /// settings.
        /// </summary>
        /// <remarks>Call this method to reset the animation to its initial frame and ensure the animation
        /// timer reflects the current value of the animation enablement property. This method is typically used
        /// internally to synchronize animation state with user or system settings.</remarks>
        private void SetAnimation()
        {
            frame = 0;
            frameModels = new Dictionary<int, List<ModelVisual3D>>(); ;
            if (IsAnimationEnabled)
            {
                CreateFrames();
                animationTimer?.Start();
            }
            else
            {
                animationTimer?.Stop();
            }
        }

        #endregion

    }
}
