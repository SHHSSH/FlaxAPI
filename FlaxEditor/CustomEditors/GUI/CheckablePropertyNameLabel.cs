////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2012-2018 Flax Engine. All rights reserved.
////////////////////////////////////////////////////////////////////////////////////

using System;
using FlaxEngine;
using FlaxEngine.Assertions;
using FlaxEngine.GUI;

namespace FlaxEditor.CustomEditors.GUI
{
    /// <summary>
    /// Custom property name label that contains a checkbox used to enable/disable a property.
    /// </summary>
    /// <seealso cref="FlaxEditor.CustomEditors.GUI.PropertyNameLabel" />
    public class CheckablePropertyNameLabel : PropertyNameLabel
    {
        /// <summary>
        /// The check box.
        /// </summary>
        public readonly CheckBox CheckBox;
        
        /// <summary>
        /// Event fired when 'checked' state gets changed.
        /// </summary>
        public event Action<CheckablePropertyNameLabel> CheckChanged;

        /// <inheritdoc />
        public CheckablePropertyNameLabel(string name)
            : base(name)
        {
            CheckBox = new CheckBox(2, 2)
            {
                Checked = true,
                Size = new Vector2(14),
                Parent = this
            };
            CheckBox.CheckChanged += OnCheckChanged;
            Margin = new Margin(CheckBox.Right + 4, 0, 0, 0);
        }

        private void OnCheckChanged()
        {
            CheckChanged?.Invoke(this);
            UpdateStyle();
        }

        /// <summary>
        /// Updates the label style.
        /// </summary>
        protected virtual void UpdateStyle()
        {
            bool check = CheckBox.Checked;

            // Update label text color
            TextColor = check ? Color.White : new Color(0.6f);

            // Update child controls enabled state
            if (FirstChildControlIndex >= 0 && Parent is PropertiesList propertiesList)
            {
                var controls = propertiesList.Children;
                var labels = propertiesList.Element.Labels;
                var thisIndex = labels.IndexOf(this);
                Assert.AreNotEqual(-1, thisIndex, "Invalid label linkage.");
                int childControlsCount = 0;
                if (thisIndex + 1 < labels.Count)
                    childControlsCount = labels[thisIndex + 1].FirstChildControlIndex - FirstChildControlIndex - 1;
                else
                    childControlsCount = controls.Count;
                int lastControl = Mathf.Min(FirstChildControlIndex + childControlsCount, controls.Count);
                for (int i = FirstChildControlIndex; i < lastControl; i++)
                {
                    controls[i].Enabled = check;
                }
            }
            else
            {
                TextColor = Color.Red;
                int a = 10;
            }
        }

        /// <inheritdoc />
        protected override void PerformLayoutSelf()
        {
            base.PerformLayoutSelf();
            
            // Center checkbox
            CheckBox.Y = (Height - CheckBox.Height) / 2;
        }
    }
}
