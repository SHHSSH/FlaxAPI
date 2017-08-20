////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2012-2017 Flax Engine. All rights reserved.
////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using FlaxEditor.CustomEditors.Editors;
using FlaxEngine.GUI;

namespace FlaxEditor.CustomEditors
{
    /// <summary>
    /// Main class for Custom Editors used to present selected objects properties and allow to modify them.
    /// </summary>
    /// <seealso cref="FlaxEditor.CustomEditors.LayoutElementsContainer" />
    public class CustomEditorPresenter : LayoutElementsContainer
    {
        /// <summary>
        /// The panel control.
        /// </summary>
        /// <seealso cref="FlaxEngine.GUI.VerticalPanel" />
        public class PresenterPanel : VerticalPanel
        {
            private CustomEditorPresenter _presenter;

            /// <summary>
            /// Initializes a new instance of the <see cref="PresenterPanel"/> class.
            /// </summary>
            /// <param name="presenter">The presenter.</param>
            internal PresenterPanel(CustomEditorPresenter presenter)
            {
                _presenter = presenter;
            }

            /// <inheritdoc />
            public override void Update(float deltaTime)
            {
                // Update editors
                _presenter.Update();

                base.Update(deltaTime);
            }
        }

        /// <summary>
        /// The panel.
        /// </summary>
        public readonly PresenterPanel Panel;

        /// <summary>
        /// The selected objects editor.
        /// </summary>
        protected readonly GenericEditor Editor = new GenericEditor();

        /// <summary>
        /// The selected objects list.
        /// </summary>
        protected readonly ValueContainer Selection = new ValueContainer(null);

        /// <summary>
        /// Occurs when selection gets changed.
        /// </summary>
        public event Action SelectionChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomEditorPresenter"/> class.
        /// </summary>
        public CustomEditorPresenter()
        {
            Panel = new PresenterPanel(this);
        }

        /// <summary>
        /// Selects the specified object.
        /// </summary>
        /// <param name="obj">The object.</param>
        public void Select(object obj)
        {
            if(obj == null)
                throw new ArgumentNullException();
            if (Selection.Count == 1 && Selection[0] == obj)
                return;

            Selection.Clear();
            Selection.Add(obj);

            OnSelectionChanged();
        }

        /// <summary>
        /// Selects the specified objects.
        /// </summary>
        /// <param name="objects">The objects.</param>
        public void Select(IEnumerable<object> objects)
        {
            if (objects == null)
                throw new ArgumentNullException();

            Selection.Clear();
            Selection.AddRange(objects);

            OnSelectionChanged();
        }

        /// <summary>
        /// Clears the selected objects.
        /// </summary>
        public void Deselect()
        {
            if (Selection.Count == 0)
                return;

            Selection.Clear();

            OnSelectionChanged();
        }

        /// <summary>
        /// Builds the editors layout.
        /// </summary>
        /// <inheritdoc />
        protected virtual void BuildLayout()
        {
            // TODO: implement layout elements reusing to reduce memory hit

            // Clear layout
            Panel.IsLayoutLocked = true;
            Panel.DisposeChildren();

            // TODO: find better way to cleanup elements if building layout is a bottleneck
            Children.Clear();
            Editor.Cleanup();

            // Build new one
            if (Selection.Count > 0)
            {
                Editor.Initialize(this, Selection);
            }
            
            Panel.UnlockChildrenRecursive();
            Panel.PerformLayout();
        }

        /// <summary>
        /// Called when selection gets changed.
        /// </summary>
        protected virtual void OnSelectionChanged()
        {
            BuildLayout();
            SelectionChanged?.Invoke();
        }

        internal void Update()
        {
            Editor.RefreshRoot();
        }

        /// <inheritdoc />
        public override ContainerControl ContainerControl => Panel;
    }
}
