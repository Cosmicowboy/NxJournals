using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using NXOpen;



public class NXJournal
{
    public static NXOpen.Session theSession = NXOpen.Session.GetSession();
    public static NXOpen.UF.UFSession uFSession = NXOpen.UF.UFSession.GetUFSession();
    public static NXOpen.Part workPart = theSession.Parts.Work;
    public static NXOpen.Part displayPart = theSession.Parts.Display;

    public static string OutPutFolder { get; private set; }

    public static void Main(string[] args)
    {

        try
        {
            theSession.Parts.LoadOptions.PartLoadOption = NXOpen.LoadOptions.LoadOption.FullyLoad;

            var componentDict = new Dictionary<string, List<NXOpen.Assemblies.Component>>();

            // ----------------------------------------------
            //   Map each TT item # to Nx component bodies
            // ----------------------------------------------
            NXOpen.Assemblies.Component root = workPart.ComponentAssembly.RootComponent;

            TraverseComponents(root, componentDict);

            var folderBrowser = new FolderBrowserDialog()
            {
                RootFolder = Environment.SpecialFolder.Desktop,
                SelectedPath = @"C:\",
                Description = "Export Folder",
            };

            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                OutPutFolder = folderBrowser.SelectedPath;
            }
            else
            {
                // exit journal
                return;
            }


            ListBox lb = GetListBox(componentDict);

            var myFBuilder = new MyFormBuilder();

            Form compSelection = myFBuilder.AddControl(lb)
                                            .WithAcceptCancelButtons()
                                            .SetText("Select To Export")
                                            .Build();

            var test = compSelection.ShowDialog();

            if (test == DialogResult.OK)
            {

                var exporter = CreateNxExporter();

                foreach (string item in lb.SelectedItems)
                {
                    var componentList = componentDict[item];
                    try
                    {
                        var comp = ExtractComponentInfo(componentList);

                        ExportBodies(exporter, comp);
                    }
                    catch (Exception ex)
                    {
                        Guide.InfoWriteLine(ex.Message);
                    }

                }
            }
            else
            {
                // exit journal
                return;
            }

            Guide.InfoWriteLine("Finished, Check Output at : " + OutPutFolder);
        }
        catch (Exception ex)
        {
            Guide.InfoWriteLine(ex.Message);
        }
    }

    #region Methods

    /// <summary>
    /// Pattern Matching of name to TT standard name: #####_XXX_
    /// </summary>
    /// <param name="comp"></param>
    /// <param name="compDict"></param>
    public static bool CheckIfMatchedComponent(NXOpen.Assemblies.Component comp)
    {
        const string pattern = @"^\d{5}(-[A-Z]|-\d{2})?_(\d{3}|[xX]{3})_.+";

        if (comp == null || comp.IsSuppressed || comp.ReferenceSet == "Empty")
        {
            return false;
        }

        Match match = Regex.Match(comp.DisplayName, pattern);

        return match.Success;
    }

    private static void AddToComponentDictionary(NXOpen.Assemblies.Component comp, Dictionary<string, List<NXOpen.Assemblies.Component>> compDict)
    {
        var outList = new List<NXOpen.Assemblies.Component>();

        if (compDict.TryGetValue(comp.Name, out outList))
        {
            outList.Add(comp);
        }
        else
        {
            compDict[comp.Name] = new List<NXOpen.Assemblies.Component> { comp };
        }
    }

    /// <summary>
    /// Checks if DisplayName and Name match if not rewrites Name to match
    /// </summary>
    /// <param name="comp"></param>
    private static void CheckNamesMatch(NXOpen.Assemblies.Component comp)
    {
        if (!comp.Name.Trim().Equals(comp.DisplayName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            comp.SetName(comp.DisplayName.ToUpper());
        }
    }

    /// <summary>
    /// Walks the component tree recursively
    /// </summary>
    /// <param name="comp"></param>
    /// <param name="componentDict"></param>
    public static void TraverseComponents(NXOpen.Assemblies.Component comp, Dictionary<string, List<NXOpen.Assemblies.Component>> componentDict)
    {
        if (comp == null)
            return;

        if (CheckIfMatchedComponent(comp))
        {
            CheckNamesMatch(comp);

            AddToComponentDictionary(comp, componentDict);
        }


        foreach (NXOpen.Assemblies.Component child in comp.GetChildren())
        {
            TraverseComponents(child, componentDict);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private static StepCreator CreateNxExporter()
    {
        var stepCreator = theSession.DexManager.CreateStepCreator();

        stepCreator.ExportAs = StepCreator.ExportAsOption.Ap214;
        stepCreator.ExportFrom = StepCreator.ExportFromOption.DisplayPart;
        stepCreator.ExportSelectionBlock.SelectionScope = ObjectSelector.Scope.EntireAssembly;
        stepCreator.BsplineTol = 0.001;
        stepCreator.ExportExtRef = true;
        stepCreator.ObjectTypes.Solids = true;
        stepCreator.ObjectTypes.Curves = true;
        stepCreator.ObjectTypes.Csys = true;
        stepCreator.FileSaveFlag = false;
        stepCreator.LayerMask = "1";
        stepCreator.ProcessHoldFlag = true;

        return stepCreator;
    }

    /// <summary>
    /// Export of components as bodies
    /// <para>returns true if should write to db through DbQuill</para>
    /// </summary>
    /// <param name="stepCreator"></param>
    /// <param name="compList"></param>
    private static bool ExportBodies(StepCreator stepCreator, ComponentDTO componentDTO)
    {
        try
        {
            stepCreator.OutputFile =
                System.IO.Path.Combine(OutPutFolder, componentDTO.FileName);

            if (!stepCreator.ExportSelectionBlock.SelectionComp.Add(componentDTO.Bodies.ToArray()))
            {
                stepCreator.ExportSelectionBlock.SelectionComp.Clear();
                throw new Exception("Part not exported due to being empty");
            }

            if (stepCreator.Validate())
            {
                stepCreator.Commit();

                if (componentDTO.ComponentNumber == "XXX")
                {
                    return false;
                }

                return true;
            }
            else
            {
                throw new Exception(" Invalid stepCreator");
            }

        }
        catch (Exception ex)
        {
            Guide.InfoWriteLine(componentDTO.ComponentNumber + "_" + componentDTO.ComponentName + ": " + ex.Message);
            return false;
        }
        finally
        {
            stepCreator.ExportSelectionBlock.SelectionComp.Clear();

            // theSession.CleanUpFacetedFacesAndEdges();
        }
    }

    /// <summary>
    /// Transform component handles to proto bodies. StepCreator does not export components
    /// </summary>
    /// <param name="comps"></param>
    /// <returns></returns>
    public static List<NXObject> GetNxBodies(List<NXOpen.Assemblies.Component> comps)
    {
        var objects = new List<NXOpen.NXObject>();

        for (int i = 0; i < comps.Count; i++)
        {

            Part tempPart = (Part)comps[i].Prototype;

            foreach (Body bod in tempPart.Bodies)
            {
                if (bod.Layer != (int)Layers.SubtractBodies)
                {
                    objects.Add((Body)comps[i].FindObject("PROTO#.Bodies|" + bod.JournalIdentifier));
                }
            }
        }


        return objects;
    }


    /// <summary>
    /// NX Supplied method for closing script
    /// </summary>
    /// <param name="dummy"></param>
    /// <returns></returns>
    public static int GetUnloadOption(string dummy) { return (int)NXOpen.Session.LibraryUnloadOption.Immediately; }

    /// <summary>
    /// Returns built List box form with multi select and dock fill
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <returns></returns>
    public static ListBox GetListBox<T>(Dictionary<string, T> items)
    {
        var lb = new ListBox()
        {
            Sorted = true,
            SelectionMode = SelectionMode.MultiExtended,
            Dock = DockStyle.Fill,
            Margin = new Padding(10),
        };

        foreach (KeyValuePair<string, T> item in items)
        {
            lb.Items.Add(item.Key);
        }

        return lb;
    }

    #endregion
    #region Classes
    /// <summary>
    /// Form Builder for Nx. Allows building of complex forms without licensing
    /// </summary>
    public class MyFormBuilder
    {
        private Form _form;
        private List<Control> _controls = new List<Control>();

        public MyFormBuilder()
        {
            _form = new Form();
        }

        public bool ShowFormIcon { get; private set; }
        public string Text { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public List<Control> Controls
        {
            get
            {
                return _controls;
            }
        }

        public MyFormBuilder WithIcon()
        {
            ShowFormIcon = true;
            return this;
        }

        /// <summary>
        /// Sets form text to specified string
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public MyFormBuilder SetText(string text)
        {
            Text = text;
            return this;
        }

        /// <summary>
        /// sets both width and height to specified amounts
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns>this.MyFormBuilder</returns>
        public MyFormBuilder SetSize(int width, int height)
        {
            Width = width;
            Height = height;
            return this;
        }

        /// <summary>
        /// sets form with to specified amount 
        /// </summary>
        /// <param name="width"></param>
        /// <returns>this.MyFormBuilder</returns>
        public MyFormBuilder SetWidth(int width)
        {
            Width = width;
            return this;
        }

        /// <summary>
        /// sets the form height to specified amount
        /// </summary>
        /// <param name="height"></param>
        /// <returns>this.MyFormBuilder</returns>
        public MyFormBuilder SetHeight(int height)
        {
            Height = height;
            return this;
        }

        /// <summary>
        /// add a specified control to the form
        /// </summary>
        /// <param name="control"></param>
        public MyFormBuilder AddControl(Control control)
        {
            Controls.Add(control);
            return this;
        }

        public MyFormBuilder AddControls(IEnumerable<Control> controls)
        {
            foreach (var control in controls)
            {
                Controls.Add(control);
            }
            return this;
        }

        /// <summary>
        /// remove a specified control from form collection
        /// </summary>
        /// <param name="control"></param>
        public bool RemoveControlItem(Control control)
        {
            return _controls.Remove(control);
        }

        public MyFormBuilder WithAcceptCancelButtons()
        {
            var acceptButton = new Button
            {
                Text = "Accept",
                AutoSize = true,
                Margin = new Padding(5),
            };
            var cancelButton = new Button
            {
                Text = "Cancel",
                AutoSize = true,
                Margin = new Padding(5)
            };

            acceptButton.Click += (sender, e) =>
            {
                _form.DialogResult = DialogResult.OK;
                _form.Close();
            };

            cancelButton.Click += (sender, e) =>
            {
                _form.DialogResult = DialogResult.Cancel;
                _form.Close();
            };

            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Bottom,
                Padding = new Padding(10),
                AutoSize = true,
                //AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            buttonPanel.Controls.Add(acceptButton);
            buttonPanel.Controls.Add(cancelButton);
            Controls.Add(buttonPanel);
            _form.AcceptButton = acceptButton;
            _form.CancelButton = cancelButton;

            return this;
        }

        /// <summary>
        /// Returns a built windows form for use by caller
        /// </summary>
        /// <returns>Form._form</returns>
        public Form Build()
        {
            _form.StartPosition = FormStartPosition.CenterParent;
            _form.ShowIcon = ShowFormIcon;

            if (_form.Height == 0 && _form.Width == 0)
            {
                _form.AutoSize = true;
            }
            else
            {
                _form.Height = Height == 0 ? 400 : Height;
                _form.Width = Width == 0 ? 400 : Width;
            }

            _form.Text = Text;

            if (Controls.Count > 0)
            {
                foreach (Control control in Controls)
                {
                    _form.Controls.Add(control);
                }
            }
            return _form;
        }
    }
    #endregion
    #region Enums
    enum Layers
    {
        MainItems = 1,
        SubtractBodies = 99,
    }

    #endregion
}


