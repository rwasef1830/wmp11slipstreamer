using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing.Design;

namespace Epsilon.Globalization
{
    /// <summary>
    /// Summary description for LocalizationFilter.
    /// </summary>
    [Designer(typeof(FilterDesigner))]
    public sealed class LocalizationFilter : System.ComponentModel.Component
    {
    }

    internal sealed class FilterDesigner : ComponentDesigner, ITypeDescriptorFilterService
    {
        private StringCollection localizableProperties;
        private ITypeDescriptorFilterService existingFilter;

        [DesignOnly(true)]
        [Category("Design")]
        [Editor(typeof(StringEditor), typeof(UITypeEditor))]
        [Description("Chooses which properties will get localized.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        private StringCollection LocalizableProperties 
        {
            get 
            {
                if (localizableProperties == null) 
                {
                    localizableProperties = new StringCollection();
                }
                return localizableProperties;
            }
        }

        [DesignOnly(true)]
        [Category("Design")]
        [Browsable(false)]
        private string[] LocalizablePropertiesArray 
        {
            get 
            {
                string[] values = new string[LocalizableProperties.Count];
                LocalizableProperties.CopyTo(values, 0);
                return values;
            }
            set 
            {
                LocalizableProperties.Clear();
                if (value != null)
                {
                    LocalizableProperties.AddRange(value);
                }
            }
        }

        public override void Initialize(IComponent component) 
        {
            base.Initialize(component);
            existingFilter = GetService(typeof(ITypeDescriptorFilterService)) as ITypeDescriptorFilterService;
            if (existingFilter != null) 
            {
                IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;
                host.RemoveService(typeof(ITypeDescriptorFilterService));
                host.AddService(typeof(ITypeDescriptorFilterService), this);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (existingFilter != null)
                {
                    IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;
                    host.RemoveService(typeof(ITypeDescriptorFilterService));
                    host.AddService(typeof(ITypeDescriptorFilterService), existingFilter);
                    existingFilter = null;
                }
            }

            base.Dispose(disposing);
        }

        [Obsolete]
        public override void OnSetComponentDefaults()
        {
            base.OnSetComponentDefaults();
            LocalizableProperties.Add("Location");
            LocalizableProperties.Add("Size");
            LocalizableProperties.Add("Text");
            LocalizableProperties.Add("Font");
        }

        protected override void PreFilterProperties(IDictionary properties) 
        {
            base.PreFilterProperties(properties);
            properties.Add("LocalizableProperties", TypeDescriptor.CreateProperty(typeof(FilterDesigner), "LocalizableProperties", typeof(StringCollection), null));
            properties.Add("LocalizablePropertiesArray", TypeDescriptor.CreateProperty(typeof(FilterDesigner), "LocalizablePropertiesArray", typeof(string[]), null));
        }

        #region Implementation of ITypeDescriptorFilterService
        bool ITypeDescriptorFilterService.FilterProperties(IComponent component, IDictionary properties)
        {
            bool retVal = existingFilter.FilterProperties(component, properties);

            // Update any properties based on our localized names
            //
            if (localizableProperties != null && localizableProperties.Count > 0)
            {
                Attribute[] nonLoc = new Attribute[] {LocalizableAttribute.No};
                string[] propNames = new string[properties.Keys.Count];
                properties.Keys.CopyTo(propNames, 0);

                foreach(string propName in propNames) 
                {
                    bool localize = false;

                    foreach(string name in localizableProperties)
                    {
                        if (name.Equals(propName)) 
                        {
                            localize = true;
                            break;
                        }
                    }

                    if (!localize)
                    {
                        PropertyDescriptor prop = (PropertyDescriptor)properties[propName];
                        if (prop != null && prop.Attributes.Contains(LocalizableAttribute.Yes))
                        {
                            prop = TypeDescriptor.CreateProperty(prop.ComponentType, prop, nonLoc);
                            properties[propName] = prop;
                        }
                    }
                }
            }

            return retVal;
        }

        bool ITypeDescriptorFilterService.FilterAttributes(IComponent component, IDictionary attributes)
        {
            return existingFilter.FilterAttributes(component, attributes);
        }

        bool ITypeDescriptorFilterService.FilterEvents(IComponent component, IDictionary events)
        {
            return existingFilter.FilterEvents(component, events);
        }
        #endregion
    }

    internal sealed class StringEditor : CollectionEditor 
    {
        public StringEditor(Type baseType) : base(baseType) 
        {
        }

        protected override CollectionForm CreateCollectionForm()
        {
            return new StringCollectionForm(this);
        }

        /// <summary>
        /// Summary description for StringCollectionForm.
        /// </summary>
        private class StringCollectionForm : CollectionForm
        {
            private System.Windows.Forms.TextBox strings;
            private System.Windows.Forms.Button okButton;
            private System.Windows.Forms.Button cancelButton;
            private System.Windows.Forms.Label label1;

            public StringCollectionForm(CollectionEditor editor) : base(editor)
            {
                InitializeComponent();
            }

		    #region Windows Form Designer generated code
            /// <summary>
            /// Required method for Designer support - do not modify
            /// the contents of this method with the code editor.
            /// </summary>
            private void InitializeComponent()
            {
                this.strings = new System.Windows.Forms.TextBox();
                this.okButton = new System.Windows.Forms.Button();
                this.cancelButton = new System.Windows.Forms.Button();
                this.label1 = new System.Windows.Forms.Label();
                this.SuspendLayout();
                // 
                // strings
                // 
                this.strings.AcceptsReturn = true;
                this.strings.Anchor = (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                    | System.Windows.Forms.AnchorStyles.Left) 
                    | System.Windows.Forms.AnchorStyles.Right);
                this.strings.CausesValidation = false;
                this.strings.Location = new System.Drawing.Point(8, 8);
                this.strings.Multiline = true;
                this.strings.Name = "strings";
                this.strings.Size = new System.Drawing.Size(248, 96);
                this.strings.TabIndex = 0;
                this.strings.Text = "";
                // 
                // okButton
                // 
                this.okButton.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
                this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.okButton.Location = new System.Drawing.Point(192, 112);
                this.okButton.Name = "okButton";
                this.okButton.Size = new System.Drawing.Size(64, 24);
                this.okButton.TabIndex = 2;
                this.okButton.Text = "OK";
                // 
                // cancelButton
                // 
                this.cancelButton.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
                this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                this.cancelButton.Location = new System.Drawing.Point(192, 144);
                this.cancelButton.Name = "cancelButton";
                this.cancelButton.Size = new System.Drawing.Size(64, 24);
                this.cancelButton.TabIndex = 3;
                this.cancelButton.Text = "Cancel";
                // 
                // label1
                // 
                this.label1.Anchor = ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
                    | System.Windows.Forms.AnchorStyles.Right);
                this.label1.Location = new System.Drawing.Point(8, 112);
                this.label1.Name = "label1";
                this.label1.Size = new System.Drawing.Size(176, 56);
                this.label1.TabIndex = 1;
                this.label1.Text = "Enter the names of properties, one per line, that you wish to localize.";
                // 
                // StringCollectionForm
                // 
                this.AcceptButton = this.okButton;
                this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
                this.CancelButton = this.cancelButton;
                this.ClientSize = new System.Drawing.Size(264, 174);
                this.Controls.AddRange(new System.Windows.Forms.Control[] {
                                                                              this.label1,
                                                                              this.cancelButton,
                                                                              this.okButton,
                                                                              this.strings});
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.Name = "StringCollectionForm";
                this.Text = "Localizable Properties";
                this.ResumeLayout(false);

            }
		    #endregion

            protected override void OnClosing(CancelEventArgs e) 
            {

                if (DialogResult == System.Windows.Forms.DialogResult.OK)
                {
                    string[] lines = strings.Lines;

                    // Replace array if there are blanks.
                    //
                    int blanks = 0;
                    foreach(string s in lines) 
                    {
                        if (s.Length == 0) 
                        {
                            blanks++;
                        }
                    }

                    if (blanks > 0) 
                    {
                        string[] newLines = new string[lines.Length - blanks];
                        int line = 0;

                        foreach(string s in lines) 
                        {
                            if (s.Length > 0) 
                            {
                                newLines[line++] = s;
                            }
                        }

                        lines = newLines;
                    }

                    // Now hand this back to the Items array
                    //
                    object[] values = new object[lines.Length];
                    lines.CopyTo(values, 0);
                    Items = values;
                }
            }

            protected override void OnEditValueChanged() 
            {
                string[] lines = new string[Items.Length];
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = (string)Items[i];
                }
                strings.Lines = lines;
            }
        }
    }
}
