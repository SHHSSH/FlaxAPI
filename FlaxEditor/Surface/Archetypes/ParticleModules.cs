// Copyright (c) 2012-2019 Wojciech Figat. All rights reserved.

using System;
using System.Linq;
using FlaxEditor.CustomEditors.Elements;
using FlaxEditor.Surface.Elements;
using FlaxEngine;
using FlaxEngine.GUI;

namespace FlaxEditor.Surface.Archetypes
{
    /// <summary>
    /// Contains archetypes for nodes from the Particle Modules group.
    /// </summary>
    public static class ParticleModules
    {
        /// <summary>
        /// The particle emitter module types.
        /// </summary>
        public enum ModuleType
        {
            /// <summary>
            /// The spawn module.
            /// </summary>
            Spawn,

            /// <summary>
            /// The init module.
            /// </summary>
            Initialize,

            /// <summary>
            /// The update module.
            /// </summary>
            Update,

            /// <summary>
            /// The render module.
            /// </summary>
            Render,
        }

        /// <summary>
        /// The sprite rendering facing modes.
        /// </summary>
        public enum ParticleSpriteFacingMode
        {
            /// <summary>
            /// Particles will face camera position.
            /// </summary>
            FaceCameraPosition,

            /// <summary>
            /// Particles will face camera plane.
            /// </summary>
            FaceCameraPlane,

            /// <summary>
            /// Particles will orient along velocity vector.
            /// </summary>
            AlongVelocity,

            /// <summary>
            /// Particles will use the custom vector for facing.
            /// </summary>
            CustomFacingVector,
        }

        /// <summary>
        /// The particles sorting modes.
        /// </summary>
        public enum ParticleSortMode
        {
            /// <summary>
            /// Do not perform additional sorting prior to rendering.
            /// </summary>
            None,

            /// <summary>
            /// Sorts particles by depth to the view's near plane.
            /// </summary>
            ViewDepth,

            /// <summary>
            /// Sorts particles by distance to the view's origin.
            /// </summary>
            ViewDistance,

            /// <summary>
            /// The custom sorting according to a per particle attribute. Lower values are rendered before higher values.
            /// </summary>
            CustomAscending,

            /// <summary>
            /// The custom sorting according to a per particle attribute. Higher values are rendered before lower values.
            /// </summary>
            CustomDescending,
        }

        /// <summary>
        /// Customized <see cref="SurfaceNode"/> for particle emitter module node.
        /// </summary>
        /// <seealso cref="FlaxEditor.Surface.SurfaceNode" />
        public class ParticleModuleNode : SurfaceNode
        {
            private static readonly Color[] Colors =
            {
                Color.ForestGreen,
                Color.GreenYellow,
                Color.Violet,
                Color.Firebrick,
            };

            private CheckBox _enabled;
            private Rectangle _arrangeButtonRect;
            private bool _arrangeButtonInUse;

            /// <summary>
            /// Gets the particle emitter surface.
            /// </summary>
            public ParticleEmitterSurface ParticleSurface => (ParticleEmitterSurface)Surface;

            /// <summary>
            /// Gets or sets a value indicating whether the module is enabled.
            /// </summary>
            public bool ModuleEnabled
            {
                get => (bool)Values[0];
                set
                {
                    if (value != (bool)Values[0])
                    {
                        SetValue(0, value);
                        _enabled.State = value ? CheckBoxState.Checked : CheckBoxState.Default;
                    }
                }
            }

            /// <summary>
            /// Gets the type of the module.
            /// </summary>
            public ModuleType ModuleType { get; }

            /// <inheritdoc />
            public ParticleModuleNode(uint id, VisjectSurfaceContext context, NodeArchetype nodeArch, GroupArchetype groupArch)
            : base(id, context, nodeArch, groupArch)
            {
                _enabled = new CheckBox(0, 0, true, FlaxEditor.Surface.Constants.NodeCloseButtonSize)
                {
                    TooltipText = "Enabled/disables this module",
                    Parent = this,
                };

                ModuleType = (ModuleType)nodeArch.DefaultValues[1];

                Size = CalculateNodeSize(nodeArch.Size.X, nodeArch.Size.Y);
            }

            private void OnEnabledStateChanged(CheckBox control)
            {
                ModuleEnabled = control.State == CheckBoxState.Checked;
            }

            /// <inheritdoc />
            public override void Draw()
            {
                var style = Style.Current;

                // Header
                var idx = (int)ModuleType;
                var headerRect = new Rectangle(0, 0, Width, 16.0f);
                //Render2D.FillRectangle(headerRect, Color.Red);
                Render2D.DrawText(style.FontMedium, Title, headerRect, style.Foreground, TextAlignment.Center, TextAlignment.Center);

                DrawChildren();

                // Border
                Render2D.DrawRectangle(new Rectangle(1, 0, Width - 2, Height - 1), Colors[idx], 1.5f);

                // Close button
                float alpha = _closeButtonRect.Contains(_mousePosition) ? 1.0f : 0.7f;
                Render2D.DrawSprite(style.Cross, _closeButtonRect, new Color(alpha));

                // Arrange button
                alpha = _arrangeButtonRect.Contains(_mousePosition) ? 1.0f : 0.7f;
                Render2D.DrawSprite(Editor.Instance.Icons.DragBar12, _arrangeButtonRect, _arrangeButtonInUse ? Color.Orange : new Color(alpha));
                if (_arrangeButtonInUse && ArrangeAreaCheck(out _, out var arrangeTargetRect))
                {
                    Render2D.FillRectangle(arrangeTargetRect, Color.Orange * 0.8f);
                }

                // Disabled overlay
                if (!ModuleEnabled)
                {
                    Render2D.FillRectangle(new Rectangle(Vector2.Zero, Size), new Color(0, 0, 0, 0.4f));
                }
            }

            private bool ArrangeAreaCheck(out int index, out Rectangle rect)
            {
                var barSidesExtend = 20.0f;
                var barHeight = 10.0f;
                var barCheckAreaHeight = 40.0f;

                var pos = PointToParent(ref _mousePosition).Y + barCheckAreaHeight * 0.5f;
                var modules = Surface.Nodes.OfType<ParticleModuleNode>().Where(x => x.ModuleType == ModuleType).ToList();
                for (var i = 0; i < modules.Count; i++)
                {
                    var module = modules[i];
                    if (Mathf.IsInRange(pos, module.Top, module.Top + barCheckAreaHeight) || (i == 0 && pos < module.Top))
                    {
                        index = i;
                        var p1 = module.UpperLeft;
                        rect = new Rectangle(PointFromParent(ref p1) - new Vector2(barSidesExtend * 0.5f, barHeight * 0.5f), Width + barSidesExtend, barHeight);
                        return true;
                    }
                }

                var p2 = modules[modules.Count - 1].BottomLeft;
                if (pos > p2.Y)
                {
                    index = modules.Count;
                    rect = new Rectangle(PointFromParent(ref p2) - new Vector2(barSidesExtend * 0.5f, barHeight * 0.5f), Width + barSidesExtend, barHeight);
                    return true;
                }

                index = -1;
                rect = Rectangle.Empty;
                return false;
            }

            /// <inheritdoc />
            public override void OnEndMouseCapture()
            {
                base.OnEndMouseCapture();

                _arrangeButtonInUse = false;
            }

            /// <inheritdoc />
            public override bool OnMouseDown(Vector2 location, MouseButton buttons)
            {
                if (buttons == MouseButton.Left && _arrangeButtonRect.Contains(ref location))
                {
                    _arrangeButtonInUse = true;
                    Focus();
                    StartMouseCapture();
                    return true;
                }

                return base.OnMouseDown(location, buttons);
            }

            /// <inheritdoc />
            public override bool OnMouseUp(Vector2 location, MouseButton buttons)
            {
                if (buttons == MouseButton.Left && _arrangeButtonInUse)
                {
                    _arrangeButtonInUse = false;
                    EndMouseCapture();

                    if (ArrangeAreaCheck(out var index, out _))
                    {
                        var modules = Surface.Nodes.OfType<ParticleModuleNode>().Where(x => x.ModuleType == ModuleType).ToList();

                        foreach (var module in modules)
                        {
                            Surface.Nodes.Remove(module);
                        }

                        int oldIndex = modules.IndexOf(this);
                        modules.RemoveAt(oldIndex);
                        if (index < 0 || index >= modules.Count)
                            modules.Add(this);
                        else
                            modules.Insert(index, this);

                        foreach (var module in modules)
                        {
                            Surface.Nodes.Add(module);
                        }

                        foreach (var module in modules)
                        {
                            module.IndexInParent = int.MaxValue;
                        }

                        ParticleSurface.ArrangeModulesNodes();
                        Surface.MarkAsEdited();
                    }
                }

                return base.OnMouseUp(location, buttons);
            }

            /// <inheritdoc />
            public override void OnLostFocus()
            {
                if (_arrangeButtonInUse)
                {
                    _arrangeButtonInUse = false;
                    EndMouseCapture();
                }

                base.OnLostFocus();
            }

            /// <inheritdoc />
            protected sealed override Vector2 CalculateNodeSize(float width, float height)
            {
                return new Vector2(width + FlaxEditor.Surface.Constants.NodeMarginX * 2, height + FlaxEditor.Surface.Constants.NodeMarginY * 2 + 16.0f);
            }

            /// <inheritdoc />
            protected override void UpdateRectangles()
            {
                const float headerSize = 16.0f;
                const float closeButtonMargin = FlaxEditor.Surface.Constants.NodeCloseButtonMargin;
                const float closeButtonSize = FlaxEditor.Surface.Constants.NodeCloseButtonSize;
                _headerRect = new Rectangle(0, 0, Width, headerSize);
                _closeButtonRect = new Rectangle(Width - closeButtonSize - closeButtonMargin, closeButtonMargin, closeButtonSize, closeButtonSize);
                _footerRect = Rectangle.Empty;
                _enabled.Location = new Vector2(_closeButtonRect.X - _enabled.Width - 2, _closeButtonRect.Y);
                _arrangeButtonRect = new Rectangle(_enabled.X - closeButtonSize - closeButtonMargin, closeButtonMargin, closeButtonSize, closeButtonSize);
            }

            /// <inheritdoc />
            public override void OnSpawned()
            {
                base.OnSpawned();

                ParticleSurface.ArrangeModulesNodes();
            }

            /// <inheritdoc />
            public override void OnSurfaceLoaded()
            {
                _enabled.Checked = ModuleEnabled;
                _enabled.StateChanged += OnEnabledStateChanged;

                base.OnSurfaceLoaded();
            }

            /// <inheritdoc />
            public override void OnDeleted()
            {
                ParticleSurface.ArrangeModulesNodes();

                base.OnDeleted();
            }

            /// <inheritdoc />
            public override void Dispose()
            {
                _enabled = null;

                base.Dispose();
            }
        }

        /// <summary>
        /// The particle emitter module that can write to the particle attribute.
        /// </summary>
        /// <seealso cref="FlaxEditor.Surface.Archetypes.ParticleModules.ParticleModuleNode" />
        public class SetParticleAttributeModuleNode : ParticleModuleNode
        {
            /// <inheritdoc />
            public SetParticleAttributeModuleNode(uint id, VisjectSurfaceContext context, NodeArchetype nodeArch, GroupArchetype groupArch)
            : base(id, context, nodeArch, groupArch)
            {
            }

            /// <inheritdoc />
            public override void OnSurfaceLoaded()
            {
                base.OnSurfaceLoaded();

                UpdateOutputBoxType();
            }

            /// <inheritdoc />
            public override void SetValue(int index, object value)
            {
                base.SetValue(index, value);

                // Update on type change
                if (index == 3)
                    UpdateOutputBoxType();
            }

            private void UpdateOutputBoxType()
            {
                ConnectionType type;
                switch ((Particles.ValueTypes)Values[3])
                {
                case Particles.ValueTypes.Float:
                    type = ConnectionType.Float;
                    break;
                case Particles.ValueTypes.Vector2:
                    type = ConnectionType.Vector2;
                    break;
                case Particles.ValueTypes.Vector3:
                    type = ConnectionType.Vector3;
                    break;
                case Particles.ValueTypes.Vector4:
                    type = ConnectionType.Vector4;
                    break;
                case Particles.ValueTypes.Int:
                    type = ConnectionType.Integer;
                    break;
                case Particles.ValueTypes.Uint:
                    type = ConnectionType.UnsignedInteger;
                    break;
                default: throw new ArgumentOutOfRangeException();
                }
                GetBox(0).CurrentType = type;
            }
        }

        /// <summary>
        /// The particle emitter module applies the sprite orientation.
        /// </summary>
        /// <seealso cref="FlaxEditor.Surface.Archetypes.ParticleModules.ParticleModuleNode" />
        public class OrientSpriteNode : ParticleModuleNode
        {
            /// <inheritdoc />
            public OrientSpriteNode(uint id, VisjectSurfaceContext context, NodeArchetype nodeArch, GroupArchetype groupArch)
            : base(id, context, nodeArch, groupArch)
            {
            }

            /// <inheritdoc />
            public override void OnSurfaceLoaded()
            {
                base.OnSurfaceLoaded();

                UpdateInputBox();
            }

            /// <inheritdoc />
            public override void SetValue(int index, object value)
            {
                base.SetValue(index, value);

                // Update on mode change
                if (index == 2)
                    UpdateInputBox();
            }

            private void UpdateInputBox()
            {
                GetBox(0).Enabled = (ParticleSpriteFacingMode)Values[2] == ParticleSpriteFacingMode.CustomFacingVector;
            }
        }

        /// <summary>
        /// The particle emitter module that can sort particles.
        /// </summary>
        /// <seealso cref="FlaxEditor.Surface.Archetypes.ParticleModules.ParticleModuleNode" />
        public class SortModuleNode : ParticleModuleNode
        {
            /// <inheritdoc />
            public SortModuleNode(uint id, VisjectSurfaceContext context, NodeArchetype nodeArch, GroupArchetype groupArch)
            : base(id, context, nodeArch, groupArch)
            {
            }

            /// <inheritdoc />
            public override void OnSurfaceLoaded()
            {
                base.OnSurfaceLoaded();

                UpdateTextBox();
            }

            /// <inheritdoc />
            public override void SetValue(int index, object value)
            {
                base.SetValue(index, value);

                // Update on sort mode change
                if (index == 2)
                    UpdateTextBox();
            }

            private void UpdateTextBox()
            {
                var mode = (ParticleSortMode)Values[2];
                ((TextBoxView)Elements[2]).Visible = mode == ParticleSortMode.CustomAscending || mode == ParticleSortMode.CustomDescending;
            }
        }

        private static SurfaceNode CreateParticleModuleNode(uint id, VisjectSurfaceContext context, NodeArchetype arch, GroupArchetype groupArch)
        {
            return new ParticleModuleNode(id, context, arch, groupArch);
        }

        private static SurfaceNode CreateSetParticleAttributeModuleNode(uint id, VisjectSurfaceContext context, NodeArchetype arch, GroupArchetype groupArch)
        {
            return new SetParticleAttributeModuleNode(id, context, arch, groupArch);
        }

        private static SurfaceNode CreateOrientSpriteNode(uint id, VisjectSurfaceContext context, NodeArchetype arch, GroupArchetype groupArch)
        {
            return new OrientSpriteNode(id, context, arch, groupArch);
        }

        private static SurfaceNode CreateSortNode(uint id, VisjectSurfaceContext context, NodeArchetype arch, GroupArchetype groupArch)
        {
            return new SortModuleNode(id, context, arch, groupArch);
        }

        /// <summary>
        /// The particle module node elements offset applied to controls to reduce default surface node header thickness.
        /// </summary>
        private const float NodeElementsOffset = 16.0f - Surface.Constants.NodeHeaderSize;

        private const NodeFlags DefaultModuleFlags = NodeFlags.ParticleEmitterGraph | NodeFlags.NoSpawnViaGUI | NodeFlags.NoMove;

        private static NodeArchetype GetParticleAttribute(ModuleType moduleType, ushort typeId, string title, string description, ConnectionType type, object defaultValue)
        {
            return new NodeArchetype
            {
                TypeID = typeId,
                Create = CreateParticleModuleNode,
                Title = title,
                Description = description,
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 1 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new[]
                {
                    true,
                    (int)moduleType,
                    defaultValue,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, string.Empty, true, type, 0, 2),
                },
            };
        }

        /// <summary>
        /// The nodes for that group.
        /// </summary>
        public static NodeArchetype[] Nodes =
        {
            // Spawn Modules
            new NodeArchetype
            {
                TypeID = 100,
                Create = CreateParticleModuleNode,
                Title = "Constant Spawn Rate",
                Description = "Emits constant amount of particles per second, depending of the rate property",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Spawn,
                    10.0f,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Rate", true, ConnectionType.Float, 0, 2),
                },
            },
            new NodeArchetype
            {
                TypeID = 101,
                Create = CreateParticleModuleNode,
                Title = "Single Burst",
                Description = "Emits a given amount of particles on start",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Spawn,
                    10.0f,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Count", true, ConnectionType.Float, 0, 2),
                },
            },
            new NodeArchetype
            {
                TypeID = 102,
                Create = CreateParticleModuleNode,
                Title = "Periodic Burst",
                Description = "Emits particles in periods of time",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, Surface.Constants.LayoutOffsetY * 2),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Spawn,
                    10.0f,
                    2.0f,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Count", true, ConnectionType.Float, 0, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, "Delay", true, ConnectionType.Float, 1, 3),
                },
            },
            new NodeArchetype
            {
                TypeID = 103,
                Create = CreateParticleModuleNode,
                Title = "Periodic Burst (range)",
                Description = "Emits random amount of particles in random periods of time (customizable ranges)",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, Surface.Constants.LayoutOffsetY * 2),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Spawn,
                    new Vector2(5.0f, 10.0f),
                    new Vector2(1.0f, 2.0f),
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Count (min, max)", true, ConnectionType.Vector2, 0, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, "Delay (min, max)", true, ConnectionType.Vector2, 1, 3),
                },
            },
            // TODO: Spawn On Event

            // Initialize
            new NodeArchetype
            {
                TypeID = 200,
                Create = CreateSetParticleAttributeModuleNode,
                Title = "Set Attribute",
                Description = "Sets the particle attribute value",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 2 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Initialize,
                    "Color",
                    (int)Particles.ValueTypes.Vector4,
                    Color.White,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.ComboBox(0, -10.0f, 80, 3, typeof(Particles.ValueTypes)),
                    NodeElementArchetype.Factory.TextBox(90, -10.0f, 120, TextBox.DefaultHeight, 2, false),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, string.Empty, true, ConnectionType.All, 0, 4),
                },
            },
            new NodeArchetype
            {
                TypeID = 201,
                Create = CreateOrientSpriteNode,
                Title = "Orient Sprite",
                Description = "Orientates the sprite particles in the space",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 2 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Initialize,
                    (int)ParticleSpriteFacingMode.FaceCameraPosition,
                    Vector3.Forward,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.ComboBox(0, -10.0f, 160, 2, typeof(ParticleSpriteFacingMode)),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, "Custom Vector", true, ConnectionType.Vector3, 0, 3),
                },
            },
            new NodeArchetype
            {
                TypeID = 202,
                Create = CreateParticleModuleNode,
                Title = "Position (sphere surface)",
                Description = "Places the particles on surface of the sphere",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 3 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Initialize,
                    Vector3.Zero,
                    100.0f,
                    360.0f,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Center", true, ConnectionType.Vector3, 0, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, "Radius", true, ConnectionType.Float, 1, 3),
                    NodeElementArchetype.Factory.Input(-0.5f + 2.0f, "Arc", true, ConnectionType.Float, 2, 4),
                },
            },
            new NodeArchetype
            {
                TypeID = 203,
                Create = CreateParticleModuleNode,
                Title = "Position (plane)",
                Description = "Places the particles on plane",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 2 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Initialize,
                    Vector3.Zero,
                    new Vector2(100.0f, 100.0f),
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Center", true, ConnectionType.Vector3, 0, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, "Size", true, ConnectionType.Vector2, 1, 3),
                },
            },
            new NodeArchetype
            {
                TypeID = 204,
                Create = CreateParticleModuleNode,
                Title = "Position (circle)",
                Description = "Places the particles on arc of the circle",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 3 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Initialize,
                    Vector3.Zero,
                    100.0f,
                    360.0f,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Center", true, ConnectionType.Vector3, 0, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, "Radius", true, ConnectionType.Float, 1, 3),
                    NodeElementArchetype.Factory.Input(-0.5f + 2.0f, "Arc", true, ConnectionType.Float, 2, 4),
                },
            },
            new NodeArchetype
            {
                TypeID = 205,
                Create = CreateParticleModuleNode,
                Title = "Position (disc)",
                Description = "Places the particles on surface of the disc",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 3 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Initialize,
                    Vector3.Zero,
                    100.0f,
                    360.0f,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Center", true, ConnectionType.Vector3, 0, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, "Radius", true, ConnectionType.Float, 1, 3),
                    NodeElementArchetype.Factory.Input(-0.5f + 2.0f, "Arc", true, ConnectionType.Float, 2, 4),
                },
            },
            new NodeArchetype
            {
                TypeID = 206,
                Create = CreateParticleModuleNode,
                Title = "Position (box surface)",
                Description = "Places the particles on top of the axis-aligned box surface",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 3 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Initialize,
                    Vector3.Zero,
                    new Vector3(100.0f),
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Center", true, ConnectionType.Vector3, 0, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, "Size", true, ConnectionType.Vector3, 1, 3),
                },
            },
            new NodeArchetype
            {
                TypeID = 207,
                Create = CreateParticleModuleNode,
                Title = "Position (box volume)",
                Description = "Places the particles inside the axis-aligned box volume",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 3 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Initialize,
                    Vector3.Zero,
                    new Vector3(100.0f),
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Center", true, ConnectionType.Vector3, 0, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, "Size", true, ConnectionType.Vector3, 1, 3),
                },
            },
            new NodeArchetype
            {
                TypeID = 208,
                Create = CreateParticleModuleNode,
                Title = "Position (cylinder)",
                Description = "Places the particles on the cylinder sides",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 4 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Initialize,
                    Vector3.Zero,
                    100.0f,
                    200.0f,
                    360.0f,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Center", true, ConnectionType.Vector3, 0, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, "Radius", true, ConnectionType.Float, 1, 3),
                    NodeElementArchetype.Factory.Input(-0.5f + 2.0f, "Height", true, ConnectionType.Float, 2, 4),
                    NodeElementArchetype.Factory.Input(-0.5f + 3.0f, "Arc", true, ConnectionType.Float, 3, 5),
                },
            },
            new NodeArchetype
            {
                TypeID = 209,
                Create = CreateParticleModuleNode,
                Title = "Position (line)",
                Description = "Places the particles on the line",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 2 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Initialize,
                    Vector3.Zero,
                    new Vector3(100.0f, 0, 0),
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Start", true, ConnectionType.Vector3, 0, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, "End", true, ConnectionType.Vector3, 1, 3),
                },
            },
            new NodeArchetype
            {
                TypeID = 210,
                Create = CreateParticleModuleNode,
                Title = "Position (torus)",
                Description = "Places the particles on a surface of the torus",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 4 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Initialize,
                    Vector3.Zero,
                    100.0f,
                    20.0f,
                    360.0f,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Center", true, ConnectionType.Vector3, 0, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, "Radius", true, ConnectionType.Float, 1, 3),
                    NodeElementArchetype.Factory.Input(-0.5f + 2.0f, "Thickness", true, ConnectionType.Float, 2, 4),
                    NodeElementArchetype.Factory.Input(-0.5f + 3.0f, "Arc", true, ConnectionType.Float, 3, 5),
                },
            },
            new NodeArchetype
            {
                TypeID = 211,
                Create = CreateParticleModuleNode,
                Title = "Position (sphere volume)",
                Description = "Places the particles inside of the sphere",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 3 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Initialize,
                    Vector3.Zero,
                    100.0f,
                    360.0f,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Center", true, ConnectionType.Vector3, 0, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, "Radius", true, ConnectionType.Float, 1, 3),
                    NodeElementArchetype.Factory.Input(-0.5f + 2.0f, "Arc", true, ConnectionType.Float, 2, 4),
                },
            },
            // TODO: Position (depth)
            // TODO: Init Seed
            // TODO: Inherit Position/Velocity/..
            GetParticleAttribute(ModuleType.Initialize, 250, "Set Position", "Sets the particle position", ConnectionType.Vector3, Vector3.Zero),
            GetParticleAttribute(ModuleType.Initialize, 251, "Set Lifetime", "Sets the particle lifetime (in seconds)", ConnectionType.Float, 10.0f),
            GetParticleAttribute(ModuleType.Initialize, 252, "Set Age", "Sets the particle age (in seconds)", ConnectionType.Float, 0.0f),
            GetParticleAttribute(ModuleType.Initialize, 253, "Set Color", "Sets the particle color (RGBA)", ConnectionType.Vector4, Color.White),
            GetParticleAttribute(ModuleType.Initialize, 254, "Set Velocity", "Sets the particle velocity (position delta per second)", ConnectionType.Vector3, Vector3.Zero),
            GetParticleAttribute(ModuleType.Initialize, 255, "Set Sprite Size", "Sets the particle size (width and height of the sprite)", ConnectionType.Vector2, new Vector2(10.0f, 10.0f)),
            GetParticleAttribute(ModuleType.Initialize, 256, "Set Mass", "Sets the particle mass (in kilograms)", ConnectionType.Float, 1.0f),
            GetParticleAttribute(ModuleType.Initialize, 257, "Set Rotation", "Sets the particle rotation (in XYZ)", ConnectionType.Vector3, Vector3.Zero),
            GetParticleAttribute(ModuleType.Initialize, 258, "Set Angular Velocity", "Sets the angular particle velocity (rotation delta per second)", ConnectionType.Vector3, Vector3.Zero),

            // Update Modules
            new NodeArchetype
            {
                TypeID = 300,
                Create = CreateParticleModuleNode,
                Title = "Update Age",
                Description = "Increases particle age every frame, based on delta time",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 0),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Update,
                },
            },
            new NodeArchetype
            {
                TypeID = 301,
                Create = CreateParticleModuleNode,
                Title = "Gravity",
                Description = "Applies the gravity force to particle velocity",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Update,
                    new Vector3(0, -981.0f, 0),
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Force", true, ConnectionType.Vector3, 0, 2),
                },
            },
            new NodeArchetype
            {
                TypeID = 302,
                Create = CreateSetParticleAttributeModuleNode,
                Title = "Set Attribute",
                Description = "Sets the particle attribute value",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 2 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Update,
                    "Color",
                    (int)Particles.ValueTypes.Vector4,
                    Color.White,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.ComboBox(0, -10.0f, 80, 3, typeof(Particles.ValueTypes)),
                    NodeElementArchetype.Factory.TextBox(90, -10.0f, 120, TextBox.DefaultHeight, 2, false),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, string.Empty, true, ConnectionType.All, 0, 4),
                },
            },
            new NodeArchetype
            {
                TypeID = 303,
                Create = CreateOrientSpriteNode,
                Title = "Orient Sprite",
                Description = "Orientates the sprite particles in the space",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 2 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Update,
                    (int)ParticleSpriteFacingMode.FaceCameraPosition,
                    Vector3.Forward,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.ComboBox(0, -10.0f, 160, 2, typeof(ParticleSpriteFacingMode)),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, "Custom Vector", true, ConnectionType.Vector3, 0, 3),
                },
            },
            new NodeArchetype
            {
                TypeID = 304,
                Create = CreateParticleModuleNode,
                Title = "Force",
                Description = "Applies the force vector to particle velocity",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Update,
                    new Vector3(100.0f, 0, 0),
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Force", true, ConnectionType.Vector3, 0, 2),
                },
            },
            new NodeArchetype
            {
                TypeID = 305,
                Create = CreateParticleModuleNode,
                Title = "Conform to Sphere",
                Description = "Applies the force vector to particles to conform around sphere",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 6 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Update,
                    Vector3.Zero,
                    100.0f,
                    5.0f,
                    2000.0f,
                    1.0f,
                    5000.0f,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Sphere Center", true, ConnectionType.Vector3, 0, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, "Sphere Radius", true, ConnectionType.Float, 1, 3),
                    NodeElementArchetype.Factory.Input(-0.5f + 2.0f, "Attraction Speed", true, ConnectionType.Float, 2, 4),
                    NodeElementArchetype.Factory.Input(-0.5f + 3.0f, "Attraction Force", true, ConnectionType.Float, 3, 5),
                    NodeElementArchetype.Factory.Input(-0.5f + 4.0f, "Stick Distance", true, ConnectionType.Float, 4, 6),
                    NodeElementArchetype.Factory.Input(-0.5f + 5.0f, "Stick Force", true, ConnectionType.Float, 5, 7),
                },
            },
            new NodeArchetype
            {
                TypeID = 306,
                Create = CreateParticleModuleNode,
                Title = "Kill (sphere)",
                Description = "Kills particles that enter/leave the sphere",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 3 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Update,
                    Vector3.Zero,
                    100.0f,
                    false,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Sphere Center", true, ConnectionType.Vector3, 0, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, "Sphere Radius", true, ConnectionType.Float, 1, 3),
                    NodeElementArchetype.Factory.Text(20.0f, (-0.5f + 2.0f) * Surface.Constants.LayoutOffsetY, "Invert"),
                    NodeElementArchetype.Factory.Bool(0, (-0.5f + 2.0f) * Surface.Constants.LayoutOffsetY, 4),
                },
            },
            new NodeArchetype
            {
                TypeID = 307,
                Create = CreateParticleModuleNode,
                Title = "Kill (box)",
                Description = "Kills particles that enter/leave the axis-aligned box",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 3 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Update,
                    Vector3.Zero,
                    new Vector3(100.0f),
                    false,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Box Center", true, ConnectionType.Vector3, 0, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1.0f, "Box Size", true, ConnectionType.Vector3, 1, 3),
                    NodeElementArchetype.Factory.Text(20.0f, (-0.5f + 2.0f) * Surface.Constants.LayoutOffsetY, "Invert"),
                    NodeElementArchetype.Factory.Bool(0, (-0.5f + 2.0f) * Surface.Constants.LayoutOffsetY, 4),
                },
            },
            new NodeArchetype
            {
                TypeID = 308,
                Create = CreateParticleModuleNode,
                Title = "Kill (custom)",
                Description = "Kills particles based on a custom boolean rule",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 1 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Update,
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f, "Kill", true, ConnectionType.Bool, 0),
                },
            },
            new NodeArchetype
            {
                TypeID = 330,
                Create = CreateParticleModuleNode,
                Title = "Collision (plane)",
                Description = "Collides particles with the plane",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 8 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Update,
                    false, // Invert
                    0.0f, // Radius
                    0.0f, // Roughness 
                    0.1f, // Elasticity 
                    0.0f, // Friction 
                    0.0f, // Lifetime Loss 
                    Vector3.Zero, // Plane Position
                    Vector3.Up, // Plane Normal
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Text(20.0f, -0.5f * Surface.Constants.LayoutOffsetY, "Invert"),
                    NodeElementArchetype.Factory.Bool(0, -0.5f * Surface.Constants.LayoutOffsetY, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1, "Radius", true, ConnectionType.Float, 0, 3),
                    NodeElementArchetype.Factory.Input(-0.5f + 2, "Roughness", true, ConnectionType.Float, 1, 4),
                    NodeElementArchetype.Factory.Input(-0.5f + 3, "Elasticity", true, ConnectionType.Float, 2, 5),
                    NodeElementArchetype.Factory.Input(-0.5f + 4, "Friction", true, ConnectionType.Float, 3, 6),
                    NodeElementArchetype.Factory.Input(-0.5f + 5, "Lifetime Loss", true, ConnectionType.Float, 4, 7),

                    NodeElementArchetype.Factory.Input(-0.5f + 6, "Plane Position", true, ConnectionType.Vector3, 5, 8),
                    NodeElementArchetype.Factory.Input(-0.5f + 7, "Plane Normal", true, ConnectionType.Vector3, 6, 9),
                },
            },
            new NodeArchetype
            {
                TypeID = 331,
                Create = CreateParticleModuleNode,
                Title = "Collision (sphere)",
                Description = "Collides particles with the sphere",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 8 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Update,
                    false, // Invert
                    0.0f, // Radius
                    0.0f, // Roughness 
                    0.1f, // Elasticity 
                    0.0f, // Friction 
                    0.0f, // Lifetime Loss 
                    Vector3.Zero, // Sphere Position
                    100.0f, // Sphere Radius
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Text(20.0f, -0.5f * Surface.Constants.LayoutOffsetY, "Invert"),
                    NodeElementArchetype.Factory.Bool(0, -0.5f * Surface.Constants.LayoutOffsetY, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1, "Radius", true, ConnectionType.Float, 0, 3),
                    NodeElementArchetype.Factory.Input(-0.5f + 2, "Roughness", true, ConnectionType.Float, 1, 4),
                    NodeElementArchetype.Factory.Input(-0.5f + 3, "Elasticity", true, ConnectionType.Float, 2, 5),
                    NodeElementArchetype.Factory.Input(-0.5f + 4, "Friction", true, ConnectionType.Float, 3, 6),
                    NodeElementArchetype.Factory.Input(-0.5f + 5, "Lifetime Loss", true, ConnectionType.Float, 4, 7),

                    NodeElementArchetype.Factory.Input(-0.5f + 6, "Sphere Position", true, ConnectionType.Vector3, 5, 8),
                    NodeElementArchetype.Factory.Input(-0.5f + 7, "Sphere Radius", true, ConnectionType.Float, 6, 9),
                },
            },
            new NodeArchetype
            {
                TypeID = 332,
                Create = CreateParticleModuleNode,
                Title = "Collision (box)",
                Description = "Collides particles with the box",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 8 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Update,
                    false, // Invert
                    0.0f, // Radius
                    0.0f, // Roughness 
                    0.1f, // Elasticity 
                    0.0f, // Friction 
                    0.0f, // Lifetime Loss 
                    Vector3.Zero, // Box Position
                    new Vector3(100.0f), // Box Size
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Text(20.0f, -0.5f * Surface.Constants.LayoutOffsetY, "Invert"),
                    NodeElementArchetype.Factory.Bool(0, -0.5f * Surface.Constants.LayoutOffsetY, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1, "Radius", true, ConnectionType.Float, 0, 3),
                    NodeElementArchetype.Factory.Input(-0.5f + 2, "Roughness", true, ConnectionType.Float, 1, 4),
                    NodeElementArchetype.Factory.Input(-0.5f + 3, "Elasticity", true, ConnectionType.Float, 2, 5),
                    NodeElementArchetype.Factory.Input(-0.5f + 4, "Friction", true, ConnectionType.Float, 3, 6),
                    NodeElementArchetype.Factory.Input(-0.5f + 5, "Lifetime Loss", true, ConnectionType.Float, 4, 7),

                    NodeElementArchetype.Factory.Input(-0.5f + 6, "Box Position", true, ConnectionType.Vector3, 5, 8),
                    NodeElementArchetype.Factory.Input(-0.5f + 7, "Box Size", true, ConnectionType.Vector3, 6, 9),
                },
            },
            new NodeArchetype
            {
                TypeID = 333,
                Create = CreateParticleModuleNode,
                Title = "Collision (cylinder)",
                Description = "Collides particles with the cylinder",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 9 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Update,
                    false, // Invert
                    0.0f, // Radius
                    0.0f, // Roughness 
                    0.1f, // Elasticity 
                    0.0f, // Friction 
                    0.0f, // Lifetime Loss 
                    Vector3.Zero, // Cylinder Position
                    100.0f, // Cylinder Height
                    50.0f, // Cylinder Radius
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Text(20.0f, -0.5f * Surface.Constants.LayoutOffsetY, "Invert"),
                    NodeElementArchetype.Factory.Bool(0, -0.5f * Surface.Constants.LayoutOffsetY, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1, "Radius", true, ConnectionType.Float, 0, 3),
                    NodeElementArchetype.Factory.Input(-0.5f + 2, "Roughness", true, ConnectionType.Float, 1, 4),
                    NodeElementArchetype.Factory.Input(-0.5f + 3, "Elasticity", true, ConnectionType.Float, 2, 5),
                    NodeElementArchetype.Factory.Input(-0.5f + 4, "Friction", true, ConnectionType.Float, 3, 6),
                    NodeElementArchetype.Factory.Input(-0.5f + 5, "Lifetime Loss", true, ConnectionType.Float, 4, 7),

                    NodeElementArchetype.Factory.Input(-0.5f + 6, "Cylinder Position", true, ConnectionType.Vector3, 5, 8),
                    NodeElementArchetype.Factory.Input(-0.5f + 7, "Cylinder Height", true, ConnectionType.Float, 6, 9),
                    NodeElementArchetype.Factory.Input(-0.5f + 8, "Cylinder Radius", true, ConnectionType.Float, 7, 10),
                },
            },
            // TODO: Collision (depth)
            GetParticleAttribute(ModuleType.Update, 350, "Set Position", "Sets the particle position", ConnectionType.Vector3, Vector3.Zero),
            GetParticleAttribute(ModuleType.Update, 351, "Set Lifetime", "Sets the particle lifetime (in seconds)", ConnectionType.Float, 10.0f),
            GetParticleAttribute(ModuleType.Update, 352, "Set Age", "Sets the particle age (in seconds)", ConnectionType.Float, 0.0f),
            GetParticleAttribute(ModuleType.Update, 353, "Set Color", "Sets the particle color (RGBA)", ConnectionType.Vector4, Color.White),
            GetParticleAttribute(ModuleType.Update, 354, "Set Velocity", "Sets the particle velocity (position delta per second)", ConnectionType.Vector3, Vector3.Zero),
            GetParticleAttribute(ModuleType.Update, 355, "Set Sprite Size", "Sets the particle size (width and height of the sprite)", ConnectionType.Vector2, new Vector2(10.0f, 10.0f)),
            GetParticleAttribute(ModuleType.Update, 356, "Set Mass", "Sets the particle mass (in kilograms)", ConnectionType.Float, 1.0f),
            GetParticleAttribute(ModuleType.Update, 357, "Set Rotation", "Sets the particle rotation (in XYZ)", ConnectionType.Vector3, Vector3.Zero),
            GetParticleAttribute(ModuleType.Update, 358, "Set Angular Velocity", "Sets the angular particle velocity (rotation delta per second)", ConnectionType.Vector3, Vector3.Zero),

            // Render Modules
            new NodeArchetype
            {
                TypeID = 400,
                Create = CreateParticleModuleNode,
                Title = "Sprite Rendering",
                Description = "Draws quad-shaped sprite for every particle",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 80),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Render,
                    Guid.Empty,
                },
                Elements = new[]
                {
                    // Material
                    NodeElementArchetype.Factory.Text(0, -10, "Material", 80.0f, 16.0f, "The material used for sprites rendering (quads). It must have Domain set to Particle."),
                    NodeElementArchetype.Factory.Asset(80, -10, 2, ContentDomain.Material),
                },
            },
            new NodeArchetype
            {
                TypeID = 401,
                Create = CreateParticleModuleNode,
                Title = "Light Rendering",
                Description = "Renders the lights at the particles positions",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 3 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Render,
                    Color.White, // Color
                    1000.0f, // Brightness
                    8.0f, // Fall Off Exponent
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(-0.5f + 0, "Color", true, ConnectionType.Vector4, 0, 2),
                    NodeElementArchetype.Factory.Input(-0.5f + 1, "Radius", true, ConnectionType.Float, 1, 3),
                    NodeElementArchetype.Factory.Input(-0.5f + 2, "Fall Off Exponent", true, ConnectionType.Float, 2, 4),
                },
            },
            new NodeArchetype
            {
                TypeID = 402,
                Create = CreateSortNode,
                Title = "Sort",
                Description = "Sorts the particles for the rendering",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 2 * Surface.Constants.LayoutOffsetY),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Render,
                    (int)ParticleSortMode.ViewDistance, // Sort Mode
                    "Age", // Custom Attribute
                },
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Text(0, -10.0f, "Mode:", 40),
                    NodeElementArchetype.Factory.ComboBox(40, -10.0f, 140, 2, typeof(ParticleSortMode)),
                    NodeElementArchetype.Factory.TextBox(185, -10.0f, 100, TextBox.DefaultHeight, 3, false),
                },
            },
            new NodeArchetype
            {
                TypeID = 403,
                Create = CreateParticleModuleNode,
                Title = "Model Rendering",
                Description = "Draws model for every particle",
                Flags = DefaultModuleFlags,
                Size = new Vector2(200, 160),
                DefaultValues = new object[]
                {
                    true,
                    (int)ModuleType.Render,
                    Guid.Empty,
                    Guid.Empty,
                },
                Elements = new[]
                {
                    // Model
                    NodeElementArchetype.Factory.Text(0, -10, "Model", 80.0f, 16.0f, "model material used for rendering."),
                    NodeElementArchetype.Factory.Asset(80, -10, 2, ContentDomain.Model),
                    // Material
                    NodeElementArchetype.Factory.Text(0, -10 + 70, "Material", 80.0f, 16.0f, "The material used for models rendering. It must have Domain set to Particle."),
                    NodeElementArchetype.Factory.Asset(80, -10 + 70, 3, ContentDomain.Material),
                },
            },
            // TODO: Ribbon Rendering
        };
    }
}
