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
